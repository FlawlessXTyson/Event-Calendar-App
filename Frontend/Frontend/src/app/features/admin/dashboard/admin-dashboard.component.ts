import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { EventService } from '../../../core/services/event.service';
import { UserService } from '../../../core/services/user.service';
import { PaymentService } from '../../../core/services/payment.service';
import { RoleRequestService } from '../../../core/services/role-request.service';
import { ToastService } from '../../../core/services/toast.service';
import { EventResponse, UserDto, CommissionSummary, RoleChangeRequest, ApprovalStatus } from '../../../core/models/models';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.css']
})
export class AdminDashboardComponent implements OnInit {
  auth = inject(AuthService);
  private eventSvc   = inject(EventService);
  private userSvc    = inject(UserService);
  private paySvc     = inject(PaymentService);
  private roleSvc    = inject(RoleRequestService);
  private toast      = inject(ToastService);

  allEvents    = signal<EventResponse[]>([]);
  users        = signal<UserDto[]>([]);
  commission   = signal<CommissionSummary | null>(null);
  roleRequests = signal<RoleChangeRequest[]>([]);
  approving    = signal<number | null>(null);
  rejecting    = signal<number | null>(null);
  approvingRole= signal<number | null>(null);
  rejectingRole= signal<number | null>(null);

  totalEvents  = () => this.allEvents().length;
  // Only show upcoming pending events (not ended) in the dashboard widget
  pendingEvents = () => this.allEvents().filter(e =>
    e.approvalStatus === ApprovalStatus.PENDING && e.hasEnded !== true
  );

  ngOnInit() {
    this.eventSvc.getPending().subscribe({ next: evs => this.allEvents.set(evs), error: () => {} });
    this.userSvc.getAll().subscribe({ next: us => this.users.set(us), error: () => {} });
    this.paySvc.getCommissionSummary().subscribe({ next: c => this.commission.set(c), error: () => {} });
    this.roleSvc.getPending().subscribe({ next: rs => this.roleRequests.set(rs), error: () => {} });
  }

  approve(ev: EventResponse) {
    this.approving.set(ev.eventId);
    this.eventSvc.approve(ev.eventId).subscribe({
      next: () => { this.allEvents.update(es => es.filter(e => e.eventId !== ev.eventId)); this.toast.success(`"${ev.title}" approved and is now live!`, 'Event Approved'); this.approving.set(null); },
      error: () => this.approving.set(null)
    });
  }

  reject(ev: EventResponse) {
    this.rejecting.set(ev.eventId);
    this.eventSvc.reject(ev.eventId).subscribe({
      next: () => { this.allEvents.update(es => es.filter(e => e.eventId !== ev.eventId)); this.toast.warning(`"${ev.title}" has been rejected.`, 'Event Rejected'); this.rejecting.set(null); },
      error: () => this.rejecting.set(null)
    });
  }

  approveRole(r: RoleChangeRequest) {
    this.approvingRole.set(r.requestId);
    this.roleSvc.approve(r.requestId).subscribe({
      next: () => { this.roleRequests.update(rs => rs.filter(x => x.requestId !== r.requestId)); this.toast.success(`${r.user?.name || 'User #' + r.userId} promoted to Organizer!`, 'Role Approved'); this.approvingRole.set(null); },
      error: () => this.approvingRole.set(null)
    });
  }

  rejectRole(r: RoleChangeRequest) {
    this.rejectingRole.set(r.requestId);
    this.roleSvc.reject(r.requestId).subscribe({
      next: () => { this.roleRequests.update(rs => rs.filter(x => x.requestId !== r.requestId)); this.toast.warning(`Role request from ${r.user?.name || 'User #' + r.userId} rejected.`, 'Role Rejected'); this.rejectingRole.set(null); },
      error: () => this.rejectingRole.set(null)
    });
  }
}
