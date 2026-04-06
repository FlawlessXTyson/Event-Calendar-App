import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, Router } from '@angular/router';
import { EventService } from '../../../core/services/event.service';
import { RegistrationService } from '../../../core/services/registration.service';
import { PaymentService } from '../../../core/services/payment.service';
import { TicketService } from '../../../core/services/ticket.service';
import { ToastService } from '../../../core/services/toast.service';
import { RegistrationStateService } from '../../../core/services/registration-state.service';
import { WalletService } from '../../../core/services/wallet.service';
import {
  EventResponse, EventRegistrationResponse, PaymentResponse,
  ApprovalStatus, RegistrationStatus, PaymentStatus, TicketResponse
} from '../../../core/models/models';

type PayMethod = 'upi' | 'netbanking' | 'card' | 'wallet' | '';

const PAY_METHODS: { key: PayMethod; label: string; icon: string; color: string }[] = [
  { key: 'upi',        label: 'UPI',           icon: '⚡', color: '#6366F1' },
  { key: 'netbanking', label: 'Net Banking',    icon: '🏦', color: '#0EA5E9' },
  { key: 'card',       label: 'Credit / Debit', icon: '💳', color: '#0D9488' },
  { key: 'wallet',     label: 'Wallet',         icon: '👛', color: '#F59E0B' },
];

@Component({
  selector: 'app-user-my-events',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './user-my-events.component.html',
  styleUrl: './user-my-events.component.css'
})
export class UserMyEventsComponent implements OnInit {
  private eventSvc  = inject(EventService);
  private regSvc    = inject(RegistrationService);
  private paySvc    = inject(PaymentService);
  private ticketSvc = inject(TicketService);
  private walletSvc = inject(WalletService);
  private toast     = inject(ToastService);
  readonly regState = inject(RegistrationStateService);
  private router    = inject(Router);

  allEvents        = signal<EventResponse[]>([]);
  registeredEvents = signal<EventResponse[]>([]); // event details for My Registrations table
  myRegs     = signal<EventRegistrationResponse[]>([]);
  myPayments = signal<PaymentResponse[]>([]);
  myTickets  = signal<TicketResponse[]>([]);
  loadingAll = signal(true);
  tab        = signal<'browse' | 'registered' | 'attended'>('browse');
  search     = '';
  acting     = signal<number | null>(null);
  paying     = signal<number | null>(null);
  cancelling = signal<number | null>(null);

  // Payment modal
  payModalEvent     = signal<EventResponse | null>(null);
  selectedPayMethod = signal<PayMethod>('');
  payMethods        = PAY_METHODS;

  // Ticket detail modal (full view)
  ticketDetailModal = signal<TicketResponse | null>(null);

  // Track events with pending refund requests (paid events that were cancelled)
  pendingRefundEventIds = signal<Set<number>>(new Set());

  // Cancel confirm modal
  cancelConfirmEvent = signal<{ ev: EventResponse | null; reg: EventRegistrationResponse | null }>({ ev: null, reg: null });
  get showCancelModal() { return this.cancelConfirmEvent().ev !== null; }

  filteredAll = computed(() => {
    const s = this.search.toLowerCase();
    return this.allEvents().filter(ev =>
      !s || ev.title.toLowerCase().includes(s) || (ev.location ?? '').toLowerCase().includes(s)
    );
  });

  // My Registrations = active registrations for events that have NOT ended
  activeRegs = computed(() => {
    const regs = this.myRegs().filter(r => r.status === RegistrationStatus.REGISTERED);
    // If registeredEvents not loaded yet, show all (fallback)
    if (this.registeredEvents().length === 0) return regs;
    return regs.filter(r => {
      const ev = this.registeredEvents().find(e => e.eventId === r.eventId)
                 ?? this.allEvents().find(e => e.eventId === r.eventId);
      return ev?.hasEnded !== true; // undefined or false = not ended = show here
    });
  });

  // Count badge = same as activeRegs
  activeRegCount = computed(() => this.activeRegs().length);

  // Attended = registered events that have EXPLICITLY ended
  attendedRegs = computed(() => {
    const regs = this.myRegs().filter(r => r.status === RegistrationStatus.REGISTERED);
    if (this.registeredEvents().length === 0) return [];
    return regs.filter(r => {
      const ev = this.registeredEvents().find(e => e.eventId === r.eventId)
                 ?? this.allEvents().find(e => e.eventId === r.eventId);
      return ev?.hasEnded === true;
    });
  });

