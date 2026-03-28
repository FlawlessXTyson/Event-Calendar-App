import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EventService } from '../../../core/services/event.service';
import { RegistrationService } from '../../../core/services/registration.service';
import { PaymentService } from '../../../core/services/payment.service';
import { TicketService } from '../../../core/services/ticket.service';
import { ToastService } from '../../../core/services/toast.service';
import {
  EventResponse, EventRegistrationResponse, PaymentResponse,
  RegistrationStatus, PaymentStatus, TicketResponse
} from '../../../core/models/models';

@Component({
  selector: 'app-events-attended',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div>
      <div style="margin-bottom:24px;">
        <h1 style="font-size:1.5rem;">Events Attended</h1>
        <p>Your event history — proof of attendance with downloadable tickets</p>
      </div>

      @if (loading()) {
        <div class="loading-center"><div class="spinner"></div></div>
      } @else if (attendedRegs().length === 0) {
        <div class="empty-state">
          <span class="material-icons-round empty-icon">verified</span>
          <h3>No attended events yet</h3>
          <p>Events you've registered for will appear here once they end.</p>
        </div>
      } @else {
        <div style="margin-bottom:12px;font-size:.875rem;color:var(--text-muted);">
          {{ attendedRegs().length }} event{{ attendedRegs().length !== 1 ? 's' : '' }} attended
        </div>
        <div class="table-wrapper">
          <table>
            <thead>
              <tr><th>Event</th><th>Date</th><th>Type</th><th>Payment</th><th>Status</th><th>Ticket</th></tr>
            </thead>
            <tbody>
              @for (reg of attendedRegs(); track reg.registrationId) {
                <tr>
                  <td>
                    <div style="font-weight:600;font-size:.9rem;">{{ getEventTitle(reg.eventId) }}</div>
                  </td>
                  <td style="color:var(--text-muted);">{{ getEventDate(reg.eventId) | date:'MMM d, y' }}</td>
                  <td>
                    @if (isEventPaid(reg.eventId)) { <span class="badge badge-warning">Paid</span> }
                    @else { <span class="badge badge-success">Free</span> }
                  </td>
                  <td>
                    @if (isEventPaid(reg.eventId)) {
                      @if (isPaid(reg.eventId)) { <span class="badge badge-success">Paid ✓</span> }
                      @else { <span class="badge badge-gray">—</span> }
                    } @else {
                      <span class="badge badge-success">Free ✓</span>
                    }
                  </td>
                  <td>
                    <span class="badge" style="background:#D1FAE5;color:#065F46;font-weight:700;">✅ Attended</span>
                  </td>
                  <td>
                    @if (getTicket(reg.eventId)) {
                      <button type="button" class="btn btn-sm"
                        (click)="openTicket(reg.eventId)"
                        title="View Ticket — Proof of Attendance"
                        style="background:linear-gradient(135deg,var(--primary-dark),var(--primary));color:#fff;border:none;border-radius:var(--r-sm);padding:5px 10px;display:flex;align-items:center;gap:4px;font-size:.75rem;font-weight:700;">
                        <span class="material-icons-round" style="font-size:15px;">confirmation_number</span>
                        Ticket
                      </button>
                    } @else {
                      <span style="font-size:.78rem;color:var(--text-muted);">—</span>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }

      <!-- Ticket modal -->
      @if (ticketModal()) {
        <div class="modal-backdrop" (click)="ticketModal.set(null)">
          <div class="modal" (click)="$event.stopPropagation()" style="max-width:480px;">
            <div class="modal-header" style="background:linear-gradient(135deg,var(--primary-dark),var(--primary));color:#fff;border-radius:var(--r-lg) var(--r-lg) 0 0;">
              <div style="display:flex;align-items:center;gap:10px;">
                <span class="material-icons-round" style="font-size:28px;">confirmation_number</span>
                <div>
                  <div style="font-weight:800;font-size:1rem;">Event Ticket</div>
                  <div style="font-size:.78rem;opacity:.8;">Ticket #{{ ticketModal()!.ticketId }}</div>
                </div>
              </div>
              <button type="button" class="btn btn-ghost btn-icon btn-sm" style="color:#fff;" (click)="ticketModal.set(null)">
                <span class="material-icons-round">close</span>
              </button>
            </div>
            <div class="modal-body" style="padding:24px;">
              <h2 style="font-size:1.2rem;margin-bottom:4px;">{{ ticketModal()!.eventTitle }}</h2>
              <p style="font-size:.875rem;color:var(--text-secondary);margin-bottom:20px;">{{ ticketModal()!.eventDescription }}</p>
              <div style="display:grid;grid-template-columns:1fr 1fr;gap:12px;margin-bottom:20px;">
                <div style="background:var(--surface-2);border-radius:var(--r-sm);padding:12px;">
                  <div style="font-size:.7rem;font-weight:700;color:var(--text-muted);text-transform:uppercase;margin-bottom:4px;">Date</div>
                  <div style="font-weight:600;font-size:.9rem;">{{ ticketModal()!.eventDate | date:'EEE, MMM d, y' }}</div>
                </div>
                <div style="background:var(--surface-2);border-radius:var(--r-sm);padding:12px;">
                  <div style="font-size:.7rem;font-weight:700;color:var(--text-muted);text-transform:uppercase;margin-bottom:4px;">Time</div>
                  <div style="font-weight:600;font-size:.9rem;">{{ fmt(ticketModal()!.startTime) }} – {{ fmt(ticketModal()!.endTime) }}</div>
                </div>
                <div style="background:var(--surface-2);border-radius:var(--r-sm);padding:12px;">
                  <div style="font-size:.7rem;font-weight:700;color:var(--text-muted);text-transform:uppercase;margin-bottom:4px;">Location</div>
                  <div style="font-weight:600;font-size:.9rem;">{{ ticketModal()!.eventLocation || '—' }}</div>
                </div>
                <div style="background:var(--surface-2);border-radius:var(--r-sm);padding:12px;">
                  <div style="font-size:.7rem;font-weight:700;color:var(--text-muted);text-transform:uppercase;margin-bottom:4px;">Amount</div>
                  <div style="font-weight:700;font-size:.9rem;color:var(--primary);">{{ ticketModal()!.isPaidEvent ? '₹' + ticketModal()!.amountPaid : 'Free' }}</div>
                </div>
              </div>
              <div style="border:2px dashed var(--border);border-radius:var(--r-sm);padding:14px;display:flex;align-items:center;justify-content:space-between;">
                <div>
                  <div style="font-size:.7rem;font-weight:700;color:var(--text-muted);text-transform:uppercase;margin-bottom:2px;">Attendee</div>
                  <div style="font-weight:700;">{{ ticketModal()!.userName }}</div>
                </div>
                <div style="text-align:right;">
                  <div style="font-size:.7rem;font-weight:700;color:var(--text-muted);text-transform:uppercase;margin-bottom:2px;">Ticket ID</div>
                  <div style="font-weight:700;font-family:monospace;font-size:1rem;color:var(--primary);">#{{ ticketModal()!.ticketId }}</div>
                </div>
              </div>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-ghost" (click)="printTicket()">
                <span class="material-icons-round">print</span> Print
              </button>
              <button type="button" class="btn btn-primary" (click)="downloadTicket()">
                <span class="material-icons-round">download</span> Download
              </button>
            </div>
          </div>
        </div>
      }
    </div>
  `
})
export class EventsAttendedComponent implements OnInit {
  private eventSvc  = inject(EventService);
  private regSvc    = inject(RegistrationService);
  private paySvc    = inject(PaymentService);
  private ticketSvc = inject(TicketService);
  private toast     = inject(ToastService);

  registeredEvents = signal<EventResponse[]>([]);
  myRegs           = signal<EventRegistrationResponse[]>([]);
  myPayments       = signal<PaymentResponse[]>([]);
  myTickets        = signal<TicketResponse[]>([]);
  loading          = signal(true);
  ticketModal      = signal<TicketResponse | null>(null);

  attendedRegs = computed(() => {
    if (this.registeredEvents().length === 0) return [];
    return this.myRegs().filter(r => {
      if (r.status !== RegistrationStatus.REGISTERED) return false;
      const ev = this.registeredEvents().find(e => e.eventId === r.eventId);
      return ev?.hasEnded === true;
    });
  });

  ngOnInit() {
    this.regSvc.getMyRegistrations().subscribe({
      next: regs => {
        this.myRegs.set(regs);
        this.eventSvc.getRegistered().subscribe({
          next: evs => { this.registeredEvents.set(evs); this.loading.set(false); },
          error: () => this.loading.set(false)
        });
      },
      error: () => this.loading.set(false)
    });
    this.paySvc.getMyPayments().subscribe({ next: p => this.myPayments.set(p), error: () => {} });
    this.ticketSvc.getMyTickets().subscribe({ next: t => this.myTickets.set(t), error: () => {} });
  }

  getEvent(eid: number)     { return this.registeredEvents().find(e => e.eventId === eid) ?? null; }
  getEventTitle(eid: number){ return this.getEvent(eid)?.title ?? `Event #${eid}`; }
  getEventDate(eid: number) { return this.getEvent(eid)?.eventDate ?? ''; }
  isEventPaid(eid: number)  { return this.getEvent(eid)?.isPaidEvent ?? false; }
  isPaid(eid: number)       { return this.myPayments().some(p => p.eventId === eid && p.status === PaymentStatus.SUCCESS); }
  getTicket(eid: number)    { return this.myTickets().find(t => t.eventId === eid) ?? null; }

  openTicket(eid: number) {
    const t = this.getTicket(eid);
    if (t) { this.ticketModal.set(t); return; }
    this.ticketSvc.getByEvent(eid).subscribe({
      next: t2 => { this.myTickets.update(ts => [...ts, t2]); this.ticketModal.set(t2); },
      error: () => this.toast.error('Could not load ticket.', 'Error')
    });
  }

  printTicket() { window.print(); }

  downloadTicket() {
    const t = this.ticketModal();
    if (!t) return;
    const content = [
      'EVENT TICKET — PROOF OF ATTENDANCE',
      '====================================',
      `Ticket ID  : #${t.ticketId}`,
      `Event      : ${t.eventTitle}`,
      `Location   : ${t.eventLocation}`,
      `Date       : ${new Date(t.eventDate).toDateString()}`,
      `Time       : ${this.fmt(t.startTime)} – ${this.fmt(t.endTime)}`,
      `Attendee   : ${t.userName}`,
      `Amount Paid: ${t.isPaidEvent ? '₹' + t.amountPaid : 'Free'}`,
      `Generated  : ${new Date(t.generatedAt).toLocaleString()}`,
      '====================================',
    ].join('\n');
    const blob = new Blob([content], { type: 'text/plain' });
    const url  = URL.createObjectURL(blob);
    const a    = document.createElement('a');
    a.href = url; a.download = `ticket-${t.ticketId}.txt`; a.click();
    URL.revokeObjectURL(url);
  }

  fmt(t?: string | null): string {
    if (!t) return '—';
    const [h, m] = t.split(':').map(Number);
    const ampm = h >= 12 ? 'PM' : 'AM';
    return `${h % 12 || 12}:${String(m).padStart(2, '0')} ${ampm}`;
  }
}
