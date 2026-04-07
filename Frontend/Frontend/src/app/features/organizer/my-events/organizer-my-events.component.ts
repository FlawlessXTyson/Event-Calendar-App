import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { EventService } from '../../../core/services/event.service';
import { ToastService } from '../../../core/services/toast.service';
import { EventResponse, ApprovalStatus, EventStatus } from '../../../core/models/models';

@Component({
  selector: 'app-organizer-my-events',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './organizer-my-events.component.html',
  styleUrl: './organizer-my-events.component.css'
})
export class OrganizerMyEventsComponent implements OnInit {
  private eventSvc = inject(EventService);
  private toast    = inject(ToastService);
  EventStatus      = EventStatus;
  ApprovalStatus   = ApprovalStatus;

  events       = signal<EventResponse[]>([]);
  loading      = signal(true);
  cancelling   = signal<number | null>(null);
  refundModal  = signal<{ totalUsersRefunded: number; totalRefundAmount: number; ticketPrice: number; eventTitle: string } | null>(null);

  currentPage  = signal(1);
  totalRecords = signal(0);
  readonly pageSize = 10;
  filterDate   = '';

  totalPages = () => Math.max(1, Math.ceil(this.totalRecords() / this.pageSize));

  pageNumbers() {
    const total = this.totalPages();
    const cur   = this.currentPage();
    const pages: number[] = [];
    const start = Math.max(1, cur - 2);
    const end   = Math.min(total, cur + 2);
    for (let i = start; i <= end; i++) pages.push(i);
    return pages;
  }

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    const date = this.filterDate || undefined;
    this.eventSvc.getMyEventsPaged(this.currentPage(), this.pageSize, date).subscribe({
      next: res => {
        this.events.set(res.data as EventResponse[]);
        this.totalRecords.set(res.totalRecords);
        this.loading.set(false);
      },
      error: () => {
        // Fallback to non-paged if new endpoint not available yet
        this.eventSvc.getMyEvents().subscribe({
          next: evs => {
            const sorted = evs.sort((a, b) => {
              const aT = new Date(a.eventDate).getTime() + (a.startTime ? timeToMs(a.startTime) : 0);
              const bT = new Date(b.eventDate).getTime() + (b.startTime ? timeToMs(b.startTime) : 0);
              return bT - aT;
            });
            this.totalRecords.set(sorted.length);
            const start = (this.currentPage() - 1) * this.pageSize;
            this.events.set(sorted.slice(start, start + this.pageSize));
            this.loading.set(false);
          },
          error: () => this.loading.set(false)
        });
      }
    });
  }

  goPage(p: number) {
    if (p < 1 || p > this.totalPages()) return;
    this.currentPage.set(p);
    this.load();
  }

  onDateChange() { this.currentPage.set(1); this.load(); }
  clearDate()    { this.filterDate = ''; this.currentPage.set(1); this.load(); }

  cancelEvent(ev: EventResponse) {
    const isPending = ev.approvalStatus === ApprovalStatus.PENDING;
    const msg = isPending
      ? `"${ev.title}" is currently under review.\n\nCancelling will withdraw it from approval. Are you sure?`
      : `Cancel "${ev.title}"? All paid attendees will be automatically refunded.`;
    if (!confirm(msg)) return;
    this.cancelling.set(ev.eventId);
    this.eventSvc.cancel(ev.eventId).subscribe({
      next: () => {
        this.events.update(es => es.map(e => e.eventId === ev.eventId ? { ...e, status: EventStatus.CANCELLED } : e));
        const toastMsg = isPending
          ? `"${ev.title}" withdrawn from review.`
          : `"${ev.title}" cancelled. Refunds processed.`;
        this.toast.success(toastMsg, 'Event Cancelled');
        this.cancelling.set(null);
      },
      error: () => this.cancelling.set(null)
    });
  }

  viewRefund(ev: EventResponse) {
    this.eventSvc.getRefundSummary(ev.eventId).subscribe({
      next: s => this.refundModal.set({ ...s, ticketPrice: ev.ticketPrice, eventTitle: ev.title }),
      error: () => {}
    });
  }

  fmt(t: string): string {
    const [h, m] = t.split(':').map(Number);
    const ampm = h >= 12 ? 'PM' : 'AM';
    return `${h % 12 || 12}:${String(m).padStart(2, '0')} ${ampm}`;
  }

  approvalLabel(s: number) { return { 1: 'Pending', 2: 'Approved', 3: 'Rejected' }[s] ?? s; }
  approvalBadge(s: number) { return { 1: 'badge-warning', 2: 'badge-success', 3: 'badge-danger' }[s] ?? 'badge-gray'; }
}

function timeToMs(t: string): number {
  const [h, m] = t.split(':').map(Number);
  return ((h || 0) * 60 + (m || 0)) * 60000;
}