  attendedCount = computed(() => this.attendedRegs().length);

  ngOnInit() {
    // Load registrations first, then events — so the filter can check registration status
    this.regSvc.getMyRegistrations().subscribe({
      next: regs => {
        this.myRegs.set(regs);
        this._loadEvents();
      },
      error: () => { this._loadEvents(); }
    });
    this.paySvc.getMyPayments().subscribe({ next: p => this.myPayments.set(p), error: () => {} });
    this.ticketSvc.getMyTickets().subscribe({ next: t => this.myTickets.set(t), error: () => {} });
    // Load registered event details for My Registrations table (includes ended events)
    this.eventSvc.getRegistered().subscribe({ next: evs => this.registeredEvents.set(evs), error: () => {} });
  }

  private _loadEvents() {
    const today = new Date(); today.setHours(0, 0, 0, 0);
    this.eventSvc.getAll().subscribe({
      next: evs => {
        this.allEvents.set(
          evs
            .filter(e =>
              e.approvalStatus === ApprovalStatus.APPROVED &&
              new Date(e.eventDate) >= today &&
              e.hasEnded !== true   // hide ended events from browse
            )
            .sort((a, b) => new Date(a.eventDate).getTime() - new Date(b.eventDate).getTime())
        );
        this.loadingAll.set(false);
      },
      error: () => this.loadingAll.set(false)
    });
  }

  // ── Tab navigation ─────────────────────────────────────────────────────────
  switchTab(t: 'browse' | 'registered' | 'attended') {
    // Only block navigation for paid events with pending payment
    if (this.regState.isNavigationBlocked()) {
      this.toast.warning('Please complete payment or cancel registration before leaving.', 'Navigation Blocked');
      return;
    }
    this.tab.set(t);
  }

