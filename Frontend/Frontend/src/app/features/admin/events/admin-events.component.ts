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
  templateUrl: './admin-events.component.html',
  styleUrl: './admin-events.component.css'
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

  tabTotals = signal<Record<string, number>>({ pending: 0, approved: 0, rejected: 0, all: 0, expired: 0, cancelled: 0 });
  pendingCount   = computed(() => this.tabTotals()['pending']   ?? 0);
  approvedCount  = computed(() => this.tabTotals()['approved']  ?? 0);
  rejectedCount  = computed(() => this.tabTotals()['rejected']  ?? 0);
  allCount       = computed(() => this.tabTotals()['all']       ?? 0);
  expiredCount   = computed(() => this.tabTotals()['expired']   ?? 0);
  cancelledCount = computed(() => this.tabTotals()['cancelled'] ?? 0);

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

  ngOnInit() { this.loadTabTotals(); this.loadTab(); }

  loadTabTotals() {
    this.eventSvc.getPendingPaged(1, 1).subscribe({ next: r => this.tabTotals.update(t => ({ ...t, pending: r.totalRecords })), error: () => {} });
    this.eventSvc.getApprovedPaged(1, 1).subscribe({ next: r => this.tabTotals.update(t => ({ ...t, approved: r.totalRecords })), error: () => {} });
    this.eventSvc.getRejectedPaged(1, 1).subscribe({ next: r => this.tabTotals.update(t => ({ ...t, rejected: r.totalRecords })), error: () => {} });
    this.eventSvc.getExpiredPaged(1, 1).subscribe({ next: r => this.tabTotals.update(t => ({ ...t, expired: r.totalRecords })), error: () => {} });
    this.eventSvc.getAllPaged(1, 1).subscribe({ next: r => this.tabTotals.update(t => ({ ...t, all: r.totalRecords })), error: () => {} });
    this.eventSvc.getCancelledPaged(1, 1).subscribe({ next: r => this.tabTotals.update(t => ({ ...t, cancelled: r.totalRecords })), error: () => {} });
  }

  setTab(t: FilterTab) { this.activeTab.set(t); this.page.set(1); this.loadTab(); }
  goPage(p: number)    { this.page.set(p); this.loadTab(); }
  onSearch()           { this.page.set(1); this.loadTab(); }
  loadAll()            { this.loadTab(); }

  loadTab() {
    this.loading.set(true);
    const t = this.activeTab(), p = this.page(), s = this.pageSize;
    if (t === 'pending') {
      this.eventSvc.getPendingPaged(p, s).subscribe({ next: r => { this.events.set(r.data); this.total.set(r.totalRecords); this.loading.set(false); }, error: () => this.loading.set(false) });
    } else if (t === 'approved') {
      this.eventSvc.getApprovedPaged(p, s).subscribe({ next: r => { this.events.set(r.data); this.total.set(r.totalRecords); this.loading.set(false); }, error: () => this.loading.set(false) });
    } else if (t === 'rejected') {
      this.eventSvc.getRejectedPaged(p, s).subscribe({ next: r => { this.events.set(r.data); this.total.set(r.totalRecords); this.loading.set(false); }, error: () => this.loading.set(false) });
    } else if (t === 'expired') {
      this.eventSvc.getExpiredPaged(p, s).subscribe({ next: r => { this.expired.set(r.data); this.total.set(r.totalRecords); this.loading.set(false); }, error: () => this.loading.set(false) });
    } else if (t === 'cancelled') {
      this.eventSvc.getCancelledPaged(p, s).subscribe({ next: r => { this.events.set(r.data); this.total.set(r.totalRecords); this.loading.set(false); }, error: () => this.loading.set(false) });
    } else {
      this.eventSvc.getAllPaged(p, s, this.allSearch.trim() || undefined).subscribe({
        next: r => { this.events.set(r.data); this.total.set(r.totalRecords); this.tabTotals.update(tt => ({ ...tt, all: r.totalRecords })); this.loading.set(false); },
        error: () => this.loading.set(false)
      });
    }
  }

  approve(ev: EventResponse) {
    this.approving.set(ev.eventId);
    this.eventSvc.approve(ev.eventId).subscribe({
      next: () => { this.toast.success(`"${ev.title}" is now live!`, 'Event Approved'); this.approving.set(null); this.loadTabTotals(); this.loadTab(); },
      error: () => this.approving.set(null)
    });
  }

  reject(ev: EventResponse) {
    this.rejecting.set(ev.eventId);
    this.eventSvc.reject(ev.eventId).subscribe({
      next: () => { this.toast.warning(`"${ev.title}" has been rejected.`, 'Event Rejected'); this.rejecting.set(null); this.loadTabTotals(); this.loadTab(); },
      error: () => this.rejecting.set(null)
    });
  }

  cancelEv(ev: EventResponse) {
    if (!confirm(`Cancel "${ev.title}"?\n\nAll paid attendees will be automatically refunded.`)) return;
    this.cancelling.set(ev.eventId);
    this.eventSvc.cancel(ev.eventId).subscribe({
      next: () => { this.toast.success(`Event cancelled. All applicable payments have been refunded.`, 'Event Cancelled'); this.cancelling.set(null); this.loadTabTotals(); this.loadTab(); },
      error: () => this.cancelling.set(null)
    });
  }

  deleteEv(ev: EventResponse) {
    if (!confirm(`Permanently delete "${ev.title}"?\n\nThis action CANNOT be undone.`)) return;
    this.deleting.set(ev.eventId);
    this.eventSvc.delete(ev.eventId).subscribe({
      next: () => { this.toast.success(`"${ev.title}" permanently deleted.`, 'Event Deleted'); this.deleting.set(null); this.loadTabTotals(); this.loadTab(); },
      error: () => this.deleting.set(null)
    });
  }

  viewCancelDetails(ev: EventResponse) {
    this.cancelDetails.set(ev);
    this.cancelDetailsLoading.set(true);
    this.cancelDetailsData.set(null);
    this.eventSvc.getRefundSummary(ev.eventId).subscribe({
      next: s => {
        const compensation = ev.isPaidEvent ? ev.ticketPrice * 0.5 * s.totalUsersRefunded : 0;
        this.cancelDetailsData.set({ totalUsersRefunded: s.totalUsersRefunded, totalRefundAmount: s.totalRefundAmount, organizerCompensation: compensation, adminTotalSpent: s.totalRefundAmount + compensation });
        this.cancelDetailsLoading.set(false);
      },
      error: () => this.cancelDetailsLoading.set(false)
    });
  }

  approvalLabel(s: ApprovalStatus) { return { 1: 'Pending', 2: 'Approved', 3: 'Rejected' }[s] ?? s; }
  approvalBadge(s: ApprovalStatus) { return { 1: 'badge-warning', 2: 'badge-success', 3: 'badge-danger' }[s] ?? 'badge-gray'; }
}
