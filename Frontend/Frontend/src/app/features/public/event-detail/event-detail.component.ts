import { Component, inject, OnInit, OnDestroy, signal, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { EventService } from '../../../core/services/event.service';
import { RegistrationService } from '../../../core/services/registration.service';
import { PaymentService } from '../../../core/services/payment.service';
import { TicketService } from '../../../core/services/ticket.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { RegistrationStateService } from '../../../core/services/registration-state.service';
import { WalletService } from '../../../core/services/wallet.service';
import {
  EventResponse, EventRegistrationResponse, PaymentResponse,
  RegistrationStatus, PaymentStatus, ApprovalStatus, EventCategory, TicketResponse
} from '../../../core/models/models';

type PayMethod = 'upi' | 'netbanking' | 'card' | 'wallet' | '';
const PAY_METHODS = [
  { key: 'upi'        as PayMethod, label: 'UPI',           icon: '⚡', color: '#6366F1' },
  { key: 'netbanking' as PayMethod, label: 'Net Banking',    icon: '🏦', color: '#0EA5E9' },
  { key: 'card'       as PayMethod, label: 'Credit / Debit', icon: '💳', color: '#0D9488' },
  { key: 'wallet'     as PayMethod, label: 'Wallet',         icon: '👛', color: '#F59E0B' },
];

@Component({
  selector: 'app-event-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './event-detail.component.html',
})
export class EventDetailComponent implements OnInit, OnDestroy {
  @Input() id!: string;

  router   = inject(Router);
  auth     = inject(AuthService);
  regState = inject(RegistrationStateService);
  private eventSvc   = inject(EventService);
  private regSvc     = inject(RegistrationService);
  private paySvc     = inject(PaymentService);
  private ticketSvc  = inject(TicketService);
  private walletSvc  = inject(WalletService);
  private toast      = inject(ToastService);

  ApprovalStatus = ApprovalStatus;
  payMethods     = PAY_METHODS;

  event          = signal<EventResponse | null>(null);
  myRegistration = signal<EventRegistrationResponse | null>(null);
  myPayment      = signal<PaymentResponse | null>(null);
  myTicket       = signal<TicketResponse | null>(null);
  loading        = signal(true);
  regLoading     = signal(false);
  payLoading     = signal(false);
  cancelLoading  = signal(false);

  // Payment modal
  showPayModal      = signal(false);
  selectedPayMethod = signal<PayMethod>('');

  // Ticket modal
  ticketModal = signal<TicketResponse | null>(null);

  // Cancel confirm modal
  showCancelModal = signal(false);

  ngOnInit() {
    const id = parseInt(this.id, 10);
    this.eventSvc.getById(id).subscribe({
      next: ev => {
        this.event.set(ev);
        this.loading.set(false);
        if (this.auth.isUser()) this.loadUserData(id);
      },
      error: () => this.loading.set(false)
    });
  }

  ngOnDestroy() {
    // Don't clear pending state on destroy — user must explicitly pay or cancel
  }

  loadUserData(eventId: number) {
    this.regSvc.getMyRegistrations().subscribe({
      next: regs => {
        const r = regs.find(r => r.eventId === eventId && r.status === RegistrationStatus.REGISTERED);
        this.myRegistration.set(r ?? null);
      },
      error: () => {}
    });
    this.paySvc.getMyPayments().subscribe({
      next: pays => {
        const p = pays.find(p => p.eventId === eventId && p.status === PaymentStatus.SUCCESS);
        this.myPayment.set(p ?? null);
        if (this.myRegistration() && this.event()?.isPaidEvent && !p) {
          this.regState.setPending(eventId, true);
        }
      },
      error: () => {}
    });
    // Load ticket if exists
    this.ticketSvc.getByEvent(eventId).subscribe({
      next: t => this.myTicket.set(t),
      error: () => {}
    });
  }

  openPayModal()  { this.selectedPayMethod.set(''); this.showPayModal.set(true); }
  closePayModal() { this.showPayModal.set(false); this.selectedPayMethod.set(''); }

  confirmPay() {
    const ev = this.event()!;
    if (!this.selectedPayMethod()) return;
    this.payLoading.set(true);

    const onSuccess = (payment: any) => {
      this.myPayment.set(payment);
      this.regState.clearPending();
      this.toast.success(`Payment of ₹${ev.ticketPrice} successful!`, 'Payment Successful');
      this.payLoading.set(false);
      this.closePayModal();
      this.ticketSvc.generate(ev.eventId, payment.paymentId).subscribe({
        next: t => { this.myTicket.set(t); this.toast.info('Your ticket is ready!', '🎟 Ticket Generated'); },
        error: () => {}
      });
    };
    const onError = () => this.payLoading.set(false);

    if (this.selectedPayMethod() === 'wallet') {
      this.walletSvc.payWithWallet({ eventId: ev.eventId }).subscribe({ next: onSuccess, error: onError });
    } else {
      this.paySvc.create({ eventId: ev.eventId }).subscribe({ next: onSuccess, error: onError });
    }
  }

  printTicket() { window.print(); }

  downloadTicket() {
    const t = this.ticketModal();
    if (!t) return;
    const content = [
      'EVENT TICKET', '============',
      `Ticket ID  : #${t.ticketId}`,
      `Event      : ${t.eventTitle}`,
      `Location   : ${t.eventLocation}`,
      `Date       : ${new Date(t.eventDate).toDateString()}`,
      `Attendee   : ${t.userName}`,
      `Amount Paid: ${t.isPaidEvent ? '₹' + t.amountPaid : 'Free'}`,
      `Generated  : ${new Date(t.generatedAt).toLocaleString()}`,
      '============',
    ].join('\n');
    const blob = new Blob([content], { type: 'text/plain' });
    const url  = URL.createObjectURL(blob);
    const a    = document.createElement('a');
    a.href = url; a.download = `ticket-${t.ticketId}.txt`; a.click();
    URL.revokeObjectURL(url);
  }

  seatsDisplay(): string {
    const ev = this.event();
    const left = ev?.seatsLeft;
    if (left === undefined || left < 0) return '';
    if (left === 0) return 'Event Full';
    if (left <= 5) return `🔥 Only ${left} seats left!`;
    return `${left} seats left`;
  }

  seatCardStyle(): string {
    const left = this.event()?.seatsLeft;
    if (left === undefined || left < 0) return 'background:var(--surface-2);';
    if (left === 0) return 'background:#FEE2E2;border:1px solid #FCA5A5;';
    if (left <= 5) return 'background:#FEF3C7;border:1px solid #FCD34D;';
    return 'background:var(--success-light);border:1px solid #A7F3D0;';
  }

  seatColor(): string {
    const left = this.event()?.seatsLeft;
    if (left === undefined || left < 0) return 'var(--text-muted)';
    if (left === 0) return '#991B1B';
    if (left <= 5) return '#92400E';
    return '#065F46';
  }

  seatBadgeClass(): string {
    const left = this.event()?.seatsLeft;
    if (left === undefined || left < 0) return 'badge badge-gray';
    if (left === 0) return 'badge badge-danger';
    if (left <= 5) return 'badge badge-warning';
    return 'badge badge-success';
  }

  goBack() {
    if (this.regState.isNavigationBlocked()) {
      this.toast.warning('Please complete payment or cancel registration before leaving.', 'Navigation Blocked');
      return;
    }
    // Go back to wherever the user came from (Browse Events in dashboard, or public events page)
    if (window.history.length > 1) {
      window.history.back();
    } else {
      this.router.navigate([this.auth.isLoggedIn() ? '/user/my-events' : '/events']);
    }
  }

  register() {
    const ev = this.event()!;
    this.regLoading.set(true);
    this.regSvc.register({ eventId: ev.eventId }).subscribe({
      next: res => {
        this.myRegistration.set(res.data);
        if (ev.isPaidEvent) {
          this.regState.setPending(ev.eventId, true);
          this.toast.info(
            `Registered for "${ev.title}". Please complete payment or cancel registration.`,
            'Registration Confirmed'
          );
          this.regLoading.set(false);
        } else {
          // Free event — auto-generate ticket immediately
          this.ticketSvc.generate(ev.eventId).subscribe({
            next: t => {
              this.myTicket.set(t);
              this.toast.success(`Successfully registered for "${ev.title}"! 🎉`, 'Registration Confirmed');
              this.toast.info('Your ticket is ready!', '🎟 Ticket Generated');
            },
            error: () => this.toast.success(`Successfully registered for "${ev.title}"! 🎉`, 'Registration Confirmed')
          });
          this.regLoading.set(false);
        }
      },
      error: () => this.regLoading.set(false)
    });
  }



  cancelRegistration() {
    // Open the cancel confirm modal instead of native confirm()
    this.showCancelModal.set(true);
  }

  confirmCancel() {
    const reg = this.myRegistration()!;
    const ev  = this.event()!;
    const hasPay = !!this.myPayment();
    this.showCancelModal.set(false);
    this.cancelLoading.set(true);
    this.regSvc.cancel(reg.registrationId).subscribe({
      next: () => {
        this.myRegistration.set(null);
        this.myPayment.set(null);
        this.myTicket.set(null);
        this.regState.clearPending();
        const msg = hasPay
          ? `Registration cancelled. Your refund of ₹${this.calcRefund(ev)} will be processed.`
          : `Registration for "${ev.title}" cancelled.`;
        this.toast.info(msg, 'Registration Cancelled');
        this.cancelLoading.set(false);
      },
      error: () => this.cancelLoading.set(false)
    });
  }

  /** Calculate refund amount based on hours before event start */
  calcRefund(ev: EventResponse): number {
    if (!ev.isPaidEvent || !this.myPayment()) return 0;
    const paid = this.myPayment()!.amountPaid;
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

  categoryLabel(c: EventCategory): string {
    const m: Record<number,string> = { 1:'Holiday', 2:'Awareness', 3:'Public', 4:'Personal' };
    return m[c] ?? 'Event';
  }
  categoryBadge(c: EventCategory): string {
    const m: Record<number,string> = { 1:'badge-warning', 2:'badge-info', 3:'badge-primary', 4:'badge-orange' };
    return m[c] ?? 'badge-gray';
  }
  approvalLabel(s: ApprovalStatus): string {
    const m: Record<number,string> = { 1:'Pending Approval', 2:'Approved', 3:'Rejected' };
    return m[s] ?? '';
  }
  approvalBadge(s: ApprovalStatus): string {
    const m: Record<number,string> = { 1:'badge-warning', 2:'badge-success', 3:'badge-danger' };
    return m[s] ?? 'badge-gray';
  }
}
