import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaymentService } from '../../../core/services/payment.service';
import { ToastService } from '../../../core/services/toast.service';
import { PaymentResponse, CommissionSummary, PaymentStatus } from '../../../core/models/models';

@Component({
  selector: 'app-admin-payments',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-payments.component.html',
  styleUrl: './admin-payments.component.css'
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
