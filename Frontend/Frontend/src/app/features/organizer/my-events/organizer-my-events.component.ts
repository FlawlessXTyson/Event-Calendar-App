import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { EventService } from '../../../core/services/event.service';
import { ToastService } from '../../../core/services/toast.service';
import { EventResponse, ApprovalStatus, EventStatus } from '../../../core/models/models';

@Component({
  selector: 'app-organizer-my-events',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div>
      <div class="section-header" style="margin-bottom:24px;">
        <div><h1 style="font-size:1.5rem;">My Events</h1><p>{{ events().length }} events created</p></div>
        <a routerLink="/organizer/create-event" class="btn btn-primary btn-sm"><span class="material-icons-round">add</span> New Event</a>
      </div>
      @if (loading()) { <div class="loading-center"><div class="spinner"></div></div> }
      @else if (events().length === 0) {
        <div class="empty-state"><span class="material-icons-round empty-icon">event_busy</span><h3>No events yet</h3><p>Create your first event to get started.</p><a routerLink="/organizer/create-event" class="btn btn-primary btn-sm" style="margin-top:16px;">Create Event</a></div>
      } @else {
        <div class="table-wrapper">
          <table>
            <thead><tr><th>Title</th><th>Date</th><th>Location</th><th>Type</th><th>Approval</th><th>Status</th><th>Actions</th></tr></thead>
            <tbody>
              @for (ev of events(); track ev.eventId) {
                <tr>
                  <td style="font-weight:600;max-width:200px;" class="truncate">{{ ev.title }}</td>
                  <td style="white-space:nowrap;">{{ ev.eventDate | date:'MMM d, y' }}</td>
                  <td style="color:var(--text-muted);">{{ ev.location || '—' }}</td>
                  <td>@if (ev.isPaidEvent) { <span class="badge badge-warning">₹{{ ev.ticketPrice | number:'1.0-0' }}</span> } @else { <span class="badge badge-success">Free</span> }</td>
                  <td><span class="badge" [class]="approvalBadge(ev.approvalStatus)">{{ approvalLabel(ev.approvalStatus) }}</span></td>
                  <td><span class="badge" [class]="statusBadge(ev.status ?? 1)">{{ statusLabel(ev.status ?? 1) }}</span></td>
                  <td>
                    <div style="display:flex;gap:6px;">
                      @if ((ev.status ?? 1) === EventStatus.ACTIVE) {
                        <button type="button" class="btn btn-danger btn-sm" [disabled]="cancelling() === ev.eventId" (click)="cancelEvent(ev)">
                          @if (cancelling() === ev.eventId) { <div class="spinner spinner-sm"></div> } @else { Cancel }
                        </button>
                      }
                      <button type="button" class="btn btn-ghost btn-sm btn-icon" (click)="viewRefund(ev)" title="Refund Summary">
                        <span class="material-icons-round" style="font-size:18px;">summarize</span>
                      </button>
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }

      <!-- Refund Summary Modal -->
      @if (refundModal()) {
        <div class="modal-backdrop" (click)="refundModal.set(null)">
          <div class="modal" (click)="$event.stopPropagation()">
            <div class="modal-header"><h3>Refund Summary</h3></div>
            <div class="modal-body">
              <div style="display:grid;grid-template-columns:1fr 1fr;gap:16px;">
                <div style="text-align:center;padding:20px;background:var(--surface-2);border-radius:var(--r-sm);">
                  <div style="font-size:2rem;font-weight:800;">{{ refundModal()!.totalUsersRefunded }}</div>
                  <div style="color:var(--text-muted);font-size:.85rem;">Users Refunded</div>
                </div>
                <div style="text-align:center;padding:20px;background:var(--success-light);border-radius:var(--r-sm);">
                  <div style="font-size:2rem;font-weight:800;color:var(--success);">₹{{ refundModal()!.totalRefundAmount | number:'1.0-0' }}</div>
                  <div style="color:var(--text-muted);font-size:.85rem;">Total Refunded</div>
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

  events     = signal<EventResponse[]>([]);
  loading    = signal(true);
  cancelling = signal<number|null>(null);
  refundModal= signal<{totalUsersRefunded:number;totalRefundAmount:number}|null>(null);

  ngOnInit() { this.eventSvc.getMyEvents().subscribe({ next: evs => { this.events.set(evs); this.loading.set(false); }, error: () => this.loading.set(false) }); }

  cancelEvent(ev: EventResponse) {
    if (!confirm(`Cancel "${ev.title}"? All paid attendees will be automatically refunded.`)) return;
    this.cancelling.set(ev.eventId);
    this.eventSvc.cancel(ev.eventId).subscribe({
      next: () => { this.events.update(es => es.map(e => e.eventId === ev.eventId ? {...e, status: EventStatus.CANCELLED} : e)); this.toast.success(`"${ev.title}" cancelled. Refunds processed.`, 'Event Cancelled'); this.cancelling.set(null); },
      error: () => this.cancelling.set(null)
    });
  }

  viewRefund(ev: EventResponse) {
    this.eventSvc.getRefundSummary(ev.eventId).subscribe({ next: s => this.refundModal.set(s), error: () => {} });
  }

  approvalLabel(s: number) { return {1:'Pending',2:'Approved',3:'Rejected'}[s] ?? s; }
  approvalBadge(s: number) { return {1:'badge-warning',2:'badge-success',3:'badge-danger'}[s] ?? 'badge-gray'; }
  statusLabel(s: number)   { return {0:'Cancelled',1:'Active',2:'Completed'}[s] ?? s; }
  statusBadge(s: number)   { return {0:'badge-danger',1:'badge-success',2:'badge-info'}[s] ?? 'badge-gray'; }
}
