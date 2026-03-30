import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EventService } from '../../../core/services/event.service';
import { PaymentService } from '../../../core/services/payment.service';
import { EventResponse, PaymentResponse, PaymentStatus } from '../../../core/models/models';

interface RefundRow {
  eventId: number;
  eventTitle: string;
  eventDate: string;
  paymentId: number;
  userId: number;
  userName: string;
  userEmail: string;
  amountPaid: number;
  refundedAmount: number;
  status: PaymentStatus;
  paymentDate: string;
  refundedAt?: string;
}

@Component({
  selector: 'app-organizer-refunds',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div>
      <div style="margin-bottom:24px;">
        <h1 style="font-size:1.5rem;">Refunds</h1>
        <p>Payments refunded by attendees who cancelled their registrations</p>
      </div>

      <!-- Stats -->
      <div class="stats-grid" style="margin-bottom:24px;">
        <div class="stat-card">
          <div class="stat-icon" style="background:#FEE2E2;"><span class="material-icons-round" style="color:#DC2626;">currency_rupee</span></div>
          <div class="stat-value">₹{{ totalRefunded() | number:'1.0-0' }}</div>
          <div class="stat-label">Total Refunded</div>
        </div>
        <div class="stat-card">
          <div class="stat-icon" style="background:#FEF3C7;"><span class="material-icons-round" style="color:#D97706;">hourglass_top</span></div>
          <div class="stat-value">{{ pendingCount() }}</div>
          <div class="stat-label">Pending Refunds</div>
        </div>
        <div class="stat-card">
          <div class="stat-icon" style="background:#D1FAE5;"><span class="material-icons-round" style="color:#059669;">check_circle</span></div>
          <div class="stat-value">{{ processedCount() }}</div>
          <div class="stat-label">Processed</div>
        </div>
      </div>

      <!-- Search -->
      <div style="margin-bottom:16px;display:flex;gap:12px;flex-wrap:wrap;">
        <div style="flex:1;min-width:200px;position:relative;">
          <span class="material-icons-round" style="position:absolute;left:10px;top:50%;transform:translateY(-50%);color:var(--text-muted);font-size:20px;">search</span>
          <input [(ngModel)]="search" type="search" class="form-control" placeholder="Search by event name…" style="padding-left:38px;" />
        </div>
        <select [(ngModel)]="filterStatus" class="form-control" style="width:auto;min-width:150px;">
          <option value="">All statuses</option>
          <option value="refunded">Refunded</option>
          <option value="pending">Pending</option>
        </select>
      </div>

      @if (loading()) {
        <div class="loading-center"><div class="spinner"></div></div>
      } @else if (filtered().length === 0) {
        <div class="empty-state">
          <span class="material-icons-round empty-icon">receipt_long</span>
          <h3>No refunds found</h3>
          <p>Refunds appear here when attendees cancel paid registrations.</p>
        </div>
      } @else {
        <div class="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>Attendee</th>
                <th>Event</th>
                <th>Event Date</th>
                <th>Amount Paid</th>
                <th>Refunded</th>
                <th>Status</th>
                <th>Payment Date</th>
                <th>Refunded At</th>
              </tr>
            </thead>
            <tbody>
              @for (r of filtered(); track r.paymentId) {
                <tr>
                  <td>
                    <div style="font-weight:600;font-size:.88rem;">{{ r.userName || 'Loading...' }}</div>
                    <div style="font-size:.75rem;color:var(--text-muted);">{{ r.userEmail || '—' }}</div>
                  </td>
                  <td style="font-weight:600;">{{ r.eventTitle }}</td>
                  <td style="color:var(--text-muted);">{{ r.eventDate | date:'MMM d, y' }}</td>
                  <td style="font-weight:700;">₹{{ r.amountPaid | number:'1.0-0' }}</td>
                  <td>
                    @if (r.refundedAmount) {
                      <span style="font-weight:700;color:var(--danger);">₹{{ r.refundedAmount | number:'1.0-0' }}</span>
                    } @else {
                      <span class="text-muted">—</span>
                    }
                  </td>
                  <td>
                    @if (r.status === PaymentStatus.REFUNDED) {
                      <span class="badge badge-success">Refunded</span>
                    } @else {
                      <span class="badge badge-warning">Pending</span>
                    }
                  </td>
                  <td style="color:var(--text-muted);font-size:.82rem;">{{ r.paymentDate | date:'MMM d, y, h:mm a' }}</td>
                  <td style="color:var(--text-muted);font-size:.82rem;">
                    {{ r.refundedAt ? (r.refundedAt | date:'MMM d, y, h:mm a') : '—' }}
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `
})
export class OrganizerRefundsComponent implements OnInit {
  private eventSvc = inject(EventService);
  private paySvc   = inject(PaymentService);
  PaymentStatus    = PaymentStatus;

  refunds  = signal<RefundRow[]>([]);
  loading  = signal(true);
  search   = '';
  filterStatus = '';

  totalRefunded  = computed(() => this.refunds().filter(r => r.status === PaymentStatus.REFUNDED).reduce((s, r) => s + (r.refundedAmount ?? 0), 0));
  pendingCount   = computed(() => this.refunds().filter(r => r.status !== PaymentStatus.REFUNDED).length);
  processedCount = computed(() => this.refunds().filter(r => r.status === PaymentStatus.REFUNDED).length);

  filtered = computed(() => {
    const s = this.search.toLowerCase();
    return this.refunds().filter(r => {
      const matchSearch = !s || r.eventTitle.toLowerCase().includes(s) || r.userName.toLowerCase().includes(s) || r.userEmail.toLowerCase().includes(s);
      const matchStatus = !this.filterStatus ||
        (this.filterStatus === 'refunded' && r.status === PaymentStatus.REFUNDED) ||
        (this.filterStatus === 'pending'  && r.status !== PaymentStatus.REFUNDED);
      return matchSearch && matchStatus;
    });
  });

  ngOnInit() {
    // Load all organizer events, then fetch payments for each paid event
    this.eventSvc.getMyEvents().subscribe({
      next: evs => {
        const paidEvents = evs.filter(e => e.isPaidEvent);
        if (paidEvents.length === 0) { this.loading.set(false); return; }

        let loaded = 0;
        const rows: RefundRow[] = [];

        paidEvents.forEach(ev => {
          this.paySvc.getByEvent(ev.eventId).subscribe({
            next: payments => {
              // Only show refunded or payments from cancelled registrations
              const refundRelated = payments.filter(p =>
                p.status === PaymentStatus.REFUNDED ||
                (p.refundedAmount !== undefined && p.refundedAmount !== null)
              );
              refundRelated.forEach(p => {
                rows.push({
                  eventId: ev.eventId,
                  eventTitle: ev.title,
                  eventDate: ev.eventDate,
                  paymentId: p.paymentId,
                  userId: p.userId ?? 0,
                  userName: p.userName ?? '',
                  userEmail: p.userEmail ?? '',
                  amountPaid: p.amountPaid,
                  refundedAmount: p.refundedAmount ?? 0,
                  status: p.status,
                  paymentDate: p.paymentDate,
                  refundedAt: p.refundedAt
                });
              });
              loaded++;
              if (loaded === paidEvents.length) {
                // Sort by payment date descending
                rows.sort((a, b) => new Date(b.paymentDate).getTime() - new Date(a.paymentDate).getTime());
                this.refunds.set(rows);
                this.loading.set(false);
              }
            },
            error: () => {
              loaded++;
              if (loaded === paidEvents.length) {
                this.refunds.set(rows);
                this.loading.set(false);
              }
            }
          });
        });
      },
      error: () => this.loading.set(false)
    });
  }
}
