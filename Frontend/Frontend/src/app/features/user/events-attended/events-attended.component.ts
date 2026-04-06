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
  templateUrl: './events-attended.component.html',
  styleUrl: './events-attended.component.css'
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
