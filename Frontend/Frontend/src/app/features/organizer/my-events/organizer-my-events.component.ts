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
  template: `
    <div>
      <div class="section-header" style="margin-bottom:24px;">
        <div><h1 style="font-size:1.5rem;">My Events</h1><p>{{ totalRecords() }} events created</p></div>
        <a routerLink="/organizer/create-event" class="btn btn-primary btn-sm"><span class="material-icons-round">add</span> New Event</a>
      </div>

      <!-- Date filter + clear -->
      <div style="display:flex;gap:12px;align-items:center;margin-bottom:20px;flex-wrap:wrap;">
        <div style="display:flex;align-items:center;gap:8px;">
          <span class="material-icons-round" style="color:var(--text-muted);">calendar_today</span>
          <input type="date" class="form-control" style="width:180px;"
            [(ngModel)]="filterDate"
            (change)="onDateChange()"
            placeholder="Filter by date" />
        </div>
        @if (filterDate) {
          <button type="button" class="btn btn-ghost btn-sm" (click)="clearDate()">
            <span class="material-icons-round" style="font-size:16px;">close</span> Clear
          </button>
          <span style="font-size:.85rem;color:var(--text-muted);">
            Showing events on {{ filterDate | date:'EEE, MMM d, y' }}
          </span>
        }
      </div>

      @if (loading()) { <div class="loading-center"><div class="spinner"></div></div> }
      @else if (events().length === 0) {
        <div class="empty-state">
          <span class="material-icons-round empty-icon">event_busy</span>
          <h3>{{ filterDate ? 'No events on this date' : 'No events yet' }}</h3>
          @if (!filterDate) { <a routerLink="/organizer/create-event" class="btn btn-primary btn-sm" style="margin-top:16px;">Create Event</a> }
        </div>
      } @else {
        <div class="table-wrapper">
          <table>
            <thead><tr><th>Title</th><th>Date</th><th>Time</th><th>Location</th><th>Type</th><th>Approval</th><th>Status</th><th>Actions</th></tr></thead>
            <tbody>
              @for (ev of events(); track ev.eventId) {
                <tr>
                  <td style="font-weight:600;max-width:180px;" class="truncate">{{ ev.title }}</td>
                  <td style="white-space:nowrap;">{{ ev.eventDate | date:'MMM d, y' }}</td>
                  <td style="white-space:nowrap;color:var(--text-muted);font-size:.82rem;">
                    @if (ev.startTime) { {{ fmt(ev.startTime) }}{{ ev.endTime ? ' – ' + fmt(ev.endTime) : '' }} }
                    @else { — }
                  </td>
                  <td style="color:var(--text-muted);max-width:160px;" class="truncate">{{ ev.location || '—' }}</td>
                  <td>@if (ev.isPaidEvent) { <span class="badge badge-warning">₹{{ ev.ticketPrice | number:'1.0-0' }}</span> } @else { <span class="badge badge-success">Free</span> }</td>
                  <td><span class="badge" [class]="approvalBadge(ev.approvalStatus)">{{ approvalLabel(ev.approvalStatus) }}</span></td>
                  <td>
                    @if ((ev.status ?? 1) === EventStatus.CANCELLED) {
                      <span class="badge badge-danger">Cancelled</span>
                    } @else if (ev.hasEnded) {
                      <span class="badge badge-info">Completed</span>
                    } @else if (ev.hasStarted) {
                      <span class="badge" style="background:#FEF3C7;color:#92400E;">🟡 Ongoing</span>
                    } @else {
                      <span class="badge badge-success">Active</span>
                    }
                  </td>
                  <td>
                    <div style="display:flex;gap:6px;">
                      @if ((ev.status ?? 1) === EventStatus.ACTIVE && !ev.hasEnded) {
                        <button type="button" class="btn btn-danger btn-sm" [disabled]="cancelling() === ev.eventId" (click)="cancelEvent(ev)">
                          @if (cancelling() === ev.eventId) { <div class="spinner spinner-sm"></div> } @else { Cancel }
                        </button>
                      }
                      @if (ev.hasEnded) {
                        <span class="badge badge-info" style="font-size:.78rem;padding:4px 10px;">✓ Completed</span>
                      }
                      @if (ev.isPaidEvent) {
                        <button type="button" class="btn btn-ghost btn-sm btn-icon" (click)="viewRefund(ev)" title="Refund Summary">
                          <span class="material-icons-round" style="font-size:18px;">summarize</span>
                        </button>
                      }
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        @if (totalPages() > 1) {
          <div style="display:flex;align-items:center;justify-content:space-between;padding:14px 0;flex-wrap:wrap;gap:8px;">
            <span style="font-size:.82rem;color:var(--text-muted);">
              Page {{ currentPage() }} of {{ totalPages() }} &nbsp;·&nbsp; {{ totalRecords() }} events
            </span>
            <div style="display:flex;gap:6px;align-items:center;">
              <button type="button" class="btn btn-ghost btn-sm" [disabled]="currentPage() === 1" (click)="goPage(1)">
                <span class="material-icons-round" style="font-size:16px;">first_page</span>
              </button>
              <button type="button" class="btn btn-ghost btn-sm" [disabled]="currentPage() === 1" (click)="goPage(currentPage() - 1)">
                <span class="material-icons-round" style="font-size:16px;">chevron_left</span>
              </button>
              @for (p of pageNumbers(); track p) {
                <button type="button" class="btn btn-sm"
                  [class]="p === currentPage() ? 'btn-primary' : 'btn-ghost'"
                  (click)="goPage(p)">{{ p }}</button>
              }
              <button type="button" class="btn btn-ghost btn-sm" [disabled]="currentPage() === totalPages()" (click)="goPage(currentPage() + 1)">
                <span class="material-icons-round" style="font-size:16px;">chevron_right</span>
              </button>
              <button type="button" class="btn btn-ghost btn-sm" [disabled]="currentPage() === totalPages()" (click)="goPage(totalPages())">
                <span class="material-icons-round" style="font-size:16px;">last_page</span>
              </button>
            </div>
          </div>
        }
      }

      <!-- Refund Summary Modal -->
      @if (refundModal()) {
        <div class="modal-backdrop" (click)="refundModal.set(null)">
          <div class="modal" (click)="$event.stopPropagation()">
            <div class="modal-header">
              <div>
                <h3 style="margin-bottom:2px;">Refund Summary</h3>
                <div style="font-size:.82rem;color:var(--text-muted);">{{ refundModal()!.eventTitle }}</div>
              </div>
            </div>
            <div class="modal-body">
              <div style="display:grid;grid-template-columns:1fr 1fr 1fr;gap:14px;">
                <div style="text-align:center;padding:18px 12px;background:var(--primary-light);border-radius:var(--r-sm);">
                  <div style="font-size:1.6rem;font-weight:800;color:var(--primary);">₹{{ refundModal()!.ticketPrice | number:'1.0-0' }}</div>
                  <div style="color:var(--text-muted);font-size:.82rem;margin-top:4px;">Ticket Price</div>
                </div>
                <div style="text-align:center;padding:18px 12px;background:var(--surface-2);border-radius:var(--r-sm);">
                  <div style="font-size:1.6rem;font-weight:800;">{{ refundModal()!.totalUsersRefunded }}</div>
                  <div style="color:var(--text-muted);font-size:.82rem;margin-top:4px;">Users Refunded</div>
                </div>
                <div style="text-align:center;padding:18px 12px;background:var(--success-light);border-radius:var(--r-sm);">
                  <div style="font-size:1.6rem;font-weight:800;color:var(--success);">₹{{ refundModal()!.totalRefundAmount | number:'1.0-0' }}</div>
                  <div style="color:var(--text-muted);font-size:.82rem;margin-top:4px;">Total Refunded</div>
                </div>
              </div>
            </div>
            <div class="modal-footer"><button type="button" class="btn btn-secondary" (click)="refundModal.set(null)">Close</button></div>
          </div>
        </div>
      }
    </div>
  `
})
export class OrganizerMyEventsComponent implements OnInit {
  private eventSvc = inject(EventService);
  private toast    = inject(ToastService);
  EventStatus      = EventStatus;

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
    if (!confirm(`Cancel "${ev.title}"? All paid attendees will be automatically refunded.`)) return;
    this.cancelling.set(ev.eventId);
    this.eventSvc.cancel(ev.eventId).subscribe({
      next: () => {
        this.events.update(es => es.map(e => e.eventId === ev.eventId ? { ...e, status: EventStatus.CANCELLED } : e));
        this.toast.success(`"${ev.title}" cancelled. Refunds processed.`, 'Event Cancelled');
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
