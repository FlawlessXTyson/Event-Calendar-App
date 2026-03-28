import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EventService } from '../../../core/services/event.service';
import { ToastService } from '../../../core/services/toast.service';
import { EventResponse, ApprovalStatus, EventStatus } from '../../../core/models/models';

type FilterTab = 'pending' | 'approved' | 'rejected' | 'all' | 'expired';

@Component({
  selector: 'app-admin-events',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div>
      <div style="margin-bottom:24px;"><h1 style="font-size:1.5rem;">Event Management</h1><p>Review, approve, reject, and manage all events</p></div>

      <div class="tabs">
        <button type="button" class="tab-btn" [class.active]="activeTab() === 'pending'" (click)="setTab('pending')">
          Pending
          @if (pendingCount() > 0) {
            <span class="badge badge-warning" style="margin-left:6px;">{{ pendingCount() }}</span>
          }
        </button>
        <button type="button" class="tab-btn" [class.active]="activeTab() === 'approved'" (click)="setTab('approved')">
          Approved
          @if (approvedCount() > 0) {
            <span class="badge badge-success" style="margin-left:6px;">{{ approvedCount() }}</span>
          }
        </button>
        <button type="button" class="tab-btn" [class.active]="activeTab() === 'rejected'" (click)="setTab('rejected')">
          Rejected
          @if (rejectedCount() > 0) {
            <span class="badge badge-danger" style="margin-left:6px;">{{ rejectedCount() }}</span>
          }
        </button>
        <button type="button" class="tab-btn" [class.active]="activeTab() === 'all'" (click)="setTab('all')">
          All
          @if (allCount() > 0) {
            <span class="badge badge-gray" style="margin-left:6px;">{{ allCount() }}</span>
          }
        </button>
        <button type="button" class="tab-btn" [class.active]="activeTab() === 'expired'" (click)="setTab('expired')">
          ⏰ Expired
          @if (expiredCount() > 0) {
            <span class="badge badge-gray" style="margin-left:6px;">{{ expiredCount() }}</span>
          }
        </button>
      </div>

      <!-- Search bar — only shown on All tab -->
      @if (activeTab() === 'all') {
        <div style="margin-bottom:16px;position:relative;max-width:360px;">
          <span class="material-icons-round" style="position:absolute;left:10px;top:50%;transform:translateY(-50%);color:var(--text-muted);font-size:20px;">search</span>
          <input [(ngModel)]="allSearch" type="search" class="form-control" placeholder="Search events by name…" style="padding-left:38px;" />
        </div>
      }

      @if (loading()) { <div class="loading-center"><div class="spinner"></div></div> }
      @else if (filtered().length === 0) {
        <div class="empty-state"><span class="material-icons-round empty-icon">event_busy</span><h3>No events in this category</h3></div>
      } @else {
        <div class="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>Title</th><th>Date</th><th>Organizer</th><th>Type</th><th>Approval</th>
                @if (activeTab() === 'expired') { <th>Registrations</th><th>Revenue</th> }
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              @for (ev of filtered(); track ev.eventId) {
                <tr>
                  <td><div style="font-weight:600;max-width:200px;" class="truncate">{{ ev.title }}</div><div style="font-size:.78rem;color:var(--text-muted);">{{ ev.location }}</div></td>
                  <td style="white-space:nowrap;color:var(--text-muted);">{{ ev.eventDate | date:'MMM d, y' }}</td>
                  <td style="color:var(--text-muted);">
                    @if (ev.organizerName) {
                      <div style="font-weight:600;font-size:.85rem;">{{ ev.organizerName }}</div>
                    } @else {
                      <span style="color:var(--text-muted);">ID #{{ ev.createdByUserId }}</span>
                    }
                  </td>
                  <td>@if (ev.isPaidEvent) { <span class="badge badge-warning">₹{{ ev.ticketPrice | number:'1.0-0' }}</span> } @else { <span class="badge badge-success">Free</span> }</td>
                  <td><span class="badge" [class]="approvalBadge(ev.approvalStatus)">{{ approvalLabel(ev.approvalStatus) }}</span></td>
                  @if (activeTab() === 'expired') {
                    <td style="color:var(--text-muted);">{{ ev.seatsLeft !== undefined && ev.seatsLeft >= 0 ? (ev.seatsLimit ?? 0) - ev.seatsLeft : '—' }} registered</td>
                    <td style="font-weight:600;color:var(--primary);">{{ ev.isPaidEvent ? '₹' + ((ev.seatsLimit ?? 0) - (ev.seatsLeft ?? 0)) * ev.ticketPrice : 'Free' }}</td>
                  }
                  <td>
                    <div style="display:flex;gap:6px;flex-wrap:wrap;">
                      @if (activeTab() === 'expired') {
                        <!-- Expired events: no approve/reject/cancel — only label + delete -->
                        <span style="font-size:.75rem;color:var(--text-muted);font-style:italic;padding:4px 0;">Event Expired</span>
                      } @else {
                        @if (ev.approvalStatus === ApprovalStatus.PENDING) {
                          <button type="button" class="btn btn-success btn-sm" [disabled]="approving() === ev.eventId" (click)="approve(ev)">
                            @if (approving() === ev.eventId) { <div class="spinner spinner-sm"></div> } @else { <span class="material-icons-round" style="font-size:15px;">check</span> Approve }
                          </button>
                          <button type="button" class="btn btn-danger btn-sm" [disabled]="rejecting() === ev.eventId" (click)="reject(ev)">
                            @if (rejecting() === ev.eventId) { <div class="spinner spinner-sm"></div> } @else { <span class="material-icons-round" style="font-size:15px;">close</span> Reject }
                          </button>
                        }
                        @if ((ev.status ?? 1) === EventStatus.ACTIVE && ev.approvalStatus === ApprovalStatus.APPROVED && !isExpiredEvent(ev)) {
                          <button type="button" class="btn btn-warning btn-sm" [disabled]="cancelling() === ev.eventId" (click)="cancelEv(ev)">
                            @if (cancelling() === ev.eventId) { <div class="spinner spinner-sm"></div> } @else { Cancel }
                          </button>
                        }
                      }
                      <button type="button" class="btn btn-ghost btn-sm btn-icon" [disabled]="deleting() === ev.eventId" (click)="deleteEv(ev)" title="Delete permanently">
                        @if (deleting() === ev.eventId) { <div class="spinner spinner-sm"></div> }
                        @else { <span class="material-icons-round" style="color:var(--danger);font-size:18px;">delete</span> }
                      </button>
                    </div>
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
export class AdminEventsComponent implements OnInit {
  private eventSvc = inject(EventService);
  private toast    = inject(ToastService);
  ApprovalStatus   = ApprovalStatus;
  EventStatus      = EventStatus;

  events     = signal<EventResponse[]>([]);
  expired    = signal<EventResponse[]>([]);
  loading    = signal(true);
  approving  = signal<number | null>(null);
  rejecting  = signal<number | null>(null);
  cancelling = signal<number | null>(null);
  deleting   = signal<number | null>(null);
  activeTab  = signal<FilterTab>('pending');
  allSearch  = '';

  pendingCount  = computed(() => this.events().filter(e => e.approvalStatus === ApprovalStatus.PENDING).length);
  approvedCount = computed(() => this.events().filter(e => e.approvalStatus === ApprovalStatus.APPROVED).length);
  rejectedCount = computed(() => this.events().filter(e => e.approvalStatus === ApprovalStatus.REJECTED).length);
  allCount      = computed(() => this.events().length);
  expiredCount  = computed(() => this.expired().length);

  filtered = computed(() => {
    const t = this.activeTab();
    if (t === 'expired')  return this.expired();
    const base =
      t === 'pending'  ? this.events().filter(e => e.approvalStatus === ApprovalStatus.PENDING) :
      t === 'approved' ? this.events().filter(e => e.approvalStatus === ApprovalStatus.APPROVED) :
      t === 'rejected' ? this.events().filter(e => e.approvalStatus === ApprovalStatus.REJECTED) :
      this.events();
    if (t === 'all' && this.allSearch.trim()) {
      const s = this.allSearch.toLowerCase();
      return base.filter(e => e.title.toLowerCase().includes(s) || (e.location ?? '').toLowerCase().includes(s));
    }
    return base;
  });

  isExpiredEvent(ev: EventResponse): boolean {
    // Only use hasEnded (event fully over) — NOT isRegistrationOpen
    // isRegistrationOpen becomes false once event starts, but admin still needs to approve/reject
    if (ev.hasEnded !== undefined) return ev.hasEnded;
    const endDate = ev.eventEndDate ? new Date(ev.eventEndDate) : new Date(ev.eventDate);
    endDate.setHours(23, 59, 59, 999);
    return endDate < new Date();
  }

  ngOnInit() { this.loadAll(); }

  loadAll() {
    this.loading.set(true);
    this.eventSvc.getPending().subscribe({
      next: pending => {
        this.eventSvc.getApproved().subscribe({
          next: approved => {
            this.eventSvc.getRejected().subscribe({
              next: rejected => {
                // Pending events whose end time has passed → auto-move to Expired
                const activePending  = pending.filter(e => !e.hasEnded);
                const expiredPending = pending.filter(e =>  e.hasEnded);

                const map = new Map<number, EventResponse>();
                [...activePending, ...approved, ...rejected].forEach(e => map.set(e.eventId, e));
                this.events.set([...map.values()]);

                // Merge expired-pending with backend expired (deduplicated)
                this.eventSvc.getExpired().subscribe({
                  next: backendExpired => {
                    const expMap = new Map<number, EventResponse>();
                    [...backendExpired, ...expiredPending].forEach(e => expMap.set(e.eventId, e));
                    this.expired.set([...expMap.values()]);
                  },
                  error: () => {
                    this.expired.set(expiredPending);
                  }
                });

                this.loading.set(false);
              },
              error: () => {
                const activePending = pending.filter(e => !e.hasEnded);
                this.events.set([...activePending, ...approved]);
                this.loading.set(false);
              }
            });
          },
          error: () => {
            this.events.set(pending.filter(e => !e.hasEnded));
            this.loading.set(false);
          }
        });
      },
      error: () => this.loading.set(false)
    });
  }

  setTab(t: FilterTab) { this.activeTab.set(t); }

  approve(ev: EventResponse) {
    this.approving.set(ev.eventId);
    this.eventSvc.approve(ev.eventId).subscribe({
      next: updated => {
        this.events.update(es => es.map(e => e.eventId === ev.eventId ? { ...e, approvalStatus: ApprovalStatus.APPROVED } : e));
        this.toast.success(`"${ev.title}" is now live and visible to all users!`, 'Event Approved');
        this.approving.set(null);
      },
      error: () => this.approving.set(null)
    });
  }

  reject(ev: EventResponse) {
    this.rejecting.set(ev.eventId);
    this.eventSvc.reject(ev.eventId).subscribe({
      next: () => {
        this.events.update(es => es.map(e => e.eventId === ev.eventId ? { ...e, approvalStatus: ApprovalStatus.REJECTED } : e));
        this.toast.warning(`"${ev.title}" has been rejected. The organizer will be notified.`, 'Event Rejected');
        this.rejecting.set(null);
      },
      error: () => this.rejecting.set(null)
    });
  }

  cancelEv(ev: EventResponse) {
    if (!confirm(`Cancel "${ev.title}"?\n\nAll paid attendees will be automatically refunded.`)) return;
    this.cancelling.set(ev.eventId);
    this.eventSvc.cancel(ev.eventId).subscribe({
      next: () => {
        this.events.update(es => es.map(e => e.eventId === ev.eventId ? { ...e, status: EventStatus.CANCELLED } : e));
        this.toast.success(`Event cancelled. All applicable payments have been refunded.`, 'Event Cancelled');
        this.cancelling.set(null);
      },
      error: () => this.cancelling.set(null)
    });
  }

  deleteEv(ev: EventResponse) {
    if (!confirm(`Permanently delete "${ev.title}"?\n\nThis action CANNOT be undone.`)) return;
    this.deleting.set(ev.eventId);
    this.eventSvc.delete(ev.eventId).subscribe({
      next: () => {
        this.events.update(es => es.filter(e => e.eventId !== ev.eventId));
        this.expired.update(es => es.filter(e => e.eventId !== ev.eventId));
        this.toast.success(`"${ev.title}" permanently deleted.`, 'Event Deleted');
        this.deleting.set(null);
      },
      error: () => this.deleting.set(null)
    });
  }

  approvalLabel(s: ApprovalStatus) { return { 1: 'Pending', 2: 'Approved', 3: 'Rejected' }[s] ?? s; }
  approvalBadge(s: ApprovalStatus) { return { 1: 'badge-warning', 2: 'badge-success', 3: 'badge-danger' }[s] ?? 'badge-gray'; }
}
