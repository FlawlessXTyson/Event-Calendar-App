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
  template: `
    <div>
      <div style="margin-bottom:24px;">
        <h1 style="font-size:1.5rem;">Refund Requests</h1>
        <p>Review and process user refund requests</p>
      </div>

      @if (loading()) { <div class="loading-center"><div class="spinner"></div></div> }
      @else if (requests().length === 0) {
        <div class="empty-state">
          <span class="material-icons-round empty-icon">receipt_long</span>
          <h3>No pending refund requests</h3>
          <p>All refund requests have been reviewed.</p>
        </div>
      } @else {
        <div style="display:flex;flex-direction:column;gap:12px;">
          @for (r of requests(); track r.refundRequestId) {
            <div class="card">
              <div class="card-body" style="display:flex;align-items:center;gap:16px;flex-wrap:wrap;">

                <!-- User info -->
                <div style="flex:1;min-width:160px;">
                  <div style="font-size:.7rem;font-weight:700;color:var(--text-muted);text-transform:uppercase;margin-bottom:2px;">User</div>
                  <div style="font-weight:700;font-size:.9rem;">{{ r.userName || 'User #' + r.userId }}</div>
                  <div style="font-size:.78rem;color:var(--text-muted);">{{ r.userEmail }}</div>
                </div>

                <!-- Event info -->
                <div style="flex:1;min-width:160px;">
                  <div style="font-size:.7rem;font-weight:700;color:var(--text-muted);text-transform:uppercase;margin-bottom:2px;">Event</div>
                  <div style="font-weight:600;font-size:.9rem;">{{ r.eventTitle }}</div>
                  <div style="font-size:.78rem;color:var(--text-muted);">Payment #{{ r.paymentId }}</div>
                </div>

                <!-- Amount -->
                <div style="min-width:100px;text-align:center;">
                  <div style="font-size:.7rem;font-weight:700;color:var(--text-muted);text-transform:uppercase;margin-bottom:2px;">Amount Paid</div>
                  <div style="font-size:1.2rem;font-weight:800;color:var(--primary);">₹{{ r.amountPaid | number:'1.0-0' }}</div>
                </div>

                <!-- Requested at -->
                <div style="min-width:120px;">
                  <div style="font-size:.7rem;font-weight:700;color:var(--text-muted);text-transform:uppercase;margin-bottom:2px;">Requested</div>
                  <div style="font-size:.82rem;">{{ r.requestedAt | date:'MMM d, y' }}</div>
                  <div style="font-size:.75rem;color:var(--text-muted);">{{ r.requestedAt | date:'h:mm a' }}</div>
                </div>

                <!-- Actions -->
                <div style="display:flex;gap:8px;align-items:center;flex-wrap:wrap;">
                  <!-- Percentage input -->
                  <div style="display:flex;align-items:center;gap:6px;">
                    <input type="number" min="0" max="100" step="5"
                      [(ngModel)]="refundPct[r.refundRequestId]"
                      style="width:72px;padding:6px 8px;border:1.5px solid var(--border);border-radius:var(--r-sm);font-size:.85rem;text-align:center;"
                      placeholder="%" />
                    <span style="font-size:.8rem;color:var(--text-muted);">%</span>
                  </div>
                  <button type="button" class="btn btn-success btn-sm"
                    [disabled]="processing() === r.refundRequestId || !refundPct[r.refundRequestId]"
                    (click)="approve(r)">
                    @if (processing() === r.refundRequestId) { <div class="spinner spinner-sm"></div> }
                    @else { <span class="material-icons-round" style="font-size:15px;">check</span> Approve }
                  </button>
                  <button type="button" class="btn btn-danger btn-sm"
                    [disabled]="processing() === r.refundRequestId"
                    (click)="reject(r)">
                    @if (processing() === r.refundRequestId) { <div class="spinner spinner-sm"></div> }
                    @else { <span class="material-icons-round" style="font-size:15px;">close</span> Reject }
                  </button>
                </div>

              </div>
              <!-- Refund preview -->
              @if (refundPct[r.refundRequestId] > 0) {
                <div style="padding:8px 24px 12px;font-size:.8rem;color:var(--text-secondary);border-top:1px solid var(--border);background:var(--surface-2);">
                  Refund amount: <strong style="color:var(--success);">₹{{ (r.amountPaid * refundPct[r.refundRequestId] / 100) | number:'1.0-2' }}</strong>
                  ({{ refundPct[r.refundRequestId] }}% of ₹{{ r.amountPaid | number:'1.0-0' }})
                </div>
              }
            </div>
          }
        </div>
      }
    </div>
  `
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
