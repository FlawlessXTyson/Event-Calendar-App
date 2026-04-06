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
              @if (event()!.seatsLeft !== undefined && event()!.seatsLeft! >= 0) {
                <div [style]="seatCardStyle()" style="border-radius:var(--r-sm);padding:12px;">
                  <div style="font-size:.75rem;font-weight:600;text-transform:uppercase;letter-spacing:.05em;margin-bottom:4px;" [style.color]="seatColor()">Seats Left</div>
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
                      @if (myTicket()) {
                        <div style="margin-top:12px;">
                          <button type="button" class="btn btn-sm"
                            (click)="ticketModal.set(myTicket()!)"
                            style="background:linear-gradient(135deg,var(--primary-dark),var(--primary));color:#fff;border:none;border-radius:var(--r-sm);padding:8px 16px;display:flex;align-items:center;gap:6px;font-size:.85rem;font-weight:700;">
                            <span class="material-icons-round" style="font-size:18px;">confirmation_number</span>
                            View / Download Ticket
                          </button>
                        </div>
                      }
                    } @else {
                      <div style="display:flex;align-items:center;gap:8px;margin-bottom:14px;padding:10px 14px;background:var(--warning-light);border-radius:var(--r-sm);">
                        <span class="material-icons-round" style="color:var(--warning);font-size:20px;">payment</span>
                        <span style="font-size:.9rem;font-weight:600;color:#92400E;">Payment pending &#8212; &#8377;{{ event()!.ticketPrice | number }}</span>
                      </div>
                      <div style="display:flex;gap:10px;flex-wrap:wrap;">
                        <button type="button" class="btn btn-primary" [disabled]="payLoading()" (click)="openPayModal()">
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
                    @if (myTicket()) {
                      <div style="margin-bottom:12px;">
                        <button type="button" class="btn btn-sm"
                          (click)="ticketModal.set(myTicket()!)"
                          style="background:linear-gradient(135deg,var(--primary-dark),var(--primary));color:#fff;border:none;border-radius:var(--r-sm);padding:8px 16px;display:flex;align-items:center;gap:6px;font-size:.85rem;font-weight:700;">
                          <span class="material-icons-round" style="font-size:18px;">confirmation_number</span>
                          View / Download Ticket
                        </button>
                      </div>
                    }
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
                    @if (event()!.seatsLeft !== undefined && event()!.seatsLeft! >= 0) {
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

    <!-- Cancel Confirm Modal -->
    @if (showCancelModal() && event()) {
      <div class="modal-backdrop" (click)="showCancelModal.set(false)">
        <div class="modal" (click)="$event.stopPropagation()" style="max-width:420px;">
          <div class="modal-header" style="background:linear-gradient(135deg,#FEE2E2,#FECACA);border-radius:var(--r-lg) var(--r-lg) 0 0;">
            <div style="display:flex;align-items:center;gap:10px;">
              <span class="material-icons-round" style="color:#DC2626;font-size:26px;">cancel</span>
              <div style="font-weight:700;font-size:1rem;color:#7F1D1D;">Cancel Registration</div>
            </div>
            <button type="button" class="btn btn-ghost btn-icon btn-sm" (click)="showCancelModal.set(false)">
              <span class="material-icons-round">close</span>
            </button>
          </div>
          <div class="modal-body" style="padding:24px;">
            <p style="font-size:.95rem;margin-bottom:20px;">
              Are you sure you want to cancel your registration for
              <strong>{{ event()!.title }}</strong>?
            </p>
            @if (event()!.isPaidEvent && myPayment()) {
              <div style="border-radius:var(--r);overflow:hidden;border:1px solid var(--border);">
                <div style="background:var(--surface-2);padding:10px 14px;font-size:.75rem;font-weight:700;color:var(--text-muted);text-transform:uppercase;letter-spacing:.05em;">Refund Breakdown</div>
                <div style="padding:14px;display:flex;flex-direction:column;gap:10px;">
                  <div style="display:flex;justify-content:space-between;font-size:.9rem;">
                    <span style="color:var(--text-secondary);">Amount Paid</span>
                    <span style="font-weight:600;">&#8377;{{ myPayment()!.amountPaid | number }}</span>
                  </div>
                  <div style="display:flex;justify-content:space-between;font-size:.9rem;">
                    <span style="color:var(--text-secondary);">Refund %</span>
                    <span style="font-weight:600;">{{ refundPercent(event()!) }}%</span>
                  </div>
                  <div style="height:1px;background:var(--border);"></div>
                  <div style="display:flex;justify-content:space-between;font-size:1rem;">
                    <span style="font-weight:700;">You will get back</span>
                    <span style="font-weight:800;font-size:1.1rem;"
                      [style.color]="calcRefund(event()!) > 0 ? 'var(--success)' : 'var(--danger)'">
                      &#8377;{{ calcRefund(event()!) | number }}
                    </span>
                  </div>
                </div>
              </div>
              @if (refundPercent(event()!) < 100) {
                <div style="margin-top:12px;display:flex;align-items:flex-start;gap:8px;padding:10px 12px;background:#FEF3C7;border-radius:var(--r-sm);">
                  <span class="material-icons-round" style="font-size:16px;color:#92400E;flex-shrink:0;margin-top:1px;">info</span>
                  <span style="font-size:.8rem;color:#78350F;">
                    @if (refundPercent(event()!) === 0) {
                      The event has already started. No refund will be issued.
                    } @else {
                      Partial refund applies because the event starts in less than
                      @if (refundPercent(event()!) === 25) { 12 hours. }
                      @else if (refundPercent(event()!) === 50) { 24 hours. }
                      @else { 48 hours. }
                    }
                  </span>
                </div>
              }
            } @else if (event()!.isPaidEvent && !myPayment()) {
              <!-- Paid event but not yet paid — simple message -->
              <div style="display:flex;align-items:center;gap:8px;padding:12px;background:#FEF3C7;border-radius:var(--r-sm);">
                <span class="material-icons-round" style="font-size:18px;color:#92400E;">info</span>
                <span style="font-size:.9rem;color:#78350F;">You haven't paid yet — cancelling will simply remove your registration.</span>
              </div>
            } @else {
              <!-- Free event — no message needed -->
            }
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-ghost" (click)="showCancelModal.set(false)">Keep Registration</button>
            <button type="button" class="btn btn-danger" [disabled]="cancelLoading()" (click)="confirmCancel()">
              @if (cancelLoading()) { <div class="spinner spinner-sm"></div> }
              @else { <span class="material-icons-round">cancel</span> }
              Yes, Cancel
            </button>
          </div>
        </div>
      </div>
    }

    <!-- Payment Method Modal -->
    @if (showPayModal()) {
      <div class="modal-backdrop" (click)="closePayModal()">
        <div class="modal" (click)="$event.stopPropagation()" style="max-width:420px;">
          <div class="modal-header">
            <div>
              <div style="font-weight:700;font-size:1rem;">Complete Payment</div>
              <div style="font-size:.85rem;color:var(--text-muted);">{{ event()!.title }}</div>
            </div>
            <button type="button" class="btn btn-ghost btn-icon btn-sm" (click)="closePayModal()"><span class="material-icons-round">close</span></button>
          </div>
          <div class="modal-body">
            <div style="text-align:center;margin-bottom:20px;">
              <div style="font-size:2rem;font-weight:800;color:var(--primary);">&#8377;{{ event()!.ticketPrice | number }}</div>
              <div style="font-size:.85rem;color:var(--text-muted);">Select a payment method to continue</div>
            </div>
            <div style="display:grid;grid-template-columns:1fr 1fr;gap:10px;margin-bottom:16px;">
              @for (m of payMethods; track m.key) {
                <button type="button" (click)="selectedPayMethod.set(m.key)"
                  style="padding:14px 10px;border-radius:var(--r);border:2px solid;cursor:pointer;display:flex;flex-direction:column;align-items:center;gap:6px;font-weight:600;font-size:.85rem;transition:all .15s;background:var(--surface);"
                  [style.borderColor]="selectedPayMethod() === m.key ? m.color : 'var(--border)'"
                  [style.background]="selectedPayMethod() === m.key ? m.color + '18' : 'var(--surface)'"
                  [style.color]="selectedPayMethod() === m.key ? m.color : 'var(--text-secondary)'">
                  <span style="font-size:1.6rem;">{{ m.icon }}</span>{{ m.label }}
                  @if (selectedPayMethod() === m.key) { <span class="material-icons-round" style="font-size:16px;">check_circle</span> }
                </button>
              }
            </div>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-ghost" (click)="closePayModal()">Cancel</button>
            <button type="button" class="btn btn-primary" [disabled]="!selectedPayMethod() || payLoading()" (click)="confirmPay()">
              @if (payLoading()) { <div class="spinner spinner-sm"></div> } @else { <span class="material-icons-round">payment</span> }
              Pay &#8377;{{ event()!.ticketPrice | number }}
            </button>
          </div>
        </div>
      </div>
    }

    <!-- Ticket Modal -->
    @if (ticketModal()) {
      <div class="modal-backdrop" (click)="ticketModal.set(null)">
        <div class="modal" (click)="$event.stopPropagation()" style="max-width:480px;">
          <div class="modal-header" style="background:linear-gradient(135deg,var(--primary-dark),var(--primary));color:#fff;border-radius:var(--r-lg) var(--r-lg) 0 0;">
            <div style="display:flex;align-items:center;gap:10px;">
              <span class="material-icons-round" style="font-size:28px;">confirmation_number</span>
              <div><div style="font-weight:800;font-size:1rem;">Event Ticket</div><div style="font-size:.78rem;opacity:.8;">Ticket #{{ ticketModal()!.ticketId }}</div></div>
            </div>
            <button type="button" class="btn btn-ghost btn-icon btn-sm" style="color:#fff;" (click)="ticketModal.set(null)"><span class="material-icons-round">close</span></button>
          </div>
          <div class="modal-body" style="padding:24px;">
            <h2 style="font-size:1.2rem;margin-bottom:4px;">{{ ticketModal()!.eventTitle }}</h2>
            <p style="font-size:.875rem;color:var(--text-secondary);margin-bottom:20px;">{{ ticketModal()!.eventDescription }}</p>
            <div style="display:grid;grid-template-columns:1fr 1fr;gap:12px;margin-bottom:20px;">
              <div style="background:var(--surface-2);border-radius:var(--r-sm);padding:12px;"><div style="font-size:.7rem;font-weight:700;color:var(--text-muted);text-transform:uppercase;margin-bottom:4px;">Date</div><div style="font-weight:600;font-size:.9rem;">{{ ticketModal()!.eventDate | date:'EEE, MMM d, y' }}</div></div>
              <div style="background:var(--surface-2);border-radius:var(--r-sm);padding:12px;"><div style="font-size:.7rem;font-weight:700;color:var(--text-muted);text-transform:uppercase;margin-bottom:4px;">Location</div><div style="font-weight:600;font-size:.9rem;">{{ ticketModal()!.eventLocation || '—' }}</div></div>
              <div style="background:var(--surface-2);border-radius:var(--r-sm);padding:12px;"><div style="font-size:.7rem;font-weight:700;color:var(--text-muted);text-transform:uppercase;margin-bottom:4px;">Amount</div><div style="font-weight:700;font-size:.9rem;color:var(--primary);">{{ ticketModal()!.isPaidEvent ? '₹' + ticketModal()!.amountPaid : 'Free' }}</div></div>
              <div style="background:var(--surface-2);border-radius:var(--r-sm);padding:12px;"><div style="font-size:.7rem;font-weight:700;color:var(--text-muted);text-transform:uppercase;margin-bottom:4px;">Attendee</div><div style="font-weight:700;font-size:.9rem;">{{ ticketModal()!.userName }}</div></div>
            </div>
            <div style="border:2px dashed var(--border);border-radius:var(--r-sm);padding:14px;text-align:center;">
              <div style="font-size:.7rem;font-weight:700;color:var(--text-muted);text-transform:uppercase;margin-bottom:4px;">Ticket ID</div>
              <div style="font-weight:700;font-family:monospace;font-size:1.2rem;color:var(--primary);">#{{ ticketModal()!.ticketId }}</div>
            </div>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-ghost" (click)="printTicket()"><span class="material-icons-round">print</span> Print</button>
            <button type="button" class="btn btn-primary" (click)="downloadTicket()"><span class="material-icons-round">download</span> Download</button>
          </div>
        </div>
      </div>
    }
  `
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
