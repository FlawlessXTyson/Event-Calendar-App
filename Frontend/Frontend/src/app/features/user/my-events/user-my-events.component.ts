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
  template: `
    <div>

      <!-- Payment pending banner — ONLY for paid events awaiting payment -->
      @if (regState.isNavigationBlocked()) {
        <div style="margin-bottom:16px;padding:14px 18px;background:linear-gradient(135deg,#FEF3C7,#FDE68A);border-left:4px solid var(--warning);border-radius:var(--r);display:flex;align-items:center;gap:12px;box-shadow:0 2px 8px rgba(245,158,11,.15);">
          <span class="material-icons-round" style="color:#92400E;font-size:22px;flex-shrink:0;">lock</span>
          <div>
            <div style="font-weight:700;color:#92400E;font-size:.9rem;">Payment Pending</div>
            <div style="font-size:.82rem;color:#78350F;margin-top:2px;">Please complete your payment or cancel the registration before navigating away.</div>
          </div>
        </div>
      }

      <!-- Tabs -->
      <div class="tabs">
        <button type="button" class="tab-btn" [class.active]="tab() === 'browse'" (click)="switchTab('browse')">
          <span class="material-icons-round" style="font-size:16px;vertical-align:middle;">explore</span> Browse Events
        </button>
        <button type="button" class="tab-btn" [class.active]="tab() === 'registered'" (click)="switchTab('registered')">
          <span class="material-icons-round" style="font-size:16px;vertical-align:middle;">how_to_reg</span>
          My Registrations
          @if (activeRegCount() > 0) {
            <span style="margin-left:6px;background:var(--primary);color:#fff;font-size:.7rem;font-weight:700;padding:1px 7px;border-radius:var(--r-full);vertical-align:middle;">{{ activeRegCount() }}</span>
          }
        </button>
        <button type="button" class="tab-btn" [class.active]="tab() === 'attended'" (click)="switchTab('attended')">
          <span class="material-icons-round" style="font-size:16px;vertical-align:middle;">verified</span>
          Events Attended
          @if (attendedCount() > 0) {
            <span style="margin-left:6px;background:#065F46;color:#fff;font-size:.7rem;font-weight:700;padding:1px 7px;border-radius:var(--r-full);vertical-align:middle;">{{ attendedCount() }}</span>
          }
        </button>
      </div>

      <!-- ── BROWSE TAB ─────────────────────────────────────────────────── -->
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
                    @if (ev.hasEnded) {
                      <span class="badge badge-gray">Event Ended</span>
                    } @else if (isRegistered(ev.eventId)) {
                      @if (ev.hasStarted) {
                        <span class="badge" style="background:#FEF3C7;color:#92400E;">🟡 Ongoing</span>
                      } @else {
                        <span class="badge badge-info">Registered</span>
                      }
                    }
                  </div>
                  <div class="event-card-title">{{ ev.title }}</div>
                </div>

                <div class="event-card-body">
                  <p>{{ ev.description }}</p>
                  @if (ev.hasEnded) {
                    <div style="margin-top:8px;display:flex;align-items:center;gap:6px;background:var(--surface-2);border-radius:var(--r-sm);padding:6px 10px;">
                      <span class="material-icons-round" style="font-size:15px;color:var(--text-muted);">event_busy</span>
                      <span style="font-size:.78rem;font-weight:700;color:var(--text-muted);">Event has ended</span>
                    </div>
                  } @else if (isDeadlinePassed(ev) && !isRegistered(ev.eventId)) {
                    <div style="margin-top:8px;display:flex;align-items:center;gap:6px;background:#FEE2E2;border-radius:var(--r-sm);padding:6px 10px;">
                      <span class="material-icons-round" style="font-size:15px;color:#991B1B;">lock</span>
                      <span style="font-size:.78rem;font-weight:700;color:#991B1B;">Registration Closed</span>
                    </div>
                  } @else if (!isDeadlinePassed(ev)) {
                    @if (ev.seatsLeft !== undefined && ev.seatsLeft >= 0) {
                      <div style="margin-top:8px;">
                        <span [class]="seatBadgeClass(ev)" style="font-size:.78rem;">{{ seatsDisplay(ev) }}</span>
                      </div>
                    }
                  }
                  @if (ev.isPaidEvent && ev.refundCutoffDays !== undefined && !ev.hasEnded) {
                    <div style="margin-top:6px;font-size:.75rem;color:var(--text-muted);display:flex;align-items:center;gap:4px;">
                      <span class="material-icons-round" style="font-size:13px;">info</span>
                      {{ ev.earlyRefundPercentage }}% refund if cancelled {{ ev.refundCutoffDays }}+ days before event
                    </div>
                  }
                </div>

                <div class="event-card-footer">
                  <div>
                    <div class="event-detail-row"><span class="material-icons-round">calendar_today</span>{{ ev.eventDate | date:'MMM d, y' }}</div>
                    @if (ev.startTime) {
                      <div class="event-detail-row" style="margin-top:3px;">
                        <span class="material-icons-round">schedule</span>
                        {{ formatTime(ev.startTime) }}{{ ev.endTime ? ' – ' + formatTime(ev.endTime) : '' }}
                      </div>
                    }
                    @if (ev.registrationDeadline) {
                      <div class="event-detail-row" style="margin-top:3px;">
                        <span class="material-icons-round" style="font-size:14px;color:var(--warning);">event_busy</span>
                        <span style="font-size:.75rem;color:var(--warning);font-weight:600;">
                          Reg. deadline: {{ toUtc(ev.registrationDeadline) | date:'MMM d, h:mm a' }}
                        </span>
                      </div>
                    }
                    @if (ev.location) { <div class="event-detail-row" style="margin-top:3px;"><span class="material-icons-round">location_on</span>{{ ev.location | slice:0:22 }}...</div> }
                  </div>

                  <div style="display:flex;gap:6px;align-items:center;flex-wrap:wrap;">
                    @if (ev.hasEnded) {
                      <!-- Event ended — no actions for anyone -->
                      @if (isRegistered(ev.eventId) && getTicket(ev.eventId)) {
                        <button type="button" class="btn btn-sm"
                          (click)="openTicketDetail(getTicket(ev.eventId)!)"
                          title="View Ticket"
                          style="background:var(--surface-2);color:var(--text-secondary);border:1px solid var(--border);border-radius:var(--r-sm);padding:6px 10px;display:flex;align-items:center;gap:4px;font-size:.78rem;">
                          <span class="material-icons-round" style="font-size:16px;">confirmation_number</span>
                          Ticket
                        </button>
                      }
                    } @else if (!isRegistered(ev.eventId)) {
                      <!-- Not registered -->
                      <button type="button" class="btn btn-primary btn-sm"
                        [disabled]="acting() === ev.eventId || isDeadlinePassed(ev)"
                        (click)="registerEvent(ev)">
                        @if (acting() === ev.eventId) { <div class="spinner spinner-sm"></div> }
                        @else if (isDeadlinePassed(ev)) { Closed }
                        @else { Register }
                      </button>
                    } @else {
                      <!-- Registered -->
                      @if (!isFreeEvent(ev) && !isPaid(ev.eventId) && !ev.hasStarted) {
                        <button type="button" class="btn btn-warning btn-sm" (click)="openPayModal(ev)">
                          <span class="material-icons-round" style="font-size:14px;">payment</span>
                          Pay &#8377;{{ ev.ticketPrice | number:'1.0-0' }}
                        </button>
                      }
                      <!-- Ticket — only show after payment confirmed (or free event) -->
                      @if (getTicket(ev.eventId) && (isFreeEvent(ev) || isPaid(ev.eventId))) {
                        <button type="button" class="btn btn-sm"
                          (click)="openTicketDetail(getTicket(ev.eventId)!)"
                          title="View / Download Ticket"
                          style="background:linear-gradient(135deg,var(--primary-dark),var(--primary));color:#fff;border:none;border-radius:var(--r-sm);padding:6px 10px;display:flex;align-items:center;gap:4px;font-size:.78rem;font-weight:700;box-shadow:0 2px 8px rgba(13,148,136,.3);">
                          <span class="material-icons-round" style="font-size:16px;">confirmation_number</span>
                          Ticket
                        </button>
                      }
                      <!-- Cancel — only before event starts -->
                      @if (!ev.hasStarted) {
                        <button type="button" class="btn btn-danger btn-sm"
                          [disabled]="cancelling() === getRegId(ev.eventId)"
                          (click)="cancelEvent(ev)">
                          @if (cancelling() === getRegId(ev.eventId)) { <div class="spinner spinner-sm"></div> } @else { Cancel }
                        </button>
                      }
                    }
                    <!-- View details link — always available -->
                    <a [routerLink]="(regState.isNavigationBlocked() && ev.isPaidEvent) ? null : ['/events', ev.eventId]"
                       class="btn btn-ghost btn-sm btn-icon"
                       (click)="guardNav(ev)">
                      <span class="material-icons-round" style="font-size:18px;">open_in_new</span>
                    </a>
                  </div>
                </div>
              </div>
            }
            @if (filteredAll().length === 0) {
              <div class="empty-state" style="grid-column:1/-1;">
                <span class="material-icons-round empty-icon">search_off</span>
                <h3>No events found</h3>
              </div>
            }
          </div>
        }
      }

      <!-- ── MY REGISTRATIONS TAB ──────────────────────────────────────── -->
      @if (tab() === 'registered') {
        @if (activeRegCount() === 0) {
          <div class="empty-state">
            <span class="material-icons-round empty-icon">event_busy</span>
            <h3>No registrations yet</h3>
            <button type="button" class="btn btn-primary btn-sm" style="margin-top:16px;" (click)="switchTab('browse')">Browse Events</button>
          </div>
        } @else {
          <div class="table-wrapper">
            <table>
              <thead>
                <tr><th>Event</th><th>Date</th><th>Type</th><th>Payment</th><th>Status</th><th>Actions</th></tr>
              </thead>
              <tbody>
                @for (reg of activeRegs(); track reg.registrationId) {
                  <tr>
                    <td style="font-weight:600;">{{ getEventTitle(reg.eventId) }}</td>
                    <td>{{ getEventDate(reg.eventId) | date:'MMM d, y' }}</td>
                    <td>
                      @if (isEventPaid(reg.eventId)) { <span class="badge badge-warning">Paid</span> }
                      @else { <span class="badge badge-success">Free</span> }
                    </td>
                    <td>
                      @if (!isFreeEvent(getEvent(reg.eventId))) {
                        @if (isPaid(reg.eventId)) { <span class="badge badge-success">Paid ✓</span> }
                        @else {
                          <button type="button" class="btn btn-warning btn-sm" (click)="openPayModalById(reg.eventId)">
                            Pay
                          </button>
                        }
                      } @else {
                        <span class="badge badge-success">Free ✓</span>
                      }
                    </td>
                    <td>
                      <span class="badge" [class]="reg.status === 1 ? 'badge-success' : 'badge-danger'">
                        {{ reg.status === 1 ? 'Active' : 'Cancelled' }}
                      </span>
                    </td>
                    <td>
                      <div style="display:flex;gap:6px;">
                        @if (reg.status === 1 && !getEvent(reg.eventId)?.hasStarted) {
                          <button type="button" class="btn btn-danger btn-sm"
                            [disabled]="cancelling() === reg.registrationId"
                            (click)="cancelById(reg)">
                            @if (cancelling() === reg.registrationId) { <div class="spinner spinner-sm"></div> } @else { Cancel }
                          </button>
                        }
                        @if (getTicket(reg.eventId) && !getEvent(reg.eventId)?.hasEnded) {
                          <button type="button" class="btn btn-ghost btn-sm btn-icon"
                            (click)="openTicketDetail(getTicket(reg.eventId)!)"
                            title="View Ticket">
                            <span class="material-icons-round" style="font-size:18px;color:var(--primary);">confirmation_number</span>
                          </button>
                        }
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      }

      <!-- ── EVENTS ATTENDED TAB ──────────────────────────────────────── -->
      @if (tab() === 'attended') {
        @if (attendedRegs().length === 0) {
          <div class="empty-state">
            <span class="material-icons-round empty-icon">verified</span>
            <h3>No attended events yet</h3>
            <p>Events you've registered for will appear here once they end.</p>
          </div>
        } @else {
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
                      <div style="font-size:.75rem;color:var(--text-muted);">{{ getEventDate(reg.eventId) | date:'MMM d, y' }}</div>
                    </td>
                    <td>{{ getEventDate(reg.eventId) | date:'MMM d, y' }}</td>
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
                      <span class="badge" style="background:#D1FAE5;color:#065F46;font-weight:700;">
                        ✅ Attended
                      </span>
                    </td>
                    <td>
                      @if (getTicket(reg.eventId)) {
                        <button type="button" class="btn btn-sm"
                          (click)="openTicketDetail(getTicket(reg.eventId)!)"
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
      }

      <!-- ── PAYMENT METHOD MODAL ──────────────────────────────────────── -->
      @if (payModalEvent()) {
        <div class="modal-backdrop" (click)="closePayModal()">
          <div class="modal" (click)="$event.stopPropagation()" style="max-width:420px;">
            <div class="modal-header">
              <div>
                <div style="font-weight:700;font-size:1rem;">Complete Payment</div>
                <div style="font-size:.85rem;color:var(--text-muted);">{{ payModalEvent()!.title }}</div>
              </div>
              <button type="button" class="btn btn-ghost btn-icon btn-sm" (click)="closePayModal()">
                <span class="material-icons-round">close</span>
              </button>
            </div>
            <div class="modal-body">
              <div style="text-align:center;margin-bottom:20px;">
                <div style="font-size:2rem;font-weight:800;color:var(--primary);">&#8377;{{ payModalEvent()!.ticketPrice | number:'1.0-0' }}</div>
                <div style="font-size:.85rem;color:var(--text-muted);">Select a payment method to continue</div>
              </div>
              <div style="display:grid;grid-template-columns:1fr 1fr;gap:10px;margin-bottom:20px;">
                @for (m of payMethods; track m.key) {
                  <button type="button"
                    (click)="selectedPayMethod.set(m.key)"
                    style="padding:14px 10px;border-radius:var(--r);border:2px solid;cursor:pointer;display:flex;flex-direction:column;align-items:center;gap:6px;font-weight:600;font-size:.85rem;transition:all .15s;background:var(--surface);"
                    [style.borderColor]="selectedPayMethod() === m.key ? m.color : 'var(--border)'"
                    [style.background]="selectedPayMethod() === m.key ? m.color + '18' : 'var(--surface)'"
                    [style.color]="selectedPayMethod() === m.key ? m.color : 'var(--text-secondary)'">
                    <span style="font-size:1.6rem;">{{ m.icon }}</span>
                    {{ m.label }}
                    @if (selectedPayMethod() === m.key) {
                      <span class="material-icons-round" style="font-size:16px;">check_circle</span>
                    }
                  </button>
                }
              </div>
              @if (payModalEvent()!.refundCutoffDays !== undefined) {
                <div style="background:var(--surface-2);border-radius:var(--r-sm);padding:10px 14px;font-size:.8rem;color:var(--text-secondary);display:flex;gap:8px;">
                  <span class="material-icons-round" style="font-size:16px;color:var(--warning);flex-shrink:0;">info</span>
                  <span>Refund policy: <strong>{{ payModalEvent()!.earlyRefundPercentage }}%</strong> refund if cancelled {{ payModalEvent()!.refundCutoffDays }}+ days before event.</span>
                </div>
              }
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-ghost" (click)="closePayModal()">Cancel</button>
              <button type="button" class="btn btn-primary"
                [disabled]="!selectedPayMethod() || paying() === payModalEvent()!.eventId"
                (click)="confirmPay()">
                @if (paying() === payModalEvent()!.eventId) { <div class="spinner spinner-sm"></div> }
                @else { <span class="material-icons-round">payment</span> }
                Pay &#8377;{{ payModalEvent()!.ticketPrice | number:'1.0-0' }}
              </button>
            </div>
          </div>
        </div>
      }

      <!-- ── CANCEL CONFIRM MODAL ──────────────────────────────────────── -->
      @if (showCancelModal && cancelConfirmEvent().ev) {
        <div class="modal-backdrop" (click)="closeCancelModal()">
          <div class="modal" (click)="$event.stopPropagation()" style="max-width:420px;">
            <div class="modal-header" style="background:linear-gradient(135deg,#FEE2E2,#FECACA);border-radius:var(--r-lg) var(--r-lg) 0 0;">
              <div style="display:flex;align-items:center;gap:10px;">
                <span class="material-icons-round" style="color:#DC2626;font-size:26px;">cancel</span>
                <div style="font-weight:700;font-size:1rem;color:#7F1D1D;">Cancel Registration</div>
              </div>
              <button type="button" class="btn btn-ghost btn-icon btn-sm" (click)="closeCancelModal()">
                <span class="material-icons-round">close</span>
              </button>
            </div>
            <div class="modal-body" style="padding:24px;">
              <p style="font-size:.95rem;margin-bottom:20px;">
                Are you sure you want to cancel your registration for
                <strong>{{ cancelConfirmEvent().ev!.title }}</strong>?
              </p>
              @if (cancelConfirmEvent().ev!.isPaidEvent && isPaid(cancelConfirmEvent().ev!.eventId)) {                <div style="border-radius:var(--r);overflow:hidden;border:1px solid var(--border);">
                  <div style="background:var(--surface-2);padding:10px 14px;font-size:.75rem;font-weight:700;color:var(--text-muted);text-transform:uppercase;letter-spacing:.05em;">Refund Breakdown</div>
                  <div style="padding:14px;display:flex;flex-direction:column;gap:10px;">
                    <div style="display:flex;justify-content:space-between;font-size:.9rem;">
                      <span style="color:var(--text-secondary);">Amount Paid</span>
                      <span style="font-weight:600;">&#8377;{{ getPaidAmount(cancelConfirmEvent().ev!.eventId) | number }}</span>
                    </div>
                    <div style="display:flex;justify-content:space-between;font-size:.9rem;">
                      <span style="color:var(--text-secondary);">Refund %</span>
                      <span style="font-weight:600;">{{ refundPercent(cancelConfirmEvent().ev!) }}%</span>
                    </div>
                    <div style="height:1px;background:var(--border);"></div>
                    <div style="display:flex;justify-content:space-between;font-size:1rem;">
                      <span style="font-weight:700;">You will get back</span>
                      <span style="font-weight:800;font-size:1.1rem;"
                        [style.color]="calcRefund(cancelConfirmEvent().ev!) > 0 ? 'var(--success)' : 'var(--danger)'">
                        &#8377;{{ calcRefund(cancelConfirmEvent().ev!) | number }}
                      </span>
                    </div>
                  </div>
                </div>
                @if (refundPercent(cancelConfirmEvent().ev!) < 100) {
                  <div style="margin-top:12px;display:flex;align-items:flex-start;gap:8px;padding:10px 12px;background:#FEF3C7;border-radius:var(--r-sm);">
                    <span class="material-icons-round" style="font-size:16px;color:#92400E;flex-shrink:0;margin-top:1px;">info</span>
                    <span style="font-size:.8rem;color:#78350F;">
                      @if (refundPercent(cancelConfirmEvent().ev!) === 0) {
                        The event has already started. No refund will be issued.
                      } @else {
                        Partial refund applies because the event starts in less than
                        @if (refundPercent(cancelConfirmEvent().ev!) === 25) { 12 hours. }
                        @else if (refundPercent(cancelConfirmEvent().ev!) === 50) { 24 hours. }
                        @else { 48 hours. }
                      }
                    </span>
                  </div>
                }
              } @else if (cancelConfirmEvent().ev!.isPaidEvent && !isPaid(cancelConfirmEvent().ev!.eventId)) {
                <!-- Paid event but not yet paid — just confirm, no refund info needed -->
                <div style="display:flex;align-items:center;gap:8px;padding:12px;background:#FEF3C7;border-radius:var(--r-sm);">
                  <span class="material-icons-round" style="font-size:18px;color:#92400E;">info</span>
                  <span style="font-size:.9rem;color:#78350F;">You haven't paid yet — cancelling will simply remove your registration.</span>
                </div>
              } @else {
                <!-- Free event — no payment info at all -->
              }
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-ghost" (click)="closeCancelModal()">Keep Registration</button>
              <button type="button" class="btn btn-danger"
                [disabled]="cancelling() !== null"
                (click)="confirmCancel()">
                @if (cancelling() !== null) { <div class="spinner spinner-sm"></div> }
                @else { <span class="material-icons-round">cancel</span> }
                Yes, Cancel
              </button>
            </div>
          </div>
        </div>
      }

      <!-- ── TICKET FULL-DETAIL MODAL (print/download) ─────────────────── -->
      @if (ticketDetailModal()) {
        <div class="modal-backdrop" (click)="ticketDetailModal.set(null)">
          <div class="modal" (click)="$event.stopPropagation()" style="max-width:480px;">
            <div class="modal-header" style="background:linear-gradient(135deg,var(--primary-dark),var(--primary));color:#fff;border-radius:var(--r-lg) var(--r-lg) 0 0;">
              <div style="display:flex;align-items:center;gap:10px;">
                <span class="material-icons-round" style="font-size:28px;">confirmation_number</span>
                <div>
                  <div style="font-weight:800;font-size:1rem;">Event Ticket</div>
                  <div style="font-size:.78rem;opacity:.8;">Ticket #{{ ticketDetailModal()!.ticketId }}</div>
                </div>
              </div>
              <button type="button" class="btn btn-ghost btn-icon btn-sm" style="color:#fff;" (click)="ticketDetailModal.set(null)">
                <span class="material-icons-round">close</span>
              </button>
            </div>
            <div class="modal-body" style="padding:24px;">
              <h2 style="font-size:1.2rem;margin-bottom:4px;">{{ ticketDetailModal()!.eventTitle }}</h2>
              <p style="font-size:.875rem;color:var(--text-secondary);margin-bottom:20px;">{{ ticketDetailModal()!.eventDescription }}</p>
              <div style="display:grid;grid-template-columns:1fr 1fr;gap:12px;margin-bottom:20px;">
                <div style="background:var(--surface-2);border-radius:var(--r-sm);padding:12px;">
                  <div style="font-size:.7rem;font-weight:700;color:var(--text-muted);text-transform:uppercase;margin-bottom:4px;">Date</div>
                  <div style="font-weight:600;font-size:.9rem;">{{ ticketDetailModal()!.eventDate | date:'EEE, MMM d, y' }}</div>
                </div>
                <div style="background:var(--surface-2);border-radius:var(--r-sm);padding:12px;">
                  <div style="font-size:.7rem;font-weight:700;color:var(--text-muted);text-transform:uppercase;margin-bottom:4px;">Time</div>
                  <div style="font-weight:600;font-size:.9rem;">
                    {{ ticketDetailModal()!.startTime ? formatTime(ticketDetailModal()!.startTime!) : '—' }}
                    @if (ticketDetailModal()!.endTime) { – {{ formatTime(ticketDetailModal()!.endTime!) }} }
                  </div>
                </div>
                <div style="background:var(--surface-2);border-radius:var(--r-sm);padding:12px;">
                  <div style="font-size:.7rem;font-weight:700;color:var(--text-muted);text-transform:uppercase;margin-bottom:4px;">Location</div>
                  <div style="font-weight:600;font-size:.9rem;">{{ ticketDetailModal()!.eventLocation || '—' }}</div>
                </div>
                <div style="background:var(--surface-2);border-radius:var(--r-sm);padding:12px;">
                  <div style="font-size:.7rem;font-weight:700;color:var(--text-muted);text-transform:uppercase;margin-bottom:4px;">Amount</div>
                  <div style="font-weight:700;font-size:.9rem;color:var(--primary);">{{ ticketDetailModal()!.isPaidEvent ? ('₹' + ticketDetailModal()!.amountPaid) : 'Free' }}</div>
                </div>
              </div>
              <div style="border:2px dashed var(--border);border-radius:var(--r-sm);padding:14px;display:flex;align-items:center;justify-content:space-between;">
                <div>
                  <div style="font-size:.7rem;font-weight:700;color:var(--text-muted);text-transform:uppercase;margin-bottom:2px;">Attendee</div>
                  <div style="font-weight:700;">{{ ticketDetailModal()!.userName }}</div>
                </div>
                <div style="text-align:right;">
                  <div style="font-size:.7rem;font-weight:700;color:var(--text-muted);text-transform:uppercase;margin-bottom:2px;">Ticket ID</div>
                  <div style="font-weight:700;font-family:monospace;font-size:1rem;color:var(--primary);">#{{ ticketDetailModal()!.ticketId }}</div>
                </div>
              </div>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-ghost" (click)="printTicket()">
                <span class="material-icons-round">print</span> Print
              </button>
              <button type="button" class="btn btn-primary" (click)="downloadTicketPdf()">
                <span class="material-icons-round">download</span> Download
              </button>
            </div>
          </div>
        </div>
      }

    </div>

    <!-- Ticket detail modal handles everything — no global widget needed -->
  `,
  styles: [`
    @keyframes slideUp {
      from { opacity: 0; transform: translateY(20px); }
      to   { opacity: 1; transform: translateY(0); }
    }
  `]
})
export class UserMyEventsComponent implements OnInit {
  private eventSvc  = inject(EventService);
  private regSvc    = inject(RegistrationService);
  private paySvc    = inject(PaymentService);
  private ticketSvc = inject(TicketService);
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
    this.paySvc.create({ eventId: ev.eventId }).subscribe({
      next: p => {
        this.myPayments.update(ps => [...ps, p]);
        this.regState.clearPending();
        this.toast.success(`₹${ev.ticketPrice} paid via ${this.selectedPayMethod().toUpperCase()} for "${ev.title}"!`, 'Payment Successful');
        this.paying.set(null);
        this.closePayModal();
        // Auto-generate ticket after payment → show in corner widget
        this.ticketSvc.generate(ev.eventId, p.paymentId).subscribe({
          next: t => {
            this.myTickets.update(ts => [...ts, t]);
          },
          error: () => {}
        });
      },
      error: () => this.paying.set(null)
    });
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
