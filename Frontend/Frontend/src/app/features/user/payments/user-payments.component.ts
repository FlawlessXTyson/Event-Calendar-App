import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaymentService } from '../../../core/services/payment.service';
import { PaymentResponse, PaymentStatus } from '../../../core/models/models';

@Component({
  selector: 'app-user-payments',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './user-payments.component.html',
  styleUrl: './user-payments.component.css'
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
