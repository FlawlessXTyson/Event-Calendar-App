import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EventService } from '../../../core/services/event.service';
import { ToastService } from '../../../core/services/toast.service';
import { EventResponse, ApprovalStatus, EventStatus } from '../../../core/models/models';

type FilterTab = 'pending' | 'approved' | 'rejected' | 'all';

@Component({
  selector: 'app-admin-events',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div>
      <div style="margin-bottom:24px;"><h1 style="font-size:1.5rem;">Event Management</h1><p>Review, approve, reject, and manage all events</p></div>

      <div class="tabs">
        @for (t of tabs; track t.key) {
          <button type="button" class="tab-btn" [class.active]="activeTab() === t.key" (click)="setTab(t.key)">
            {{ t.label }}
            @if (t.key === 'pending' && pendingCount() > 0) { <span class="badge badge-warning" style="margin-left:6px;">{{ pendingCount() }}</span> }
          </button>
        }
      </div>

      @if (loading()) { <div class="loading-center"><div class="spinner"></div></div> }
      @else if (filtered().length === 0) {
        <div class="empty-state"><span class="material-icons-round empty-icon">event_busy</span><h3>No events in this category</h3></div>
      } @else {
        <div class="table-wrapper">
          <table>
            <thead><tr><th>Title</th><th>Date</th><th>Organizer</th><th>Type</th><th>Approval</th><th>Actions</th></tr></thead>
            <tbody>
              @for (ev of filtered(); track ev.eventId) {
                <tr>
                  <td><div style="font-weight:600;max-width:200px;" class="truncate">{{ ev.title }}</div><div style="font-size:.78rem;color:var(--text-muted);">{{ ev.location }}</div></td>
                  <td style="white-space:nowrap;color:var(--text-muted);">{{ ev.eventDate | date:'MMM d, y' }}</td>
                  <td style="color:var(--text-muted);">ID #{{ ev.createdByUserId }}</td>
                  <td>@if (ev.isPaidEvent) { <span class="badge badge-warning">₹{{ ev.ticketPrice | number:'1.0-0' }}</span> } @else { <span class="badge badge-success">Free</span> }</td>
                  <td><span class="badge" [class]="approvalBadge(ev.approvalStatus)">{{ approvalLabel(ev.approvalStatus) }}</span></td>
                  <td>
                    <div style="display:flex;gap:6px;flex-wrap:wrap;">
                      @if (ev.approvalStatus === ApprovalStatus.PENDING) {
                        <button type="button" class="btn btn-success btn-sm" [disabled]="approving() === ev.eventId" (click)="approve(ev)">
                          @if (approving() === ev.eventId) { <div class="spinner spinner-sm"></div> } @else { <span class="material-icons-round" style="font-size:15px;">check</span> Approve }
                        </button>
                        <button type="button" class="btn btn-danger btn-sm" [disabled]="rejecting() === ev.eventId" (click)="reject(ev)">
                          @if (rejecting() === ev.eventId) { <div class="spinner spinner-sm"></div> } @else { <span class="material-icons-round" style="font-size:15px;">close</span> Reject }
                        </button>
                      }
                      @if ((ev.status ?? 1) === EventStatus.ACTIVE && ev.approvalStatus === ApprovalStatus.APPROVED) {
                        <button type="button" class="btn btn-warning btn-sm" [disabled]="cancelling() === ev.eventId" (click)="cancelEv(ev)">
                          @if (cancelling() === ev.eventId) { <div class="spinner spinner-sm"></div> } @else { Cancel }
                        </button>
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
  loading    = signal(true);
  approving  = signal<number | null>(null);
  rejecting  = signal<number | null>(null);
  cancelling = signal<number | null>(null);
  deleting   = signal<number | null>(null);
  activeTab  = signal<FilterTab>('pending');

  tabs = [
    { key: 'pending'  as FilterTab, label: 'Pending' },
    { key: 'approved' as FilterTab, label: 'Approved' },
    { key: 'rejected' as FilterTab, label: 'Rejected' },
    { key: 'all'      as FilterTab, label: 'All' },
  ];

  pendingCount = () => this.events().filter(e => e.approvalStatus === ApprovalStatus.PENDING).length;

  filtered = computed(() => {
    const t = this.activeTab();
    if (t === 'pending')  return this.events().filter(e => e.approvalStatus === ApprovalStatus.PENDING);
    if (t === 'approved') return this.events().filter(e => e.approvalStatus === ApprovalStatus.APPROVED);
    if (t === 'rejected') return this.events().filter(e => e.approvalStatus === ApprovalStatus.REJECTED);
    return this.events();
  });

  ngOnInit() { this.loadAll(); }

  loadAll() {
    this.loading.set(true);
    // Load pending first (primary workflow), then fetch all for the 'all' tab
    this.eventSvc.getPending().subscribe({
      next: pending => {
        this.eventSvc.getApproved().subscribe({
          next: approved => {
            this.eventSvc.getRejected().subscribe({
              next: rejected => {
                const map = new Map<number, EventResponse>();
                [...pending, ...approved, ...rejected].forEach(e => map.set(e.eventId, e));
                this.events.set([...map.values()]);
                this.loading.set(false);
              },
              error: () => {
                this.events.set([...pending, ...approved]);
                this.loading.set(false);
              }
            });
          },
          error: () => { this.events.set(pending); this.loading.set(false); }
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
        this.toast.success(`"${ev.title}" permanently deleted.`, 'Event Deleted');
        this.deleting.set(null);
      },
      error: () => this.deleting.set(null)
    });
  }

  approvalLabel(s: ApprovalStatus) { return { 1: 'Pending', 2: 'Approved', 3: 'Rejected' }[s] ?? s; }
  approvalBadge(s: ApprovalStatus) { return { 1: 'badge-warning', 2: 'badge-success', 3: 'badge-danger' }[s] ?? 'badge-gray'; }
}
