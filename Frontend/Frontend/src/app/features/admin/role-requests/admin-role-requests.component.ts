import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RoleRequestService } from '../../../core/services/role-request.service';
import { ToastService } from '../../../core/services/toast.service';
import { RoleChangeRequest, RequestStatus } from '../../../core/models/models';

@Component({
  selector: 'app-admin-role-requests',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-role-requests.component.html',
  styleUrl: './admin-role-requests.component.css'
})
export class AdminRoleRequestsComponent implements OnInit {
  private roleSvc = inject(RoleRequestService);
  private toast   = inject(ToastService);
  RequestStatus   = RequestStatus;

  requests  = signal<RoleChangeRequest[]>([]);
  loading   = signal(true);
  approving = signal<number | null>(null);
  rejecting = signal<number | null>(null);

  ngOnInit() {
    this.roleSvc.getPending().subscribe({
      next: rs => { this.requests.set(rs); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

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
