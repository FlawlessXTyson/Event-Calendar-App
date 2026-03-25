import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EventService } from '../../../core/services/event.service';
import { RegistrationService } from '../../../core/services/registration.service';
import { PaymentService } from '../../../core/services/payment.service';
import {
  EventResponse, EventRegistrationResponse,
  PaymentResponse, RegistrationStatus, PaymentStatus, ApprovalStatus
} from '../../../core/models/models';

interface EventWithStats {
  event: EventResponse;
  regs: EventRegistrationResponse[];
  payments: PaymentResponse[];
  loading: boolean;
  expanded: boolean;
}

@Component({
  selector: 'app-event-registrations',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div>
      <div style="margin-bottom:24px;">
        <h1 style="font-size:1.5rem;">Event Registrations</h1>
        <p>Click any event to see its attendees</p>
      </div>

      @if (loadingEvents()) {
        <div class="loading-center"><div class="spinner"></div></div>
      } @else if (eventList().length === 0) {
        <div class="empty-state">
          <span class="material-icons-round empty-icon">event_busy</span>
          <h3>No events yet</h3>
          <p>Create an event first to start seeing registrations.</p>
        </div>
      } @else {
        <!-- Summary stats -->
        <div class="stats-grid" style="margin-bottom:24px;">
          <div class="stat-card">
            <div class="stat-icon" style="background:var(--primary-light);"><span class="material-icons-round" style="color:var(--primary);">event</span></div>
            <div class="stat-value">{{ eventList().length }}</div>
            <div class="stat-label">Total Events</div>
          </div>
          <div class="stat-card">
            <div class="stat-icon" style="background:var(--success-light);"><span class="material-icons-round" style="color:var(--success);">how_to_reg</span></div>
            <div class="stat-value">{{ totalRegistrations() }}</div>
            <div class="stat-label">Total Registrations</div>
          </div>
          <div class="stat-card">
            <div class="stat-icon" style="background:var(--warning-light);"><span class="material-icons-round" style="color:var(--warning);">payments</span></div>
            <div class="stat-value">₹{{ totalRevenue() | number:'1.0-0' }}</div>
            <div class="stat-label">Total Revenue</div>
          </div>
        </div>

        <!-- Event cards -->
        <div style="display:flex;flex-direction:column;gap:12px;">
          @for (item of eventList(); track item.event.eventId) {
            <div class="card" style="overflow:hidden;">

              <!-- Event header row — click to expand -->
              <div style="padding:16px 20px;display:flex;align-items:center;gap:16px;cursor:pointer;transition:background .15s;"
                (click)="toggle(item)"
                onmouseover="this.style.background='var(--surface-2)'"
                onmouseout="this.style.background=''">

                <!-- Expand icon -->
                <span class="material-icons-round" style="color:var(--primary);font-size:22px;flex-shrink:0;transition:transform .2s;"
                  [style.transform]="item.expanded ? 'rotate(90deg)' : 'rotate(0)'">
                  chevron_right
                </span>

                <!-- Event info -->
                <div style="flex:1;min-width:0;">
                  <div style="font-weight:700;font-size:.95rem;" class="truncate">{{ item.event.title }}</div>
                  <div style="display:flex;gap:12px;flex-wrap:wrap;margin-top:4px;">
                    <span style="font-size:.78rem;color:var(--text-muted);display:flex;align-items:center;gap:4px;">
                      <span class="material-icons-round" style="font-size:14px;">calendar_today</span>
                      {{ item.event.eventDate | date:'EEE, MMM d, y' }}
                    </span>
                    @if (item.event.location) {
                      <span style="font-size:.78rem;color:var(--text-muted);display:flex;align-items:center;gap:4px;">
                        <span class="material-icons-round" style="font-size:14px;">location_on</span>
                        {{ item.event.location }}
                      </span>
                    }
                  </div>
                </div>

                <!-- Badges -->
                <div style="display:flex;gap:8px;align-items:center;flex-shrink:0;flex-wrap:wrap;">
                  @if (item.event.isPaidEvent) {
                    <span class="badge badge-purple">₹{{ item.event.ticketPrice | number }}</span>
                  } @else {
                    <span class="badge badge-success">Free</span>
                  }
                  <span class="badge" [class]="approvalBadge(item.event.approvalStatus)">
                    {{ approvalLabel(item.event.approvalStatus) }}
                  </span>
                  <!-- Registrations count pill -->
                  @if (item.regs.length > 0) {
                    <span style="background:var(--primary);color:#fff;font-size:.75rem;font-weight:700;padding:3px 10px;border-radius:var(--r-full);">
                      {{ activeCount(item) }} attendee{{ activeCount(item) !== 1 ? 's' : '' }}
                    </span>
                  } @else if (!item.loading) {
                    <span style="background:var(--surface-2);color:var(--text-muted);font-size:.75rem;font-weight:600;padding:3px 10px;border-radius:var(--r-full);">
                      No registrations
                    </span>
                  }
                </div>
              </div>

              <!-- Expanded attendees panel -->
              @if (item.expanded) {
                <div style="border-top:1px solid var(--border);">
                  @if (item.loading) {
                    <div style="padding:32px;text-align:center;"><div class="spinner" style="margin:0 auto;"></div></div>
                  } @else if (item.regs.length === 0) {
                    <div class="empty-state" style="padding:32px;">
                      <span class="material-icons-round" style="font-size:40px;color:var(--text-muted);opacity:.4;">people_outline</span>
                      <p style="margin-top:8px;">No registrations for this event yet.</p>
                    </div>
                  } @else {
                    <!-- Attendee sub-stats -->
                    <div style="padding:14px 20px;background:var(--surface-2);display:flex;gap:24px;flex-wrap:wrap;border-bottom:1px solid var(--border);">
                      <span style="font-size:.82rem;color:var(--text-secondary);">
                        <strong>{{ activeCount(item) }}</strong> active &nbsp;·&nbsp;
                        <strong>{{ item.regs.length - activeCount(item) }}</strong> cancelled &nbsp;·&nbsp;
                        <strong>₹{{ eventRevenue(item) | number:'1.0-0' }}</strong> revenue
                      </span>
                    </div>

                    <div class="table-wrapper" style="border:none;border-radius:0;">
                      <table>
                        <thead>
                          <tr>
                            <th>#</th>
                            <th>Attendee</th>
                            <th>Email</th>
                            <th>Registered At</th>
                            <th>Status</th>
                            <th>Payment</th>
                          </tr>
                        </thead>
                        <tbody>
                          @for (r of item.regs; track r.registrationId) {
                            <tr>
                              <td style="color:var(--text-muted);font-size:.8rem;">#{{ r.registrationId }}</td>
                              <td>
                                <div style="font-weight:600;font-size:.88rem;">
                                  {{ r.userName || 'User #' + r.userId }}
                                </div>
                              </td>
                              <td style="color:var(--text-muted);font-size:.82rem;">{{ r.userEmail || '—' }}</td>
                              <td style="color:var(--text-muted);font-size:.82rem;white-space:nowrap;">
                                {{ r.registeredAt | date:'MMM d, y, h:mm a' }}
                              </td>
                              <td>
                                <span class="badge" [class]="r.status === RegistrationStatus.REGISTERED ? 'badge-success' : 'badge-danger'">
                                  {{ r.status === RegistrationStatus.REGISTERED ? 'Active' : 'Cancelled' }}
                                </span>
                              </td>
                              <td>
                                @if (getPayment(item, r.userId); as pay) {
                                  <span class="badge" [class]="payBadge(pay.status)">
                                    ₹{{ pay.amountPaid | number:'1.0-0' }} — {{ payLabel(pay.status) }}
                                  </span>
                                } @else if (item.event.isPaidEvent) {
                                  <span class="badge badge-warning">Not Paid</span>
                                } @else {
                                  <span class="badge badge-success">Free</span>
                                }
                              </td>
                            </tr>
                          }
                        </tbody>
                      </table>
                    </div>
                  }
                </div>
              }

            </div>
          }
        </div>
      }
    </div>
  `
})
export class EventRegistrationsComponent implements OnInit {
  private eventSvc = inject(EventService);
  private regSvc   = inject(RegistrationService);
  private paySvc   = inject(PaymentService);
  RegistrationStatus = RegistrationStatus;

  loadingEvents = signal(true);
  eventList     = signal<EventWithStats[]>([]);

  totalRegistrations = computed(() =>
    this.eventList().reduce((sum, i) => sum + i.regs.filter(r => r.status === RegistrationStatus.REGISTERED).length, 0)
  );
  totalRevenue = computed(() =>
    this.eventList().reduce((sum, i) => sum + i.payments.filter(p => p.status === PaymentStatus.SUCCESS).reduce((s, p) => s + p.amountPaid, 0), 0)
  );

  ngOnInit() {
    this.eventSvc.getMyEvents().subscribe({
      next: evs => {
        this.eventList.set(evs.map(ev => ({ event: ev, regs: [], payments: [], loading: false, expanded: false })));
        this.loadingEvents.set(false);
      },
      error: () => this.loadingEvents.set(false)
    });
  }

  toggle(item: EventWithStats) {
    if (!item.expanded && item.regs.length === 0) {
      // First open — fetch data
      item.loading = true;
      item.expanded = true;
      this.eventList.update(list => [...list]); // trigger change detection

      this.regSvc.getByEvent(item.event.eventId).subscribe({
        next: regs => {
          item.regs = regs;
          item.loading = false;
          this.eventList.update(list => [...list]);
        },
        error: () => { item.loading = false; this.eventList.update(list => [...list]); }
      });

      if (item.event.isPaidEvent) {
        this.paySvc.getByEvent(item.event.eventId).subscribe({
          next: pays => { item.payments = pays; this.eventList.update(list => [...list]); },
          error: () => {}
        });
      }
    } else {
      item.expanded = !item.expanded;
      this.eventList.update(list => [...list]);
    }
  }

  activeCount(item: EventWithStats) {
    return item.regs.filter(r => r.status === RegistrationStatus.REGISTERED).length;
  }

  eventRevenue(item: EventWithStats) {
    return item.payments.filter(p => p.status === PaymentStatus.SUCCESS).reduce((s, p) => s + p.amountPaid, 0);
  }

  getPayment(item: EventWithStats, userId: number): PaymentResponse | undefined {
    return item.payments.find(p => p.status === PaymentStatus.SUCCESS &&
      item.regs.find(r => r.userId === userId)?.eventId === p.eventId);
  }

  approvalLabel(s: ApprovalStatus) { return { 1: 'Pending', 2: 'Approved', 3: 'Rejected' }[s] ?? ''; }
  approvalBadge(s: ApprovalStatus) { return { 1: 'badge-warning', 2: 'badge-success', 3: 'badge-danger' }[s] ?? 'badge-gray'; }
  payLabel(s: PaymentStatus) { return { 1: 'Pending', 2: 'Paid', 3: 'Failed', 4: 'Refunded' }[s] ?? s; }
  payBadge(s: PaymentStatus) { return { 1: 'badge-warning', 2: 'badge-success', 3: 'badge-danger', 4: 'badge-info' }[s] ?? 'badge-gray'; }
}
