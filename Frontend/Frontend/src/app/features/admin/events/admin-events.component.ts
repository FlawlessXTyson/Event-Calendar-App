import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EventService } from '../../../core/services/event.service';
import { ToastService } from '../../../core/services/toast.service';
import { EventResponse, ApprovalStatus, EventStatus } from '../../../core/models/models';

type FilterTab = 'pending' | 'approved' | 'rejected' | 'all' | 'expired' | 'cancelled';

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
        <button type="button" class="tab-btn" [class.active]="activeTab() === 'cancelled'" (click)="setTab('cancelled')">
          🚫 Cancelled
          @if (cancelledCount() > 0) {
            <span class="badge badge-danger" style="margin-left:6px;">{{ cancelledCount() }}</span>
          }
        </button>
      </div>

      <!-- Search bar — only shown on All tab -->
      @if (activeTab() === 'all') {
        <div style="margin-bottom:16px;position:relative;max-width:360px;">
          <span class="material-icons-round" style="position:absolute;left:10px;top:50%;transform:translateY(-50%);color:var(--text-muted);font-size:20px;">search</span>
          <input [(ngModel)]="allSearch" type="search" class="form-control" placeholder="Search events by name…"
            style="padding-left:38px;"
            (ngModelChange)="onSearch()" />
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
                        <span style="font-size:.75rem;color:var(--text-muted);font-style:italic;padding:4px 0;">Event Expired</span>
                      } @else if (activeTab() === 'cancelled') {
                        <span class="badge badge-danger" style="font-size:.75rem;">Cancelled</span>
                        <button type="button" class="btn btn-ghost btn-sm" (click)="viewCancelDetails(ev)" title="View cancellation details">
                          <span class="material-icons-round" style="font-size:16px;color:var(--primary);">info</span>
                        </button>
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
                        @if ((ev.status ?? 1) === EventStatus.CANCELLED) {
                          <span class="badge badge-danger" style="font-size:.75rem;">Cancelled</span>
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

      <!-- Pagination -->
      @if (totalPages() > 1) {
        <div style="display:flex;align-items:center;justify-content:space-between;padding:14px 0;flex-wrap:wrap;gap:10px;">
          <div style="font-size:.82rem;color:var(--text-muted);">
            Page {{ page() }} of {{ totalPages() }} &nbsp;·&nbsp; {{ total() }} total
          </div>
          <div style="display:flex;gap:6px;">
            <button type="button" class="btn btn-ghost btn-sm" [disabled]="page() <= 1" (click)="goPage(1)">
              <span class="material-icons-round" style="font-size:16px;">first_page</span>
            </button>
            <button type="button" class="btn btn-ghost btn-sm" [disabled]="page() <= 1" (click)="goPage(page() - 1)">
              <span class="material-icons-round" style="font-size:16px;">chevron_left</span>
            </button>
            @for (p of pageNums(); track p) {
              <button type="button" class="btn btn-sm"
                [class]="p === page() ? 'btn-primary' : 'btn-ghost'"
                (click)="goPage(p)">{{ p }}</button>
            }
            <button type="button" class="btn btn-ghost btn-sm" [disabled]="page() >= totalPages()" (click)="goPage(page() + 1)">
              <span class="material-icons-round" style="font-size:16px;">chevron_right</span>
            </button>
            <button type="button" class="btn btn-ghost btn-sm" [disabled]="page() >= totalPages()" (click)="goPage(totalPages())">
              <span class="material-icons-round" style="font-size:16px;">last_page</span>
            </button>
          </div>
        </div>
      }
    </div>

    <!-- Cancellation Details Modal -->
    @if (cancelDetails()) {
      <div class="modal-backdrop" (click)="cancelDetails.set(null)">
        <div class="modal" (click)="$event.stopPropagation()" style="max-width:500px;">
          <div class="modal-header" style="background:linear-gradient(135deg,#FEE2E2,#FECACA);border-radius:var(--r-lg) var(--r-lg) 0 0;">
            <div style="display:flex;align-items:center;gap:10px;">
              <span class="material-icons-round" style="color:#DC2626;font-size:24px;">cancel</span>
              <div>
                <div style="font-weight:700;font-size:1rem;color:#7F1D1D;">Cancellation Details</div>
                <div style="font-size:.82rem;color:#991B1B;">{{ cancelDetails()!.title }}</div>
              </div>
            </div>
            <button type="button" class="btn btn-ghost btn-icon btn-sm" (click)="cancelDetails.set(null)">
              <span class="material-icons-round">close</span>
            </button>
          </div>
          <div class="modal-body" style="padding:24px;">
            @if (cancelDetailsLoading()) {
              <div class="loading-center"><div class="spinner"></div></div>
            } @else if (cancelDetailsData()) {
              <div style="display:grid;grid-template-columns:1fr 1fr;gap:12px;">
                <div style="background:var(--surface-2);border-radius:var(--r-sm);padding:14px;text-align:center;">
                  <div style="font-size:.72rem;font-weight:700;color:var(--text-muted);text-transform:uppercase;margin-bottom:6px;">Users Refunded</div>
                  <div style="font-size:1.6rem;font-weight:800;">{{ cancelDetailsData()!.totalUsersRefunded }}</div>
                </div>
                <div style="background:#FEE2E2;border-radius:var(--r-sm);padding:14px;text-align:center;">
                  <div style="font-size:.72rem;font-weight:700;color:#991B1B;text-transform:uppercase;margin-bottom:6px;">Total Refunded to Users</div>
                  <div style="font-size:1.6rem;font-weight:800;color:#DC2626;">&#8377;{{ cancelDetailsData()!.totalRefundAmount | number:'1.0-0' }}</div>
                </div>
                <div style="background:#D1FAE5;border-radius:var(--r-sm);padding:14px;text-align:center;">
                  <div style="font-size:.72rem;font-weight:700;color:#065F46;text-transform:uppercase;margin-bottom:6px;">Organizer Compensation</div>
                  <div style="font-size:1.6rem;font-weight:800;color:var(--success);">&#8377;{{ cancelDetailsData()!.organizerCompensation | number:'1.0-0' }}</div>
                  <div style="font-size:.72rem;color:var(--text-muted);margin-top:2px;">50% × ticket × users (if &lt;48h)</div>
                </div>
                <div style="background:#FEF3C7;border-radius:var(--r-sm);padding:14px;text-align:center;">
                  <div style="font-size:.72rem;font-weight:700;color:#92400E;text-transform:uppercase;margin-bottom:6px;">Admin Total Spent</div>
                  <div style="font-size:1.6rem;font-weight:800;color:#D97706;">&#8377;{{ cancelDetailsData()!.adminTotalSpent | number:'1.0-0' }}</div>
                  <div style="font-size:.72rem;color:var(--text-muted);margin-top:2px;">Refunds + compensation</div>
                </div>
              </div>
            }
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-ghost" (click)="cancelDetails.set(null)">Close</button>
          </div>
        </div>
      </div>
    }
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

  // Pagination per tab
  page      = signal(1);
  pageSize  = 10;
  total     = signal(0);
  totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));
  pageNums   = computed(() => {
    const t = this.totalPages(), c = this.page();
    const pages: number[] = [];
    for (let i = Math.max(1, c - 2); i <= Math.min(t, c + 2); i++) pages.push(i);
    return pages;
  });

  // Per-tab totals (loaded on init)
  tabTotals = signal<Record<string, number>>({ pending: 0, approved: 0, rejected: 0, all: 0, expired: 0, cancelled: 0 });

  pendingCount   = computed(() => this.tabTotals()['pending']   ?? 0);
  approvedCount  = computed(() => this.tabTotals()['approved']  ?? 0);
  rejectedCount  = computed(() => this.tabTotals()['rejected']  ?? 0);
  allCount       = computed(() => this.tabTotals()['all']       ?? 0);
  expiredCount   = computed(() => this.tabTotals()['expired']   ?? 0);
  cancelledCount = computed(() => this.tabTotals()['cancelled'] ?? 0);

  // Cancellation details modal
  cancelDetails        = signal<EventResponse | null>(null);
  cancelDetailsLoading = signal(false);
  cancelDetailsData    = signal<{ totalUsersRefunded: number; totalRefundAmount: number; organizerCompensation: number; adminTotalSpent: number } | null>(null);

  filtered = computed(() => {
    const t = this.activeTab();
    if (t === 'expired')   return this.expired();
    if (t === 'cancelled') return this.events();
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
    if (ev.hasEnded !== undefined) return ev.hasEnded;
    const endDate = ev.eventEndDate ? new Date(ev.eventEndDate) : new Date(ev.eventDate);
    endDate.setHours(23, 59, 59, 999);
    return endDate < new Date();
  }

  ngOnInit() {
    this.loadTabTotals();
    this.loadTab();
  }

  /** Load total counts for all tabs (for badge display) */
  loadTabTotals() {
    this.eventSvc.getPendingPaged(1, 1).subscribe({ next: r => this.tabTotals.update(t => ({ ...t, pending: r.totalRecords })), error: () => {} });
    this.eventSvc.getApprovedPaged(1, 1).subscribe({ next: r => this.tabTotals.update(t => ({ ...t, approved: r.totalRecords })), error: () => {} });
    this.eventSvc.getRejectedPaged(1, 1).subscribe({ next: r => this.tabTotals.update(t => ({ ...t, rejected: r.totalRecords })), error: () => {} });
    this.eventSvc.getExpiredPaged(1, 1).subscribe({ next: r => this.tabTotals.update(t => ({ ...t, expired: r.totalRecords })), error: () => {} });
    this.eventSvc.getAllPaged(1, 1).subscribe({ next: r => this.tabTotals.update(t => ({ ...t, all: r.totalRecords })), error: () => {} });
    this.eventSvc.getCancelledPaged(1, 1).subscribe({ next: r => this.tabTotals.update(t => ({ ...t, cancelled: r.totalRecords })), error: () => {} });
  }

  setTab(t: FilterTab) { this.activeTab.set(t); this.page.set(1); this.loadTab(); }

  goPage(p: number) { this.page.set(p); this.loadTab(); }

  onSearch() { this.page.set(1); this.loadTab(); }

  loadTab() {
    this.loading.set(true);
    const t = this.activeTab();
    const p = this.page(), s = this.pageSize;

    if (t === 'pending') {
      this.eventSvc.getPendingPaged(p, s).subscribe({
        next: res => {
          // Backend already filters upcoming-only pending events
          this.events.set(res.data);
          this.total.set(res.totalRecords);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
    } else if (t === 'approved') {
      this.eventSvc.getApprovedPaged(p, s).subscribe({
        next: res => { this.events.set(res.data); this.total.set(res.totalRecords); this.loading.set(false); },
        error: () => this.loading.set(false)
      });
    } else if (t === 'rejected') {
      this.eventSvc.getRejectedPaged(p, s).subscribe({
        next: res => { this.events.set(res.data); this.total.set(res.totalRecords); this.loading.set(false); },
        error: () => this.loading.set(false)
      });
    } else if (t === 'expired') {
      this.eventSvc.getExpiredPaged(p, s).subscribe({
        next: res => { this.expired.set(res.data); this.total.set(res.totalRecords); this.loading.set(false); },
        error: () => this.loading.set(false)
      });
    } else if (t === 'cancelled') {
      this.eventSvc.getCancelledPaged(p, s).subscribe({
        next: res => { this.events.set(res.data); this.total.set(res.totalRecords); this.loading.set(false); },
        error: () => this.loading.set(false)
      });
    } else {
      // 'all' tab — paginated with optional search
      this.eventSvc.getAllPaged(p, s, this.allSearch.trim() || undefined).subscribe({
        next: res => {
          this.events.set(res.data);
          this.total.set(res.totalRecords);
          this.tabTotals.update(tt => ({ ...tt, all: res.totalRecords }));
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
    }
  }

  loadAll() { this.loadTab(); }

  approve(ev: EventResponse) {
    this.approving.set(ev.eventId);
    this.eventSvc.approve(ev.eventId).subscribe({
      next: () => {
        this.toast.success(`"${ev.title}" is now live!`, 'Event Approved');
        this.approving.set(null);
        this.loadTabTotals();
        this.loadTab();
      },
      error: () => this.approving.set(null)
    });
  }

  reject(ev: EventResponse) {
    this.rejecting.set(ev.eventId);
    this.eventSvc.reject(ev.eventId).subscribe({
      next: () => {
        this.toast.warning(`"${ev.title}" has been rejected.`, 'Event Rejected');
        this.rejecting.set(null);
        this.loadTabTotals();
        this.loadTab();
      },
      error: () => this.rejecting.set(null)
    });
  }

  cancelEv(ev: EventResponse) {
    if (!confirm(`Cancel "${ev.title}"?\n\nAll paid attendees will be automatically refunded.`)) return;
    this.cancelling.set(ev.eventId);
    this.eventSvc.cancel(ev.eventId).subscribe({
      next: () => {
        this.toast.success(`Event cancelled. All applicable payments have been refunded.`, 'Event Cancelled');
        this.cancelling.set(null);
        this.loadTabTotals();
        this.loadTab();
      },
      error: () => this.cancelling.set(null)
    });
  }

  deleteEv(ev: EventResponse) {
    if (!confirm(`Permanently delete "${ev.title}"?\n\nThis action CANNOT be undone.`)) return;
    this.deleting.set(ev.eventId);
    this.eventSvc.delete(ev.eventId).subscribe({
      next: () => {
        this.toast.success(`"${ev.title}" permanently deleted.`, 'Event Deleted');
        this.deleting.set(null);
        this.loadTabTotals();
        this.loadTab();
      },
      error: () => this.deleting.set(null)
    });
  }

  viewCancelDetails(ev: EventResponse) {
    this.cancelDetails.set(ev);
    this.cancelDetailsLoading.set(true);
    this.cancelDetailsData.set(null);
    this.eventSvc.getRefundSummary(ev.eventId).subscribe({
      next: s => {
        // Compensation = 50% of ticket price × users refunded (only if paid event)
        const compensation = ev.isPaidEvent ? ev.ticketPrice * 0.5 * s.totalUsersRefunded : 0;
        const adminTotalSpent = s.totalRefundAmount + compensation;
        this.cancelDetailsData.set({
          totalUsersRefunded:   s.totalUsersRefunded,
          totalRefundAmount:    s.totalRefundAmount,
          organizerCompensation: compensation,
          adminTotalSpent:      adminTotalSpent
        });
        this.cancelDetailsLoading.set(false);
      },
      error: () => this.cancelDetailsLoading.set(false)
    });
  }

  approvalLabel(s: ApprovalStatus) { return { 1: 'Pending', 2: 'Approved', 3: 'Rejected' }[s] ?? s; }
  approvalBadge(s: ApprovalStatus) { return { 1: 'badge-warning', 2: 'badge-success', 3: 'badge-danger' }[s] ?? 'badge-gray'; }
}