  // Only block navigation for paid-pending events
  guardNav(ev?: EventResponse) {
    if (this.regState.isNavigationBlocked() && ev?.isPaidEvent) {
      this.toast.warning('Please complete payment or cancel registration before leaving.', 'Navigation Blocked');
    }
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  isRegistered(eid: number) { return this.myRegs().some(r => r.eventId === eid && r.status === RegistrationStatus.REGISTERED); }

  /** True when event is free — either not marked as paid OR ticket price is 0 */
  isFreeEvent(ev: EventResponse | null): boolean {
    if (!ev) return true;
    return !ev.isPaidEvent || ev.ticketPrice === 0;
  }

  /** True when registration is no longer possible */
  isDeadlinePassed(ev: EventResponse): boolean {
    const now = new Date();

    // Check registration deadline — ensure UTC parsing by appending Z if missing
    if (ev.registrationDeadline) {
      const dl = ev.registrationDeadline;
      // If no timezone info, treat as UTC (backend stores in UTC)
      const deadlineDate = new Date(dl.endsWith('Z') || dl.includes('+') ? dl : dl + 'Z');
      if (deadlineDate <= now) return true;
    }

    // Event has started or ended (server-computed with local time)
    if (ev.hasStarted === true || ev.hasEnded === true) return true;

    // No deadline set + server says closed
    if (!ev.registrationDeadline && ev.isRegistrationOpen === false) return true;

    return false;
  }

  // A payment is only "active" if the user is currently registered AND payment is SUCCESS.
  // This prevents old SUCCESS payments from a previous cancelled registration from blocking re-payment.
  isPaid(eid: number) {
    const isCurrentlyRegistered = this.isRegistered(eid);
    if (!isCurrentlyRegistered) return false;
    return this.myPayments().some(p => p.eventId === eid && p.status === PaymentStatus.SUCCESS);
  }
  getRegId(eid: number)     { return this.myRegs().find(r => r.eventId === eid && r.status === RegistrationStatus.REGISTERED)?.registrationId ?? null; }
  getEvent(eid: number)     { return this.registeredEvents().find(e => e.eventId === eid) ?? this.allEvents().find(e => e.eventId === eid) ?? null; }
  getEventTitle(eid: number){ return this.getEvent(eid)?.title ?? `Event #${eid}`; }
  getEventDate(eid: number) { return this.getEvent(eid)?.eventDate ?? ''; }
  isEventPaid(eid: number)  { return this.getEvent(eid)?.isPaidEvent ?? false; }
  getTicket(eid: number)    { return this.myTickets().find(t => t.eventId === eid) ?? null; }

  getPaidAmount(eid: number): number {
    return this.myPayments().find(p => p.eventId === eid && p.status === PaymentStatus.SUCCESS)?.amountPaid ?? 0;
  }

  seatsDisplay(ev: EventResponse): string {
    const left = ev.seatsLeft;
    if (left === undefined || left < 0) return '';
    if (left === 0) return '🚫 Event Full';
    if (left <= 5)  return `🔥 Only ${left} left!`;
    return `${left} seats left`;
  }

  seatBadgeClass(ev: EventResponse): string {
    const left = ev.seatsLeft;
    if (left === undefined || left < 0) return 'badge badge-gray';
    if (left === 0) return 'badge badge-danger';
    if (left <= 5)  return 'badge badge-warning';
    return 'badge badge-success';
  }

  // ── Payment modal ──────────────────────────────────────────────────────────
  openPayModal(ev: EventResponse) { this.selectedPayMethod.set(''); this.payModalEvent.set(ev); }
  openPayModalById(eid: number)   { const ev = this.allEvents().find(e => e.eventId === eid); if (ev) this.openPayModal(ev); }
  closePayModal()                 { this.payModalEvent.set(null); this.selectedPayMethod.set(''); }

  confirmPay() {
    const ev = this.payModalEvent();
    if (!ev || !this.selectedPayMethod()) return;
    this.paying.set(ev.eventId);

    const onSuccess = (p: any) => {
      this.myPayments.update(ps => [...ps, p]);
      this.regState.clearPending();
      this.toast.success(`₹${ev.ticketPrice} paid via ${this.selectedPayMethod().toUpperCase()} for "${ev.title}"!`, 'Payment Successful');
      this.paying.set(null);
      this.closePayModal();
      this.ticketSvc.generate(ev.eventId, p.paymentId).subscribe({
        next: t => { this.myTickets.update(ts => [...ts, t]); },
        error: () => {}
      });
    };
    const onError = () => this.paying.set(null);

    if (this.selectedPayMethod() === 'wallet') {
      this.walletSvc.payWithWallet({ eventId: ev.eventId }).subscribe({ next: onSuccess, error: onError });
    } else {
      this.paySvc.create({ eventId: ev.eventId }).subscribe({ next: onSuccess, error: onError });
    }
  }

  // ── Registration ───────────────────────────────────────────────────────────
  registerEvent(ev: EventResponse) {
    this.acting.set(ev.eventId);
    this.regSvc.register({ eventId: ev.eventId }).subscribe({
      next: res => {
        this.myRegs.update(rs => [...rs, res.data]);
        if (this.isFreeEvent(ev)) {
          // Free event (isPaidEvent=false OR ticketPrice=0): no lock, auto-generate ticket
          this.toast.success(`Registered for "${ev.title}"!`, 'Registered!');
          this.ticketSvc.generate(ev.eventId).subscribe({
            next: t => { this.myTickets.update(ts => [...ts, t]); },
            error: () => {}
          });
        } else {
          // Paid event: lock navigation until paid or cancelled
          this.regState.setPending(ev.eventId, true);
          this.toast.info(`Registered for "${ev.title}". Please complete payment or cancel.`, 'Payment Required');
        }
        this.acting.set(null);
      },
      error: () => this.acting.set(null)
    });
  }

  cancelEvent(ev: EventResponse) {
    const regId = this.getRegId(ev.eventId);
    if (!regId) return;
    const reg = this.myRegs().find(r => r.registrationId === regId) ?? null;
    this.cancelConfirmEvent.set({ ev, reg });
  }

  cancelById(reg: EventRegistrationResponse) {
    const ev = this.getEvent(reg.eventId);
    this.cancelConfirmEvent.set({ ev: ev ?? null, reg });
  }

  closeCancelModal() { this.cancelConfirmEvent.set({ ev: null, reg: null }); }

  confirmCancel() {
    const { ev, reg } = this.cancelConfirmEvent();
    if (!reg) return;
    const regId = reg.registrationId;
    this.closeCancelModal();
    this.cancelling.set(regId);
    this.regSvc.cancel(regId).subscribe({
      next: res => {
        this.myRegs.update(rs => rs.map(r => r.registrationId === regId ? { ...r, status: RegistrationStatus.CANCELLED } : r));
        this.myPayments.update(ps => ps.map(p =>
          p.eventId === reg.eventId && p.status === PaymentStatus.SUCCESS ? { ...p, status: PaymentStatus.REFUNDED } : p
        ));
        if (ev?.isPaidEvent && (ev?.ticketPrice ?? 0) > 0) {
          this.pendingRefundEventIds.update(s => new Set([...s, reg.eventId]));
        }
        this.regState.clearPending();
        this.toast.success(res.message, 'Registration Cancelled');
        this.cancelling.set(null);
      },
      error: () => this.cancelling.set(null)
    });
  }

  /** Calculate refund amount based on hours before event start */
  calcRefund(ev: EventResponse): number {
    const paid = this.myPayments().find(p => p.eventId === ev.eventId && p.status === PaymentStatus.SUCCESS)?.amountPaid ?? 0;
    if (!ev.isPaidEvent || paid === 0) return 0;
    const eventStart = new Date(`${ev.eventDate.split('T')[0]}T${ev.startTime ?? '00:00:00'}`);
    const hoursLeft = (eventStart.getTime() - Date.now()) / 3600000;
    let pct = 0;
    if (hoursLeft >= 48)      pct = 100;
    else if (hoursLeft >= 24) pct = 75;
    else if (hoursLeft >= 12) pct = 50;
    else if (hoursLeft > 0)   pct = 25;
    return Math.round(paid * pct / 100);
  }

  refundPercent(ev: EventResponse): number {
    const eventStart = new Date(`${ev.eventDate.split('T')[0]}T${ev.startTime ?? '00:00:00'}`);
    const hoursLeft = (eventStart.getTime() - Date.now()) / 3600000;
    if (hoursLeft >= 48)      return 100;
    if (hoursLeft >= 24)      return 75;
    if (hoursLeft >= 12)      return 50;
    if (hoursLeft > 0)        return 25;
    return 0;
  }

  // ── Ticket detail ─────────────────────────────────────────────────────────
  openTicketDetail(t: TicketResponse) { this.ticketDetailModal.set(t); }

  printTicket() { window.print(); }

  downloadTicketPdf() { const t = this.ticketDetailModal(); if (t) this._doDownload(t); }

  private _doDownload(t: TicketResponse) {
    const content = [
      'EVENT TICKET',
      '============',
      `Ticket ID  : #${t.ticketId}`,
      `Event      : ${t.eventTitle}`,
      `Description: ${t.eventDescription}`,
      `Location   : ${t.eventLocation}`,
      `Date       : ${new Date(t.eventDate).toDateString()}`,
      `Time       : ${t.startTime ? this.formatTime(t.startTime) : '—'} – ${t.endTime ? this.formatTime(t.endTime) : '—'}`,
      `Attendee   : ${t.userName}`,
      `Amount Paid: ${t.isPaidEvent ? '₹' + t.amountPaid : 'Free'}`,
      `Generated  : ${new Date(t.generatedAt).toLocaleString()}`,
      '============',
    ].join('\n');
    const blob = new Blob([content], { type: 'text/plain' });
    const url  = URL.createObjectURL(blob);
    const a    = document.createElement('a');
    a.href     = url;
    a.download = `ticket-${t.ticketId}-${t.eventTitle.replace(/\s+/g, '_')}.txt`;
    a.click();
    URL.revokeObjectURL(url);
  }

  catLabel(c: number) { return ['', 'Holiday', 'Awareness', 'Public', 'Personal'][c] ?? 'Event'; }

  /** Ensure UTC datetime string is parsed as UTC (append Z if missing) */
  toUtc(dt: string): Date {
    return new Date(dt.endsWith('Z') || dt.includes('+') ? dt : dt + 'Z');
  }

  /** Convert "HH:mm:ss" or "HH:mm" to "h:mm AM/PM" */
  formatTime(time: string): string {
    if (!time) return '—';
    const [hourStr, minuteStr] = time.split(':');
    let h = parseInt(hourStr, 10);
    const m = minuteStr ?? '00';
    const ampm = h >= 12 ? 'PM' : 'AM';
    h = h % 12 || 12;
    return `${h}:${m} ${ampm}`;
  }
}
