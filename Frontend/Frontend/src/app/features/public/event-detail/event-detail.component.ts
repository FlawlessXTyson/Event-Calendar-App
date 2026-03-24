import { Component, inject, OnInit, OnDestroy, signal, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { EventService } from '../../../core/services/event.service';
import { RegistrationService } from '../../../core/services/registration.service';
import { PaymentService } from '../../../core/services/payment.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { RegistrationStateService } from '../../../core/services/registration-state.service';
import {
  EventResponse, EventRegistrationResponse, PaymentResponse,
  RegistrationStatus, PaymentStatus, ApprovalStatus, EventCategory
} from '../../../core/models/models';

@Component({
  selector: 'app-event-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div style="padding:32px max(5%,24px);max-width:860px;margin:0 auto;">
      <div style="margin-bottom:20px;">
        <button type="button" class="btn btn-ghost btn-sm" (click)="goBack()">
          <span class="material-icons-round">arrow_back</span> Back to Events
        </button>
      </div>

      @if (loading()) {
        <div class="card card-body" style="display:flex;flex-direction:column;gap:16px;">
          <div class="skeleton" style="height:32px;width:60%;"></div>
          <div class="skeleton" style="height:20px;width:40%;"></div>
          <div class="skeleton" style="height:80px;"></div>
          <div class="skeleton" style="height:48px;width:200px;"></div>
        </div>
      } @else if (!event()) {
        <div class="card card-body empty-state">
          <span class="material-icons-round empty-icon">event_busy</span>
          <h3>Event Not Found</h3>
          <p>This event doesn't exist or has been removed.</p>
          <a routerLink="/events" class="btn btn-primary" style="margin-top:16px;">Browse Events</a>
        </div>
      } @else {
        @if (regState.isNavigationBlocked()) {
          <div class="alert alert-warning" style="margin-bottom:16px;border:2px solid var(--warning);">
            <span class="material-icons-round">lock</span>
            <div><strong>Navigation locked</strong> — Please complete payment or cancel your registration before leaving this page.</div>
          </div>
        }

        <div class="card">
          <div class="card-header" style="display:block;">
            <div style="display:flex;gap:8px;flex-wrap:wrap;margin-bottom:10px;">
              <span class="badge" [ngClass]="categoryBadge(event()!.category)">{{ categoryLabel(event()!.category) }}</span>
              <span class="badge" [ngClass]="approvalBadge(event()!.approvalStatus)">{{ approvalLabel(event()!.approvalStatus) }}</span>
              @if (event()!.isPaidEvent) {
                <span class="badge badge-purple" style="font-size:.875rem;padding:4px 12px;">&#8377;{{ event()!.ticketPrice | number }} / ticket</span>
              } @else {
                <span class="badge badge-success">Free Entry</span>
              }
            </div>
            <h1 style="font-size:1.6rem;">{{ event()!.title }}</h1>
          </div>

          <div class="card-body">
            <div style="display:grid;grid-template-columns:repeat(auto-fill,minmax(180px,1fr));gap:12px;margin-bottom:24px;">
              <div style="background:var(--surface-2);border-radius:var(--r-sm);padding:12px;">
                <div style="font-size:.75rem;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:.05em;margin-bottom:4px;">Date</div>
                <div style="font-weight:600;">{{ event()!.eventDate | date:'EEE, MMM d, y' }}</div>
              </div>
              @if (event()!.startTime) {
                <div style="background:var(--surface-2);border-radius:var(--r-sm);padding:12px;">
                  <div style="font-size:.75rem;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:.05em;margin-bottom:4px;">Time</div>
                  <div style="font-weight:600;">{{ event()!.startTime }} &#8211; {{ event()!.endTime }}</div>
                </div>
              }
              @if (event()!.location) {
                <div style="background:var(--surface-2);border-radius:var(--r-sm);padding:12px;">
                  <div style="font-size:.75rem;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:.05em;margin-bottom:4px;">Location</div>
                  <div style="font-weight:600;">{{ event()!.location }}</div>
                </div>
              }
              @if (event()!.seatsLimit) {
                <div [style]="seatCardStyle()" style="border-radius:var(--r-sm);padding:12px;">
                  <div style="font-size:.75rem;font-weight:600;text-transform:uppercase;letter-spacing:.05em;margin-bottom:4px;" [style.color]="seatColor()">Seats</div>
                  <div style="font-weight:700;font-size:1rem;" [style.color]="seatColor()">{{ seatsDisplay() }}</div>
                </div>
              }
              @if (event()!.registrationDeadline) {
                <div style="background:var(--warning-light);border-radius:var(--r-sm);padding:12px;">
                  <div style="font-size:.75rem;font-weight:600;color:#92400E;text-transform:uppercase;letter-spacing:.05em;margin-bottom:4px;">Reg. Deadline</div>
                  <div style="font-weight:600;color:#92400E;">{{ event()!.registrationDeadline | date:'MMM d, y h:mm a' }}</div>
                </div>
              }
            </div>

            @if (event()!.description) {
              <div style="margin-bottom:24px;">
                <h3 style="font-size:1rem;margin-bottom:10px;">About This Event</h3>
                <p style="line-height:1.75;">{{ event()!.description }}</p>
              </div>
            }

            <div class="divider"></div>

            @if (event()!.approvalStatus !== ApprovalStatus.APPROVED) {
              <div class="alert alert-warning">
                <span class="material-icons-round">warning_amber</span>
                <div>This event is not yet approved for registration.</div>
              </div>
            } @else if (!auth.isLoggedIn()) {
              <div class="alert alert-info">
                <span class="material-icons-round">info</span>
                <div>
                  <strong>Sign in to register for this event.</strong><br>
                  <a routerLink="/auth/login" style="font-weight:600;">Login</a> or
                  <a routerLink="/auth/register" style="font-weight:600;">Create Account</a>
                </div>
              </div>
            } @else if (!auth.isUser()) {
              <div class="alert alert-info">
                <span class="material-icons-round">info</span>
                <div>Event registration is for user accounts only.</div>
              </div>
            } @else {
              @if (myRegistration()) {
                <div style="background:var(--success-light);border:1px solid #A7F3D0;border-radius:var(--r);padding:20px;">
                  <div style="display:flex;align-items:center;gap:10px;margin-bottom:16px;">
                    <span class="material-icons-round" style="color:var(--success);font-size:28px;">check_circle</span>
                    <div>
                      <div style="font-weight:700;color:#065F46;font-size:1rem;">You're Registered!</div>
                      <div style="font-size:.875rem;color:#047857;">Registration #{{ myRegistration()!.registrationId }}</div>
                    </div>
                  </div>

                  @if (event()!.isPaidEvent) {
                    @if (myPayment()) {
                      <div style="display:flex;align-items:center;gap:8px;padding:10px 14px;background:white;border-radius:var(--r-sm);">
                        <span class="material-icons-round" style="color:var(--success);font-size:20px;">payments</span>
                        <span style="font-weight:600;font-size:.9rem;">Payment confirmed &#8212; &#8377;{{ myPayment()!.amountPaid | number }}</span>
                        <span class="badge badge-success" style="margin-left:auto;">Paid</span>
                      </div>
                    } @else {
                      <div style="display:flex;align-items:center;gap:8px;margin-bottom:14px;padding:10px 14px;background:var(--warning-light);border-radius:var(--r-sm);">
                        <span class="material-icons-round" style="color:var(--warning);font-size:20px;">payment</span>
                        <span style="font-size:.9rem;font-weight:600;color:#92400E;">Payment pending &#8212; &#8377;{{ event()!.ticketPrice | number }}</span>
                      </div>
                      <div style="display:flex;gap:10px;flex-wrap:wrap;">
                        <button type="button" class="btn btn-primary" [disabled]="payLoading()" (click)="pay()">
                          @if (payLoading()) { <div class="spinner spinner-sm"></div> }
                          @else { <span class="material-icons-round">payment</span> }
                          Pay &#8377;{{ event()!.ticketPrice | number }}
                        </button>
                        <button type="button" class="btn btn-ghost btn-sm" style="color:var(--danger);"
                          [disabled]="cancelLoading()" (click)="cancelRegistration()">
                          @if (cancelLoading()) { <div class="spinner spinner-sm"></div> }
                          @else { <span class="material-icons-round">cancel</span> }
                          Cancel Registration
                        </button>
                      </div>
                    }
                  } @else {
                    <button type="button" class="btn btn-ghost btn-sm" style="color:var(--danger);"
                      [disabled]="cancelLoading()" (click)="cancelRegistration()">
                      @if (cancelLoading()) { <div class="spinner spinner-sm"></div> }
                      @else { <span class="material-icons-round">cancel</span> }
                      Cancel Registration
                    </button>
                  }
                </div>
              } @else {
                <div style="display:flex;align-items:center;justify-content:space-between;flex-wrap:wrap;gap:16px;">
                  <div>
                    @if (event()!.isPaidEvent) {
                      <div style="font-size:1.25rem;font-weight:700;">&#8377;{{ event()!.ticketPrice | number }}</div>
                      <div style="font-size:.875rem;color:var(--text-muted);">per ticket &#8226; Register first, then pay</div>
                    } @else {
                      <div style="font-size:1.1rem;font-weight:700;color:var(--success);">Free Entry</div>
                      <div style="font-size:.875rem;color:var(--text-muted);">No payment required</div>
                    }
                    @if (event()!.seatsLimit) {
                      <div style="margin-top:6px;">
                        <span [class]="seatBadgeClass()" style="font-size:.8rem;">{{ seatsDisplay() }}</span>
                      </div>
                    }
                  </div>
                  <button type="button" class="btn btn-primary btn-lg"
                    [disabled]="regLoading()"
                    (click)="register()">
                    @if (regLoading()) { <div class="spinner spinner-sm"></div> }
                    @else { <span class="material-icons-round">how_to_reg</span> }
                    Register Now
                  </button>
                </div>
              }
            }
          </div>
        </div>
      }
    </div>
  `
})
export class EventDetailComponent implements OnInit, OnDestroy {
  @Input() id!: string;

  router   = inject(Router);
  auth     = inject(AuthService);
  regState = inject(RegistrationStateService);
  private eventSvc = inject(EventService);
  private regSvc   = inject(RegistrationService);
  private paySvc   = inject(PaymentService);
  private toast    = inject(ToastService);

  ApprovalStatus = ApprovalStatus;

  event          = signal<EventResponse | null>(null);
  myRegistration = signal<EventRegistrationResponse | null>(null);
  myPayment      = signal<PaymentResponse | null>(null);
  loading        = signal(true);
  regLoading     = signal(false);
  payLoading     = signal(false);
  cancelLoading  = signal(false);

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
        // If registered for paid event without payment, block navigation
        const reg = this.myRegistration();
        if (reg && this.event()?.isPaidEvent && !p) {
          this.regState.setPending(eventId, true);
        }
      },
      error: () => {}
    });
  }

  seatsDisplay(): string {
    const ev = this.event();
    if (!ev?.seatsLimit) return '';
    const limit = ev.seatsLimit;
    if (limit === 0) return 'Event Full';
    if (limit <= 5) return `\uD83D\uDD25 Only ${limit} seats left`;
    return `Seats Left: ${limit}`;
  }

  seatCardStyle(): string {
    const ev = this.event();
    if (!ev?.seatsLimit) return 'background:var(--surface-2);';
    if (ev.seatsLimit === 0) return 'background:#FEE2E2;border:1px solid #FCA5A5;';
    if (ev.seatsLimit <= 5) return 'background:#FEF3C7;border:1px solid #FCD34D;';
    return 'background:var(--success-light);border:1px solid #A7F3D0;';
  }

  seatColor(): string {
    const ev = this.event();
    if (!ev?.seatsLimit) return 'var(--text-muted)';
    if (ev.seatsLimit === 0) return '#991B1B';
    if (ev.seatsLimit <= 5) return '#92400E';
    return '#065F46';
  }

  seatBadgeClass(): string {
    const ev = this.event();
    if (!ev?.seatsLimit) return 'badge badge-gray';
    if (ev.seatsLimit === 0) return 'badge badge-danger';
    if (ev.seatsLimit <= 5) return 'badge badge-warning';
    return 'badge badge-success';
  }

  goBack() {
    if (this.regState.isNavigationBlocked()) {
      this.toast.warning('Please complete payment or cancel registration before leaving.', 'Navigation Blocked');
      return;
    }
    this.router.navigate(['/events']);
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
        } else {
          this.toast.success(`Successfully registered for "${ev.title}"! \uD83C\uDF89`, 'Registration Confirmed');
        }
        this.regLoading.set(false);
      },
      error: () => this.regLoading.set(false)
    });
  }

  pay() {
    const ev = this.event()!;
    this.payLoading.set(true);
    this.paySvc.create({ eventId: ev.eventId }).subscribe({
      next: payment => {
        this.myPayment.set(payment);
        this.regState.clearPending();
        this.toast.success(`Payment of \u20B9${ev.ticketPrice} successful! Your seat is confirmed.`, 'Payment Successful');
        this.payLoading.set(false);
      },
      error: () => this.payLoading.set(false)
    });
  }

  cancelRegistration() {
    const reg = this.myRegistration()!;
    const ev  = this.event()!;
    const hasPay = !!this.myPayment();
    if (!confirm(`Cancel your registration for "${ev.title}"?${hasPay ? '\n\nYour payment will be refunded.' : ''}`)) return;
    this.cancelLoading.set(true);
    this.regSvc.cancel(reg.registrationId).subscribe({
      next: () => {
        this.myRegistration.set(null);
        this.myPayment.set(null);
        this.regState.clearPending();
        const msg = hasPay
          ? `Registration cancelled. Your payment will be refunded.`
          : `Registration for "${ev.title}" cancelled.`;
        this.toast.info(msg, 'Registration Cancelled');
        this.cancelLoading.set(false);
      },
      error: () => this.cancelLoading.set(false)
    });
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
