import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RefundRequestService } from '../../../core/services/refund-request.service';
import { ToastService } from '../../../core/services/toast.service';
import { RefundRequestResponse, RefundRequestStatus } from '../../../core/models/models';

@Component({
  selector: 'app-admin-refund-requests',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-refund-requests.component.html',
  styleUrl: './admin-refund-requests.component.css'
})
export class AdminRefundRequestsComponent implements OnInit {
  private svc   = inject(RefundRequestService);
  private toast = inject(ToastService);

  requests   = signal<RefundRequestResponse[]>([]);
  loading    = signal(true);
  processing = signal<number | null>(null);
  refundPct: Record<number, number> = {};

  ngOnInit() {
    this.svc.getPending().subscribe({
      next: rs => { this.requests.set(rs); rs.forEach(r => this.refundPct[r.refundRequestId] = 100); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  approve(r: RefundRequestResponse) {
    const pct = this.refundPct[r.refundRequestId];
    if (!pct && pct !== 0) { this.toast.warning('Enter a refund percentage (0–100).', 'Required'); return; }
    this.processing.set(r.refundRequestId);
    this.svc.approve(r.refundRequestId, pct).subscribe({
      next: () => {
        this.requests.update(rs => rs.filter(x => x.refundRequestId !== r.refundRequestId));
        this.toast.success(`Refund of ${pct}% (₹${(r.amountPaid * pct / 100).toFixed(0)}) approved for ${r.userName}.`, 'Refund Approved');
        this.processing.set(null);
      },
      error: () => this.processing.set(null)
    });
  }

  reject(r: RefundRequestResponse) {
    this.processing.set(r.refundRequestId);
    this.svc.reject(r.refundRequestId).subscribe({
      next: () => {
        this.requests.update(rs => rs.filter(x => x.refundRequestId !== r.refundRequestId));
        this.toast.warning(`Refund request from ${r.userName} rejected.`, 'Refund Rejected');
        this.processing.set(null);
      },
      error: () => this.processing.set(null)
    });
  }
}
