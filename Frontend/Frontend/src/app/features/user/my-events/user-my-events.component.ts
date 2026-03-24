import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, Router } from '@angular/router';
import { EventService } from '../../../core/services/event.service';
import { RegistrationService } from '../../../core/services/registration.service';
import { PaymentService } from '../../../core/services/payment.service';
import { ToastService } from '../../../core/services/toast.service';
import { RegistrationStateService } from '../../../core/services/registration-state.service';
import { EventResponse, EventRegistrationResponse, PaymentResponse, ApprovalStatus, RegistrationStatus, PaymentStatus } from '../../../core/models/models';

@Component({
  selector: 'app-user-my-events',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div>
      <!-- Navigation blocked banner -->
      @if (regState.isNavigationBlocked()) {
        <div class="alert alert-warning" style="margin-bottom:16px;border:2px solid var(--warning);">
          <span class="material-icons-round">lock</span>
          <div><strong>Navigation locked</strong> — Please complete payment or cancel your registration before leaving.</div>
        </div>
      }

      <!-- Tabs -->
      <div class="tabs">
        <button type="button" class="tab-btn" [class.active]="tab() === 'browse'" (click)="switchTab('browse')">
          <span class="material-icons-round" style="font-size:16px;vertical-align:middle;">explore</span> Browse Events
        </button>
        <button type="button" class="tab-btn" [class.active]="tab() === 'registered'" (click)="switchTab('registered')">
          <span class="material-icons-round" style="font-size:16px;vertical-align:middle;">how_to_reg</span> My Registrations
        </button>
      </div>

      <!-- BROWSE -->
      @if (tab() === 'browse') {
        <div style="margin-bottom:20px;display:flex;gap:12px;flex-wrap:wrap;">
          <div style="flex:1;min-width:200px;position:relative;">
            <span class="material-icons-round" style="position:absolute;left:10px;top:50%;transform:translateY(-50%);color:var(--text-muted);font-size:20px;">search</span>
            <input [(ngModel)]="search" type="search" class="form-control" placeholder="Search events..." style="padding-left:38px;" />
          </div>
        </div>
        @if (loadingAll()) {
          <div class="loading-center"><div class="spinner"></div></div>
        } @else {
          <div class="events-grid">
            @for (ev of filteredAll(); track ev.eventId) {
              <div class="event-card">
                <div class="event-card-header">
                  <div class="event-card-meta">
                    <span class="badge badge-primary">{{ catLabel(ev.category) }}</span>
                    @if (ev.isPaidEvent) { <span class="badge badge-warning">&#8377;{{ ev.ticketPrice | number:'1.0-0' }}</span> }
                    @else { <span class="badge badge-success">Free</span> }
                    @if (isRegistered(ev.eventId)) { <span class="badge badge-info">Registered</span> }
                  </div>
                  <div class="event-card-title">{{ ev.title }}</div>
                </div>
                <div class="event-card-body">
                  <p>{{ ev.description }}</p>
                  <!-- Seats display -->
                  @if (ev.seatsLimit) {
                    <div style="margin-top:8px;">
                      <span [class]="seatBadgeClass(ev)" style="font-size:.78rem;">{{ seatsDisplay(ev) }}</span>
                    </div>
                  }
                </div>
                <div class="event-card-footer">
                  <div>
                    <div class="event-detail-row"><span class="material-icons-round">calendar_today</span>{{ ev.eventDate | date:'MMM d, y' }}</div>
                    @if (ev.location) { <div class="event-detail-row"><span class="material-icons-round">location_on</span>{{ ev.location | slice:0:20 }}...</div> }
                  </div>
                  <div style="display:flex;gap:8px;align-items:center;">
                    @if (!isRegistered(ev.eventId)) {
                      <button type="button" class="btn btn-primary btn-sm"
                        [disabled]="acting() === ev.eventId"
                        (click)="registerEvent(ev)">
                        @if (acting() === ev.eventId) { <div class="spinner spinner-sm"></div> }
                        @else { Register }
                      </button>
                    } @else {
                      <!-- Only show Pay + Cancel when registered -->
                      @if (ev.isPaidEvent && !isPaid(ev.eventId)) {
                        <button type="button" class="btn btn-warning btn-sm"
                          [disabled]="paying() === ev.eventId"
                          (click)="payEvent(ev)">
                          @if (paying() === ev.eventId) { <div class="spinner spinner-sm"></div> }
                          @else { Pay &#8377;{{ ev.ticketPrice | number:'1.0-0' }} }
                        </button>
                      }
                      <button type="button" class="btn btn-danger btn-sm"
                        [disabled]="cancelling() === getRegId(ev.eventId)"
                        (click)="cancelEvent(ev)">
                        @if (cancelling() === getRegId(ev.eventId)) { <div class="spinner spinner-sm"></div> }
                        @else { Cancel }
                      </button>
                    }
                    <a [routerLink]="regState.isNavigationBlocked() ? null : ['/events', ev.eventId]"
                       class="btn btn-ghost btn-sm btn-icon"
                       (click)="guardNav()">
                      <span class="material-icons-round" style="font-size:18px;">open_in_new</span>
                    </a>
                  </div>
                </div>
              </div>
            }
            @if (filteredAll().length === 0 && !loadingAll()) {
              <div class="empty-state" style="grid-column:1/-1;"><span class="material-icons-round empty-icon">search_off</span><h3>No events found</h3><p>Try different search terms.</p></div>
            }
          </div>
        }
      }

      <!-- MY REGISTRATIONS -->
      @if (tab() === 'registered') {
        @if (myRegs().length === 0) {
          <div class="empty-state"><span class="material-icons-round empty-icon">event_busy</span><h3>No registrations yet</h3><p>Browse events and register to see them here.</p><button type="button" class="btn btn-primary btn-sm" style="margin-top:16px;" (click)="switchTab('browse')">Browse Events</button></div>
        } @else {
          <div class="table-wrapper">
            <table>
              <thead><tr><th>Event</th><th>Date</th><th>Seats</th><th>Type</th><th>Payment</th><th>Status</th><th>Actions</th></tr></thead>
              <tbody>
                @for (reg of myRegs(); track reg.registrationId) {
                  <tr>
                    <td style="font-weight:600;">{{ getEventTitle(reg.eventId) }}</td>
                    <td>{{ getEventDate(reg.eventId) | date:'MMM d, y' }}</td>
                    <td>
                      @if (getEvent(reg.eventId)?.seatsLimit) {
                        <span [class]="seatBadgeClass(getEvent(reg.eventId)!)" style="font-size:.75rem;">
                          {{ seatsDisplay(getEvent(reg.eventId)!) }}
                        </span>
                      } @else {
                        <span class="text-muted" style="font-size:.8rem;">Unlimited</span>
                      }
                    </td>
                    <td>@if (isEventPaid(reg.eventId)) { <span class="badge badge-warning">Paid</span> } @else { <span class="badge badge-success">Free</span> }</td>
                    <td>
                      @if (isEventPaid(reg.eventId)) {
                        @if (isPaid(reg.eventId)) { <span class="badge badge-success">Paid &#10003;</span> }
                        @else {
                          <button type="button" class="btn btn-warning btn-sm"
                            [disabled]="paying() === reg.eventId"
                            (click)="payById(reg.eventId)">
                            @if (paying() === reg.eventId) { <div class="spinner spinner-sm"></div> }
                            @else { Pay }
                          </button>
                        }
                      } @else { <span class="text-muted">&#8212;</span> }
                    </td>
                    <td><span class="badge" [class]="reg.status === 1 ? 'badge-success' : 'badge-danger'">{{ reg.status === 1 ? 'Active' : 'Cancelled' }}</span></td>
                    <td>
                      @if (reg.status === 1) {
                        <button type="button" class="btn btn-danger btn-sm"
                          [disabled]="cancelling() === reg.registrationId"
                          (click)="cancelById(reg)">
                          @if (cancelling() === reg.registrationId) { <div class="spinner spinner-sm"></div> }
                          @else { Cancel }
                        </button>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      }
    </div>
  `
})
export class UserMyEventsComponent implements OnInit {
  private eventSvc = inject(EventService);
  private regSvc   = inject(RegistrationService);
  private paySvc   = inject(PaymentService);
  private toast    = inject(ToastService);
  readonly regState = inject(RegistrationStateService);
  private router   = inject(Router);

  allEvents  = signal<EventResponse[]>([]);
  myRegs     = signal<EventRegistrationResponse[]>([]);
  myPayments = signal<PaymentResponse[]>([]);
  loadingAll = signal(true);
  tab        = signal<'browse'|'registered'>('browse');
  search     = '';
  acting     = signal<number|null>(null);
  paying     = signal<number|null>(null);
  cancelling = signal<number|null>(null);

  filteredAll = computed(() => {
    const s = this.search.toLowerCase();
    return this.allEvents().filter(ev => !s || ev.title.toLowerCase().includes(s) || (ev.location??'').toLowerCase().includes(s));
  });

  ngOnInit() {
    const today = new Date(); today.setHours(0,0,0,0);
    this.eventSvc.getAll().subscribe({
      next: evs => {
        this.allEvents.set(evs.filter(e => e.approvalStatus === ApprovalStatus.APPROVED && new Date(e.eventDate) >= today));
        this.loadingAll.set(false);
      },
      error: () => this.loadingAll.set(false)
    });
    this.regSvc.getMyRegistrations().subscribe({ next: r => this.myRegs.set(r), error: () => {} });
    this.paySvc.getMyPayments().subscribe({ next: p => this.myPayments.set(p), error: () => {} });
  }

  switchTab(t: 'browse'|'registered') {
    if (this.regState.isNavigationBlocked()) {
      this.toast.warning('Please complete payment or cancel registration before leaving.', 'Navigation Blocked');
      return;
    }
    this.tab.set(t);
  }

  guardNav() {
    if (this.regState.isNavigationBlocked()) {
      this.toast.warning('Please complete payment or cancel registration before leaving.', 'Navigation Blocked');
    }
  }

  isRegistered(eid: number) { return this.myRegs().some(r => r.eventId === eid && r.status === RegistrationStatus.REGISTERED); }
  isPaid(eid: number)       { return this.myPayments().some(p => p.eventId === eid && p.status === PaymentStatus.SUCCESS); }
  getRegId(eid: number)     { return this.myRegs().find(r => r.eventId === eid && r.status === RegistrationStatus.REGISTERED)?.registrationId ?? null; }
  getEvent(eid: number)     { return this.allEvents().find(e => e.eventId === eid) ?? null; }
  getEventTitle(eid: number){ return this.allEvents().find(e => e.eventId === eid)?.title ?? `Event #${eid}`; }
  getEventDate(eid: number) { return this.allEvents().find(e => e.eventId === eid)?.eventDate ?? ''; }
  isEventPaid(eid: number)  { return this.allEvents().find(e => e.eventId === eid)?.isPaidEvent ?? false; }

  seatsDisplay(ev: EventResponse): string {
    if (!ev.seatsLimit) return '';
    if (ev.seatsLimit === 0) return 'Event Full';
    if (ev.seatsLimit <= 5) return `\uD83D\uDD25 Only ${ev.seatsLimit} left`;
    return `Seats: ${ev.seatsLimit}`;
  }

  seatBadgeClass(ev: EventResponse): string {
    if (!ev.seatsLimit) return 'badge badge-gray';
    if (ev.seatsLimit === 0) return 'badge badge-danger';
    if (ev.seatsLimit <= 5) return 'badge badge-warning';
    return 'badge badge-success';
  }

  registerEvent(ev: EventResponse) {
    this.acting.set(ev.eventId);
    this.regSvc.register({ eventId: ev.eventId }).subscribe({
      next: res => {
        this.myRegs.update(rs => [...rs, res.data]);
        if (ev.isPaidEvent) {
          this.regState.setPending(ev.eventId, true);
          this.toast.info(`Registered for "${ev.title}". Please complete payment or cancel registration.`, 'Payment Required');
        } else {
          this.toast.success(res.message, 'Registered!');
        }
        this.acting.set(null);
      },
      error: () => this.acting.set(null)
    });
  }

  payEvent(ev: EventResponse) {
    this.paying.set(ev.eventId);
    this.paySvc.create({ eventId: ev.eventId }).subscribe({
      next: p => {
        this.myPayments.update(ps => [...ps, p]);
        this.regState.clearPending();
        this.toast.success(`\u20B9${ev.ticketPrice} paid successfully for "${ev.title}"!`, 'Payment Successful');
        this.paying.set(null);
      },
      error: () => this.paying.set(null)
    });
  }

  payById(eid: number) {
    const ev = this.allEvents().find(e => e.eventId === eid);
    if (ev) this.payEvent(ev);
  }

  cancelEvent(ev: EventResponse) {
    const regId = this.getRegId(ev.eventId);
    if (!regId) return;
    if (!confirm(`Cancel registration for "${ev.title}"? Any payments will be refunded.`)) return;
    this.cancelling.set(regId);
    this.regSvc.cancel(regId).subscribe({
      next: res => {
        this.myRegs.update(rs => rs.map(r => r.registrationId === regId ? {...r, status: RegistrationStatus.CANCELLED} : r));
        this.regState.clearPending();
        this.toast.success(res.message, 'Registration Cancelled');
        this.cancelling.set(null);
      },
      error: () => this.cancelling.set(null)
    });
  }

  cancelById(reg: EventRegistrationResponse) {
    if (!confirm('Cancel this registration? Any payments will be refunded.')) return;
    this.cancelling.set(reg.registrationId);
    this.regSvc.cancel(reg.registrationId).subscribe({
      next: res => {
        this.myRegs.update(rs => rs.map(r => r.registrationId === reg.registrationId ? {...r, status: RegistrationStatus.CANCELLED} : r));
        this.regState.clearPending();
        this.toast.success(res.message, 'Registration Cancelled');
        this.cancelling.set(null);
      },
      error: () => this.cancelling.set(null)
    });
  }

  catLabel(c: number) { return ['','Holiday','Awareness','Public','Personal'][c] ?? 'Event'; }
}
