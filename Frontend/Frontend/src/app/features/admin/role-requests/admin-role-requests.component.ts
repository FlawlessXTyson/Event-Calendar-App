import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RoleRequestService } from '../../../core/services/role-request.service';
import { ToastService } from '../../../core/services/toast.service';
import { RoleChangeRequest, RequestStatus } from '../../../core/models/models';

@Component({
  selector: 'app-admin-role-requests',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div>
      <div style="margin-bottom:24px;"><h1 style="font-size:1.5rem;">Role Upgrade Requests</h1><p>Users requesting Organizer access</p></div>
      @if (loading()) { <div class="loading-center"><div class="spinner"></div></div> }
      @else if (requests().length === 0) {
        <div class="empty-state"><span class="material-icons-round empty-icon">verified_user</span><h3>No pending requests</h3><p>All role requests have been reviewed.</p></div>
      } @else {
        <div class="table-wrapper">
          <table>
            <thead><tr><th>#</th><th>User</th><th>Requested Role</th><th>Status</th><th>Requested At</th><th>Actions</th></tr></thead>
            <tbody>
              @for (r of requests(); track r.requestId) {
                <tr>
                  <td style="color:var(--text-muted);">#{{ r.requestId }}</td>
                  <td>
                    <div style="font-weight:600;font-size:.9rem;">{{ r.user?.name || 'User #' + r.userId }}</div>
                    <div style="font-size:.78rem;color:var(--text-muted);">{{ r.user?.email || '—' }}</div>
                  </td>
                  <td><span class="badge badge-warning">Organizer</span></td>
                  <td><span class="badge" [class]="statusBadge(r.status)">{{ statusLabel(r.status) }}</span></td>
                  <td style="color:var(--text-muted);">{{ r.requestedAt | date:'MMM d, y, h:mm a' }}</td>
                  <td>
                    @if (r.status === RequestStatus.PENDING) {
                      <div style="display:flex;gap:8px;">
                        <button type="button" class="btn btn-success btn-sm" [disabled]="approving() === r.requestId" (click)="approve(r)">
                          @if (approving() === r.requestId) { <div class="spinner spinner-sm"></div> }
                          @else { <span class="material-icons-round" style="font-size:15px;">check</span> Approve }
                        </button>
                        <button type="button" class="btn btn-danger btn-sm" [disabled]="rejecting() === r.requestId" (click)="reject(r)">
                          @if (rejecting() === r.requestId) { <div class="spinner spinner-sm"></div> }
                          @else { <span class="material-icons-round" style="font-size:15px;">close</span> Reject }
                        </button>
                      </div>
                    } @else {
                      <span class="text-muted text-sm">Reviewed</span>
                    }
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
export class AdminRoleRequestsComponent implements OnInit {
  private roleSvc = inject(RoleRequestService);
  private toast   = inject(ToastService);
  RequestStatus   = RequestStatus;

  requests  = signal<RoleChangeRequest[]>([]);
  loading   = signal(true);
  approving = signal<number | null>(null);
  rejecting = signal<number | null>(null);

  ngOnInit() { this.roleSvc.getPending().subscribe({ next: rs => { this.requests.set(rs); this.loading.set(false); }, error: () => this.loading.set(false) }); }

  approve(r: RoleChangeRequest) {
    this.approving.set(r.requestId);
    this.roleSvc.approve(r.requestId).subscribe({
      next: () => {
        this.requests.update(rs => rs.filter(x => x.requestId !== r.requestId));
        this.toast.success(`${r.user?.name || 'User #' + r.userId} has been promoted to Organizer!`, 'Role Approved');
        this.approving.set(null);
      },
      error: () => this.approving.set(null)
    });
  }

  reject(r: RoleChangeRequest) {
    this.rejecting.set(r.requestId);
    this.roleSvc.reject(r.requestId).subscribe({
      next: () => {
        this.requests.update(rs => rs.filter(x => x.requestId !== r.requestId));
        this.toast.warning(`Role request from ${r.user?.name || 'User #' + r.userId} has been rejected.`, 'Role Rejected');
        this.rejecting.set(null);
      },
      error: () => this.rejecting.set(null)
    });
  }

  statusLabel(s: RequestStatus) { return { 1: 'Pending', 2: 'Approved', 3: 'Rejected' }[s] ?? s; }
  statusBadge(s: RequestStatus) { return { 1: 'badge-warning', 2: 'badge-success', 3: 'badge-danger' }[s] ?? 'badge-gray'; }
}
