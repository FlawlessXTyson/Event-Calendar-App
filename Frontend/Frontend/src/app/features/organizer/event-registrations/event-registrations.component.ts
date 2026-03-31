import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EventService } from '../../../core/services/event.service';
import { RegistrationService } from '../../../core/services/registration.service';
import { PaymentService } from '../../../core/services/payment.service';
import {
  EventResponse, EventRegistrationResponse,
  PaymentResponse, RegistrationStatus, PaymentStatus, ApprovalStatus, PagedResult
} from '../../../core/models/models';

interface EvItem {
  event: EventResponse;
  regs: EventRegistrationResponse[];
  payments: PaymentResponse[];
  page: number;
  pageSize: number;
  total: number;       // total registrations (all statuses) for pagination
  activeTotal: number; // active registrations only — for badge
  loading: boolean;
  expanded: boolean;
}

@Component({
  selector: 'app-event-registrations',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div>
      <div style="margin-bottom:24px;">
        <h1 style="font-size:1.5rem;">Event Registrations</h1>
        <p>Click any event to see its attendees</p>
      </div>

      @if (loadingEvents()) {
        <div class="loading-center"><div class="spinner"></div></div>
      } @else if (allItems().length === 0) {
        <div class="empty-state">
          <span class="material-icons-round empty-icon">event_busy</span>
          <h3>No events yet</h3>
          <p>Create an event first to start seeing registrations.</p>
        </div>
      } @else {
        <!-- Stats -->
        <div class="stats-grid" style="margin-bottom:24px;">
          <div class="stat-card">
            <div class="stat-icon" style="background:var(--primary-light);">
              <span class="material-icons-round" style="color:var(--primary);">event</span>
            </div>
            <div class="stat-value">{{ allItems().length }}</div>
            <div class="stat-label">Total Events</div>
          </div>
          <div class="stat-card">
            <div class="stat-icon" style="background:var(--success-light);">
              <span class="material-icons-round" style="color:var(--success);">how_to_reg</span>
            </div>
            <div class="stat-value">{{ totalRegs() }}</div>
            <div class="stat-label">Total Registrations</div>
          </div>
          <div class="stat-card">
            <div class="stat-icon" style="background:var(--warning-light);">
              <span class="material-icons-round" style="color:var(--warning);">payments</span>
            </div>
            <div class="stat-value">&#8377;{{ totalRev() | number:'1.0-0' }}</div>
            <div class="stat-label">Total Revenue</div>
          </div>
          <div class="stat-card">
            <div class="stat-icon" style="background:var(--success-light);">
              <span class="material-icons-round" style="color:var(--success);">account_balance_wallet</span>
            </div>
            <div class="stat-value" style="color:var(--success);">&#8377;{{ totalNet() | number:'1.0-0' }}</div>
            <div class="stat-label">Total Net Earnings</div>
          </div>
        </div>

        <!-- Global date filter -->
        <div style="display:flex;align-items:center;gap:12px;margin-bottom:20px;padding:14px 18px;background:var(--surface);border:1px solid var(--border);border-radius:var(--r);">
          <span class="material-icons-round" style="color:var(--primary);font-size:22px;">calendar_month</span>
          <span style="font-size:.9rem;font-weight:600;color:var(--text-secondary);">Filter by event date:</span>
          <input type="date" class="form-control" style="width:180px;font-size:.875rem;"
            [(ngModel)]="globalDate" (ngModelChange)="onGlobalDate()" />
          @if (globalDate) {
            <button type="button" class="btn btn-ghost btn-sm" (click)="globalDate=''; onGlobalDate()">
              <span class="material-icons-round" style="font-size:16px;">close</span> Clear
            </button>
            <span style="font-size:.82rem;color:var(--primary);font-weight:600;">
              {{ filteredItems().length }} event{{ filteredItems().length !== 1 ? 's' : '' }} on {{ globalDate | date:'MMM d, y' }}
            </span>
          }
        </div>

        <!-- Event list -->
        @if (filteredItems().length === 0) {
          <div class="empty-state">
            <span class="material-icons-round empty-icon">event_busy</span>
            <h3>No events on this date</h3>
            <p>Try a different date or clear the filter.</p>
          </div>
        } @else {
          <div style="display:flex;flex-direction:column;gap:12px;">
            @for (item of filteredItems(); track item.event.eventId) {
              <div class="card" style="overflow:hidden;">

                <!-- Event header -->
                <div style="padding:16px 20px;display:flex;align-items:center;gap:16px;cursor:pointer;"
                  (click)="toggle(item)"
                  onmouseover="this.style.background='var(--surface-2)'"
                  onmouseout="this.style.background=''">
                  <span class="material-icons-round"
                    style="color:var(--primary);font-size:22px;flex-shrink:0;transition:transform .2s;"
                    [style.transform]="item.expanded ? 'rotate(90deg)' : 'rotate(0)'">
                    chevron_right
                  </span>
                  <div style="flex:1;min-width:0;">
                    <div style="font-weight:700;font-size:.95rem;">{{ item.event.title }}</div>
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
                  <div style="display:flex;gap:8px;align-items:center;flex-shrink:0;flex-wrap:wrap;">
                    @if (item.event.isPaidEvent) {
                      <span class="badge badge-purple">&#8377;{{ item.event.ticketPrice | number }}</span>
                    } @else {
                      <span class="badge badge-success">Free</span>
                    }
                    <span class="badge" [class]="approvalBadge(item.event.approvalStatus)">
                      {{ approvalLabel(item.event.approvalStatus) }}
                    </span>
                    @if (item.activeTotal > 0) {
                      <span style="background:var(--primary);color:#fff;font-size:.75rem;font-weight:700;padding:3px 10px;border-radius:var(--r-full);">
                        {{ item.activeTotal }} attendee{{ item.activeTotal !== 1 ? 's' : '' }}
                      </span>
                    } @else if (!item.loading) {
                      <span style="background:var(--surface-2);color:var(--text-muted);font-size:.75rem;font-weight:600;padding:3px 10px;border-radius:var(--r-full);">
                        No registrations
                      </span>
                    }
                  </div>
                </div>

                <!-- Expanded attendees -->
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
                      <!-- Sub-stats bar -->
                      <div style="padding:10px 20px;background:var(--surface-2);border-bottom:1px solid var(--border);font-size:.82rem;color:var(--text-secondary);">
                        <strong>{{ activeCount(item) }}</strong> active &nbsp;·&nbsp;
                        <strong>{{ item.regs.length - activeCount(item) }}</strong> cancelled &nbsp;·&nbsp;
                        <strong>&#8377;{{ eventRev(item) | number:'1.0-0' }}</strong> revenue &nbsp;·&nbsp;
                        <strong style="color:var(--success);">&#8377;{{ eventNet(item) | number:'1.0-0' }}</strong> net earnings
                      </div>

                      <div class="table-wrapper" style="border:none;border-radius:0;">
                        <table>
                          <thead>
                            <tr>
                              <th>#</th><th>Attendee</th><th>Email</th>
                              <th>Registered At</th><th>Status</th><th>Payment</th>
                             
                            </tr>
                          </thead>
                          <tbody>
                            @for (r of item.regs; track r.registrationId) {
                              <tr>
                                <td style="color:var(--text-muted);font-size:.8rem;">#{{ r.registrationId }}</td>
                                <td><div style="font-weight:600;font-size:.88rem;">{{ r.userName || ('User #' + r.userId) }}</div></td>
                                <td style="color:var(--text-muted);font-size:.82rem;">{{ r.userEmail || '—' }}</td>
                                <td style="color:var(--text-muted);font-size:.82rem;white-space:nowrap;">{{ r.registeredAt | date:'MMM d, y, h:mm a' }}</td>
                                <td>
                                  <span class="badge" [class]="r.status === RegistrationStatus.REGISTERED ? 'badge-success' : 'badge-danger'">
                                    {{ r.status === RegistrationStatus.REGISTERED ? 'Active' : 'Cancelled' }}
                                  </span>
                                </td>
                                <td>
                                  @if (getPayment(item, r.userId); as pay) {
                                    <span class="badge" [class]="payBadge(pay.status)">
                                      &#8377;{{ pay.amountPaid | number:'1.0-0' }} — {{ payLabel(pay.status) }}
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

                      <!-- Pagination -->
                      @if (totalPages(item) > 1) {
                        <div style="padding:14px 20px;display:flex;align-items:center;justify-content:space-between;border-top:1px solid var(--border);flex-wrap:wrap;gap:10px;">
                          <div style="font-size:.82rem;color:var(--text-muted);">
                            Page {{ item.page }} of {{ totalPages(item) }} &nbsp;·&nbsp; {{ item.regs.length }} of {{ item.total }}
                          </div>
                          <div style="display:flex;gap:6px;">
                            <button type="button" class="btn btn-ghost btn-sm" [disabled]="item.page <= 1" (click)="goPage(item, 1)">
                              <span class="material-icons-round" style="font-size:16px;">first_page</span>
                            </button>
                            <button type="button" class="btn btn-ghost btn-sm" [disabled]="item.page <= 1" (click)="goPage(item, item.page - 1)">
                              <span class="material-icons-round" style="font-size:16px;">chevron_left</span>
                            </button>
                            @for (p of pageNums(item); track p) {
                              <button type="button" class="btn btn-sm"
                                [class]="p === item.page ? 'btn-primary' : 'btn-ghost'"
                                (click)="goPage(item, p)">{{ p }}</button>
                            }
                            <button type="button" class="btn btn-ghost btn-sm" [disabled]="item.page >= totalPages(item)" (click)="goPage(item, item.page + 1)">
                              <span class="material-icons-round" style="font-size:16px;">chevron_right</span>
                            </button>
                            <button type="button" class="btn btn-ghost btn-sm" [disabled]="item.page >= totalPages(item)" (click)="goPage(item, totalPages(item))">
                              <span class="material-icons-round" style="font-size:16px;">last_page</span>
                            </button>
                          </div>
                        </div>
                      }
                    }
                  </div>
                }

              </div>
            }
          </div>
        }
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
  allItems      = signal<EvItem[]>([]);
  globalDate    = '';

  filteredItems = computed(() => {
    if (!this.globalDate) return this.allItems();
    const d = this.globalDate; // 'YYYY-MM-DD'
    return this.allItems().filter(i => i.event.eventDate.startsWith(d));
  });

  totalRegs = computed(() => this.allItems().reduce((s, i) => s + i.activeTotal, 0));
  totalRev  = computed(() =>
    this.allItems().reduce((s, i) =>
      s + i.payments.filter(p => p.status === PaymentStatus.SUCCESS).reduce((a, p) => a + p.amountPaid, 0), 0)
  );
  totalNet  = computed(() =>
    this.allItems().reduce((s, i) =>
      s + i.payments.filter(p => p.status === PaymentStatus.SUCCESS).reduce((a, p) => a + this.netEarning(p), 0), 0)
  );

  ngOnInit() {
    this.eventSvc.getMyEvents().subscribe({
      next: evs => {
        this.allItems.set(evs.map(ev => ({
          event: ev, regs: [], payments: [],
          page: 1, pageSize: 10, total: 0, activeTotal: 0,
          loading: false, expanded: false
        })));
        this.loadingEvents.set(false);
      },
      error: () => this.loadingEvents.set(false)
    });
  }

  onGlobalDate() {
    // Collapse all expanded panels when date changes
    this.allItems.update(list => list.map(i => ({ ...i, expanded: false, regs: [], page: 1 })));
  }

  toggle(item: EvItem) {
    if (!item.expanded) {
      item.expanded = true;
      this.loadRegs(item);
    } else {
      item.expanded = false;
      this.allItems.update(l => [...l]);
    }
  }

  loadRegs(item: EvItem) {
    item.loading = true;
    this.allItems.update(l => [...l]);
    this.regSvc.getByEventPaged(item.event.eventId, item.page, item.pageSize).subscribe({
      next: (res: PagedResult<EventRegistrationResponse>) => {
        item.regs    = res.data;
        item.total   = res.totalRecords;
        // Count active registrations from loaded page + update activeTotal
        // Load all regs to get accurate active count for badge
        item.loading = false;
        this.allItems.update(l => [...l]);
        // Load all regs to compute active count accurately
        this.regSvc.getByEvent(item.event.eventId).subscribe({
          next: allRegs => {
            item.activeTotal = allRegs.filter(r => r.status === RegistrationStatus.REGISTERED).length;
            this.allItems.update(l => [...l]);
          },
          error: () => {}
        });
      },
      error: () => { item.loading = false; this.allItems.update(l => [...l]); }
    });
    if (item.event.isPaidEvent && item.payments.length === 0) {
      this.paySvc.getByEvent(item.event.eventId).subscribe({
        next: pays => { item.payments = pays; this.allItems.update(l => [...l]); },
        error: () => {}
      });
    }
  }

  goPage(item: EvItem, page: number) {
    item.page = page;
    this.loadRegs(item);
  }

  totalPages(item: EvItem): number {
    return Math.max(1, Math.ceil(item.total / item.pageSize));
  }

  pageNums(item: EvItem): number[] {
    const total = this.totalPages(item);
    const cur   = item.page;
    const start = Math.max(1, cur - 2);
    const end   = Math.min(total, cur + 2);
    const pages: number[] = [];
    for (let i = start; i <= end; i++) pages.push(i);
    return pages;
  }

  activeCount(item: EvItem) {
    return item.regs.filter(r => r.status === RegistrationStatus.REGISTERED).length;
  }

  eventRev(item: EvItem) {
    return item.payments.filter(p => p.status === PaymentStatus.SUCCESS).reduce((s, p) => s + p.amountPaid, 0);
  }

  eventNet(item: EvItem) {
    return item.payments.filter(p => p.status === PaymentStatus.SUCCESS).reduce((s, p) => s + this.netEarning(p), 0);
  }

  getPayment(item: EvItem, userId: number): PaymentResponse | undefined {
    return item.payments.find(p => p.userId === userId && p.status === PaymentStatus.SUCCESS);
  }

  /** Use stored organizerAmount; fall back to 90% if 0 (old payments before commission tracking) */
  netEarning(pay: PaymentResponse): number {
    return (pay.organizerAmount && pay.organizerAmount > 0)
      ? pay.organizerAmount
      : pay.amountPaid * 0.9;
  }

  approvalLabel(s: ApprovalStatus) { return ({ 1: 'Pending', 2: 'Approved', 3: 'Rejected' } as Record<number,string>)[s] ?? ''; }
  approvalBadge(s: ApprovalStatus) { return ({ 1: 'badge-warning', 2: 'badge-success', 3: 'badge-danger' } as Record<number,string>)[s] ?? 'badge-gray'; }
  payLabel(s: PaymentStatus)       { return ({ 1: 'Pending', 2: 'Paid', 3: 'Failed', 4: 'Refunded' } as Record<number,string>)[s] ?? String(s); }
  payBadge(s: PaymentStatus)       { return ({ 1: 'badge-warning', 2: 'badge-success', 3: 'badge-danger', 4: 'badge-info' } as Record<number,string>)[s] ?? 'badge-gray'; }
}
