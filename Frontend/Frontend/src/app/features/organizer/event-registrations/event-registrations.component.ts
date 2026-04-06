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
  templateUrl: './event-registrations.component.html',
  styleUrl: './event-registrations.component.css'
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
