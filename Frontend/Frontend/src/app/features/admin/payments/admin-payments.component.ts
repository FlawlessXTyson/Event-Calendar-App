import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaymentService } from '../../../core/services/payment.service';
import { ToastService } from '../../../core/services/toast.service';
import { PaymentResponse, CommissionSummary, PaymentStatus } from '../../../core/models/models';

@Component({
  selector: 'app-admin-payments',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div>
      <div style="margin-bottom:24px;"><h1 style="font-size:1.5rem;">Payment Management</h1><p>All transactions and platform revenue</p></div>

      @if (commission()) {
        <div class="stats-grid" style="margin-bottom:24px;">
          <div class="stat-card" style="border-top:3px solid var(--success);"><div class="stat-icon" style="background:var(--success-light);"><span class="material-icons-round" style="color:var(--success);">account_balance</span></div><div class="stat-value" style="color:var(--success);">₹{{ commission()!.totalCommission | number:'1.0-0' }}</div><div class="stat-label">Platform Revenue</div></div>
          <div class="stat-card" style="border-top:3px solid var(--primary);"><div class="stat-icon" style="background:var(--primary-light);"><span class="material-icons-round" style="color:var(--primary);">payments</span></div><div class="stat-value">₹{{ commission()!.totalOrganizerPayout | number:'1.0-0' }}</div><div class="stat-label">Organizer Payouts</div></div>
          <div class="stat-card" style="border-top:3px solid var(--info);"><div class="stat-icon" style="background:var(--info-light);"><span class="material-icons-round" style="color:var(--info);">receipt_long</span></div><div class="stat-value">{{ commission()!.totalPayments }}</div><div class="stat-label">Total Transactions</div></div>
        </div>
      }

      @if (loading()) { <div class="loading-center"><div class="spinner"></div></div> }
      @else if (payments().length === 0) {
        <div class="empty-state"><span class="material-icons-round empty-icon">payments</span><h3>No payments found</h3></div>
      } @else {
        <div class="table-wrapper">
          <table>
            <thead><tr><th>#</th><th>Event</th><th>User</th><th>Amount</th><th>Commission</th><th>Organizer</th><th>Status</th><th>Refunded</th><th>Date</th><th>Actions</th></tr></thead>
            <tbody>
              @for (p of payments(); track p.paymentId) {
                <tr>
                  <td style="color:var(--text-muted);">#{{ p.paymentId }}</td>
                  <td>{{ p.eventTitle || ('Event #' + p.eventId) }}</td>
                  <td style="color:var(--text-muted);">{{ p.userName || '—' }}</td>
                  <td style="font-weight:700;">₹{{ p.amountPaid | number:'1.0-0' }}</td>
                  <td style="color:var(--danger);">₹{{ (p.amountPaid * 0.1) | number:'1.0-0' }}</td>
                  <td style="color:var(--success);">₹{{ (p.amountPaid * 0.9) | number:'1.0-0' }}</td>
                  <td><span class="badge" [class]="statusBadge(p.status)">{{ statusLabel(p.status) }}</span></td>
                  <td>{{ p.refundedAmount ? ('₹' + (p.refundedAmount | number:'1.0-0')) : '—' }}</td>
                  <td style="color:var(--text-muted);white-space:nowrap;">{{ p.paymentDate | date:'MMM d, y' }}</td>
                  <td>
                    @if (p.status === PaymentStatus.SUCCESS) {
                      <button type="button" class="btn btn-warning btn-sm" [disabled]="refunding() === p.paymentId" (click)="refund(p)">
                        @if (refunding() === p.paymentId) { <div class="spinner spinner-sm"></div> } @else { Refund }
                      </button>
                    } @else { <span class="text-muted text-sm">—</span> }
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
export class AdminPaymentsComponent implements OnInit {
  private paySvc = inject(PaymentService);
  private toast  = inject(ToastService);
  PaymentStatus  = PaymentStatus;

  payments   = signal<PaymentResponse[]>([]);
  commission = signal<CommissionSummary | null>(null);
  loading    = signal(true);
  refunding  = signal<number | null>(null);

  ngOnInit() {
    this.paySvc.getAll().subscribe({ next: ps => { this.payments.set(ps); this.loading.set(false); }, error: () => this.loading.set(false) });
    this.paySvc.getCommissionSummary().subscribe({ next: c => this.commission.set(c), error: () => {} });
  }

  refund(p: PaymentResponse) {
    if (!confirm(`Issue full refund of ₹${p.amountPaid} for Payment #${p.paymentId}?\n\nNote: Backend rejects refunds after event has started.`)) return;
    this.refunding.set(p.paymentId);
    this.paySvc.refund(p.paymentId).subscribe({
      next: updated => {
        this.payments.update(ps => ps.map(x => x.paymentId === p.paymentId ? updated : x));
        this.toast.success(`Refund of ₹${p.amountPaid} processed for Payment #${p.paymentId}.`, 'Refund Successful');
        this.refunding.set(null);
      },
      error: () => this.refunding.set(null)
    });
  }

  statusLabel(s: PaymentStatus) { return { 1: 'Pending', 2: 'Success', 3: 'Failed', 4: 'Refunded' }[s] ?? s; }
  statusBadge(s: PaymentStatus) { return { 1: 'badge-warning', 2: 'badge-success', 3: 'badge-danger', 4: 'badge-info' }[s] ?? 'badge-gray'; }
}
