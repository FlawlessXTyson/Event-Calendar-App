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
  template: `
    <div>
      <div style="margin-bottom:28px;">
        <h1 style="font-size:1.5rem;">Admin Dashboard</h1>
        <p>Platform overview and quick actions</p>
      </div>

      <div class="stats-grid">
        <div class="stat-card"><div class="stat-icon" style="background:var(--primary-light);"><span class="material-icons-round" style="color:var(--primary);">event</span></div><div class="stat-value">{{ totalEvents() }}</div><div class="stat-label">Total Events</div></div>
        <div class="stat-card"><div class="stat-icon" style="background:var(--warning-light);"><span class="material-icons-round" style="color:var(--warning);">pending_actions</span></div><div class="stat-value">{{ pendingEvents().length }}</div><div class="stat-label">Pending Approval</div></div>
        <div class="stat-card"><div class="stat-icon" style="background:var(--info-light);"><span class="material-icons-round" style="color:var(--info);">group</span></div><div class="stat-value">{{ users().length }}</div><div class="stat-label">Total Users</div></div>
        <div class="stat-card"><div class="stat-icon" style="background:var(--success-light);"><span class="material-icons-round" style="color:var(--success);">account_balance</span></div><div class="stat-value">₹{{ commission()?.totalCommission | number:'1.0-0' }}</div><div class="stat-label">Platform Revenue</div></div>
      </div>

      <div style="display:grid;grid-template-columns:1fr 1fr;gap:20px;margin-top:8px;" class="dash-grid-2">
        <!-- Pending Events -->
        <div class="card">
          <div class="card-header" style="display:flex;align-items:center;justify-content:space-between;">
            <h3>⏳ Pending Events <span class="badge badge-warning" style="margin-left:8px;">{{ pendingEvents().length }}</span></h3>
            <a routerLink="/admin/events" class="btn btn-ghost btn-sm">View All →</a>
          </div>
          <div class="card-body" style="padding:0;">
            @if (pendingEvents().length === 0) {
              <div class="empty-state" style="padding:24px;"><p>No pending events.</p></div>
            } @else {
              @for (ev of pendingEvents().slice(0,5); track ev.eventId) {
                <div style="padding:12px 16px;border-bottom:1px solid var(--border);display:flex;align-items:center;justify-content:space-between;gap:8px;">
                  <div style="flex:1;min-width:0;">
                    <div style="font-weight:600;font-size:.9rem;" class="truncate">{{ ev.title }}</div>
                    <div style="font-size:.8rem;color:var(--text-muted);">{{ ev.eventDate | date:'MMM d, y' }}</div>
                  </div>
                  <div style="display:flex;gap:6px;flex-shrink:0;">
                    <button type="button" class="btn btn-success btn-sm" [disabled]="approving() === ev.eventId" (click)="approve(ev)">
                      @if (approving() === ev.eventId) { <div class="spinner spinner-sm"></div> } @else { ✓ }
                    </button>
                    <button type="button" class="btn btn-danger btn-sm" [disabled]="rejecting() === ev.eventId" (click)="reject(ev)">
                      @if (rejecting() === ev.eventId) { <div class="spinner spinner-sm"></div> } @else { ✕ }
                    </button>
                  </div>
                </div>
              }
            }
          </div>
        </div>

        <!-- Role Requests -->
        <div class="card">
          <div class="card-header" style="display:flex;align-items:center;justify-content:space-between;">
            <h3>🔑 Role Requests <span class="badge badge-info" style="margin-left:8px;">{{ roleRequests().length }}</span></h3>
            <a routerLink="/admin/role-requests" class="btn btn-ghost btn-sm">View All →</a>
          </div>
          <div class="card-body" style="padding:0;">
            @if (roleRequests().length === 0) {
              <div class="empty-state" style="padding:24px;"><p>No pending role requests.</p></div>
            } @else {
              @for (r of roleRequests().slice(0,5); track r.requestId) {
                <div style="padding:12px 16px;border-bottom:1px solid var(--border);display:flex;align-items:center;justify-content:space-between;gap:8px;">
                  <div>
                    <div style="font-weight:600;font-size:.9rem;">User #{{ r.userId }}</div>
                    <div style="font-size:.8rem;color:var(--text-muted);">Requesting Organizer Role</div>
                  </div>
                  <div style="display:flex;gap:6px;">
                    <button type="button" class="btn btn-success btn-sm" [disabled]="approvingRole() === r.requestId" (click)="approveRole(r)">
                      @if (approvingRole() === r.requestId) { <div class="spinner spinner-sm"></div> } @else { ✓ }
                    </button>
                    <button type="button" class="btn btn-danger btn-sm" [disabled]="rejectingRole() === r.requestId" (click)="rejectRole(r)">
                      @if (rejectingRole() === r.requestId) { <div class="spinner spinner-sm"></div> } @else { ✕ }
                    </button>
                  </div>
                </div>
              }
            }
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`@media(max-width:768px){ .dash-grid-2{ grid-template-columns:1fr!important; } }`]
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
  pendingEvents= () => this.allEvents().filter(e => e.approvalStatus === ApprovalStatus.PENDING);

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
      next: () => { this.roleRequests.update(rs => rs.filter(x => x.requestId !== r.requestId)); this.toast.success(`User #${r.userId} promoted to Organizer!`, 'Role Approved'); this.approvingRole.set(null); },
      error: () => this.approvingRole.set(null)
    });
  }

  rejectRole(r: RoleChangeRequest) {
    this.rejectingRole.set(r.requestId);
    this.roleSvc.reject(r.requestId).subscribe({
      next: () => { this.roleRequests.update(rs => rs.filter(x => x.requestId !== r.requestId)); this.toast.warning(`Role request from User #${r.userId} rejected.`, 'Role Rejected'); this.rejectingRole.set(null); },
      error: () => this.rejectingRole.set(null)
    });
  }
}
