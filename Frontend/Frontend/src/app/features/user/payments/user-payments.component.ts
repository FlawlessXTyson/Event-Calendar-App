import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaymentService } from '../../../core/services/payment.service';
import { PaymentResponse, PaymentStatus } from '../../../core/models/models';

@Component({
  selector: 'app-user-payments',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div>
      <div style="margin-bottom:24px;"><h1 style="font-size:1.5rem;">My Payments</h1><p>Your complete payment history</p></div>
      <div class="stats-grid" style="margin-bottom:24px;">
        <div class="stat-card"><div class="stat-icon" style="background:var(--success-light);"><span class="material-icons-round" style="color:var(--success);">payments</span></div><div class="stat-value">₹{{ totalSpent() | number:'1.0-0' }}</div><div class="stat-label">Total Spent</div></div>
        <div class="stat-card"><div class="stat-icon" style="background:var(--info-light);"><span class="material-icons-round" style="color:var(--info);">receipt_long</span></div><div class="stat-value">{{ payments().length }}</div><div class="stat-label">Total Payments</div></div>
        <div class="stat-card"><div class="stat-icon" style="background:var(--warning-light);"><span class="material-icons-round" style="color:var(--warning);">currency_exchange</span></div><div class="stat-value">₹{{ totalRefunded() | number:'1.0-0' }}</div><div class="stat-label">Total Refunded</div></div>
      </div>
      @if (loading()) {
        <div class="loading-center"><div class="spinner"></div></div>
      } @else if (payments().length === 0) {
        <div class="empty-state"><span class="material-icons-round empty-icon">receipt_long</span><h3>No payments yet</h3><p>Register for paid events to see your payment history here.</p></div>
      } @else {
        <div class="table-wrapper">
          <table>
            <thead><tr><th>#</th><th>Event</th><th>Amount Paid</th><th>Status</th><th>Refunded Amount</th><th>Date</th></tr></thead>
            <tbody>
              @for (p of payments(); track p.paymentId) {
                <tr>
                  <td style="color:var(--text-muted);">#{{ p.paymentId }}</td>
                  <td style="font-weight:600;">{{ p.eventTitle || 'Event #' + p.eventId }}</td>
                  <td style="font-weight:700;">₹{{ p.amountPaid | number:'1.0-0' }}</td>
                  <td><span class="badge" [class]="statusBadge(p.status)">{{ statusLabel(p.status) }}</span></td>
                  <td>{{ p.refundedAmount ? ('₹' + (p.refundedAmount | number:'1.0-0')) : '—' }}</td>
                  <td style="color:var(--text-muted);">{{ p.paymentDate | date:'MMM d, y, h:mm a' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `
})
export class UserPaymentsComponent implements OnInit {
  private paySvc = inject(PaymentService);
  payments = signal<PaymentResponse[]>([]);
  loading  = signal(true);
  totalSpent    = () => this.payments().filter(p => p.status === PaymentStatus.SUCCESS).reduce((s,p) => s+p.amountPaid, 0);
  totalRefunded = () => this.payments().filter(p => p.status === PaymentStatus.REFUNDED).reduce((s,p) => s+(p.refundedAmount??0), 0);
  ngOnInit() { this.paySvc.getMyPayments().subscribe({ next: ps => { this.payments.set(ps); this.loading.set(false); }, error: () => this.loading.set(false) }); }
  statusLabel(s: PaymentStatus) { return {1:'Pending',2:'Success',3:'Failed',4:'Refunded'}[s] ?? s; }
  statusBadge(s: PaymentStatus) { return {1:'badge-warning',2:'badge-success',3:'badge-danger',4:'badge-info'}[s] ?? 'badge-gray'; }
}
