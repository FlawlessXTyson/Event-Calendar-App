import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PaymentService } from '../../../core/services/payment.service';
import { PaymentResponse, PaymentStatus, PagedResult } from '../../../core/models/models';

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
          <div class="stat-icon" style="background:#FEE2E2;">
            <span class="material-icons-round" style="color:#DC2626;">currency_rupee</span>
          </div>
          <div class="stat-value">&#8377;{{ totalRefunded() | number:'1.0-0' }}</div>
          <div class="stat-label">Total Refunded</div>
        </div>
        <div class="stat-card">
          <div class="stat-icon" style="background:#D1FAE5;">
            <span class="material-icons-round" style="color:#059669;">check_circle</span>
          </div>
          <div class="stat-value">{{ totalRecords() }}</div>
          <div class="stat-label">Total Processed</div>
        </div>
      </div>

      @if (loading()) {
        <div class="loading-center"><div class="spinner"></div></div>
      } @else if (refunds().length === 0) {
        <div class="empty-state">
          <span class="material-icons-round empty-icon">receipt_long</span>
          <h3>No refunds yet</h3>
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
                <th>Payment Date</th>
                <th>Refunded Amount</th>
                <th>Refunded At</th>
              </tr>
            </thead>
            <tbody>
              @for (r of refunds(); track r.paymentId) {
                <tr>
                  <td>
                    <div style="font-weight:600;font-size:.88rem;">{{ r.userName || '—' }}</div>
                    <div style="font-size:.75rem;color:var(--text-muted);">{{ r.userEmail || '—' }}</div>
                  </td>
                  <td style="font-weight:600;">{{ r.eventTitle || ('Event #' + r.eventId) }}</td>
                  <td style="color:var(--text-muted);">{{ r.eventDate ? (r.eventDate | date:'MMM d, y') : '—' }}</td>
                  <td style="font-weight:700;">&#8377;{{ r.amountPaid | number:'1.0-0' }}</td>
                  <td style="color:var(--text-muted);font-size:.82rem;">{{ r.paymentDate | date:'MMM d, y, h:mm a' }}</td>
                  <td>
                    @if (r.refundedAmount) {
                      <span style="font-weight:700;color:var(--danger);">&#8377;{{ r.refundedAmount | number:'1.0-0' }}</span>
                    } @else {
                      <span style="color:var(--text-muted);">—</span>
                    }
                  </td>
                  <td style="color:var(--text-muted);font-size:.82rem;">
                    {{ r.refundedAt ? (r.refundedAt | date:'MMM d, y, h:mm a') : '—' }}
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        @if (totalPages() > 1) {
          <div style="padding:16px 0;display:flex;align-items:center;justify-content:space-between;flex-wrap:wrap;gap:10px;">
            <div style="font-size:.82rem;color:var(--text-muted);">
              Page {{ page() }} of {{ totalPages() }} &nbsp;·&nbsp; {{ refunds().length }} of {{ totalRecords() }} refunds
            </div>
            <div style="display:flex;gap:6px;">
              <button type="button" class="btn btn-ghost btn-sm" [disabled]="page() <= 1" (click)="goPage(1)">
                <span class="material-icons-round" style="font-size:16px;">first_page</span>
              </button>
              <button type="button" class="btn btn-ghost btn-sm" [disabled]="page() <= 1" (click)="goPage(page() - 1)">
                <span class="material-icons-round" style="font-size:16px;">chevron_left</span>
              </button>
              @for (p of pageNums(); track p) {
                <button type="button" class="btn btn-sm"
                  [class]="p === page() ? 'btn-primary' : 'btn-ghost'"
                  (click)="goPage(p)">{{ p }}</button>
              }
              <button type="button" class="btn btn-ghost btn-sm" [disabled]="page() >= totalPages()" (click)="goPage(page() + 1)">
                <span class="material-icons-round" style="font-size:16px;">chevron_right</span>
              </button>
              <button type="button" class="btn btn-ghost btn-sm" [disabled]="page() >= totalPages()" (click)="goPage(totalPages())">
                <span class="material-icons-round" style="font-size:16px;">last_page</span>
              </button>
            </div>
          </div>
        }
      }
    </div>
  `
})
export class OrganizerRefundsComponent implements OnInit {
  private paySvc = inject(PaymentService);
  PaymentStatus  = PaymentStatus;

  refunds      = signal<PaymentResponse[]>([]);
  loading      = signal(true);
  page         = signal(1);
  pageSize     = 10;
  totalRecords = signal(0);

  totalRefunded = computed(() => this.refunds().reduce((s, r) => s + (r.refundedAmount ?? 0), 0));
  totalPages    = computed(() => Math.max(1, Math.ceil(this.totalRecords() / this.pageSize)));

  pageNums = computed(() => {
    const total = this.totalPages();
    const cur   = this.page();
    const start = Math.max(1, cur - 2);
    const end   = Math.min(total, cur + 2);
    const pages: number[] = [];
    for (let i = start; i <= end; i++) pages.push(i);
    return pages;
  });

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.paySvc.getOrganizerRefunds(this.page(), this.pageSize).subscribe({
      next: (res: PagedResult<PaymentResponse>) => {
        this.refunds.set(res.data);
        this.totalRecords.set(res.totalRecords);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  goPage(p: number) {
    this.page.set(p);
    this.load();
  }
}
