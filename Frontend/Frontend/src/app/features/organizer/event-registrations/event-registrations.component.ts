import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EventService } from '../../../core/services/event.service';
import { RegistrationService } from '../../../core/services/registration.service';
import { PaymentService } from '../../../core/services/payment.service';
import { EventResponse, EventRegistrationResponse, PaymentResponse, RegistrationStatus, PaymentStatus } from '../../../core/models/models';

@Component({
  selector: 'app-event-registrations',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div>
      <div style="margin-bottom:24px;"><h1 style="font-size:1.5rem;">Event Registrations</h1><p>View attendees for each of your events</p></div>

      <div class="form-group" style="max-width:360px;margin-bottom:24px;">
        <label class="form-label">Select Event</label>
        <select [(ngModel)]="selectedEventId" (ngModelChange)="onEventChange($event)" class="form-control">
          <option value="">— Select an event —</option>
          @for (ev of myEvents(); track ev.eventId) {
            <option [value]="ev.eventId">{{ ev.title }} ({{ ev.eventDate | date:'MMM d, y' }})</option>
          }
        </select>
      </div>

      @if (selectedEventId) {
        <!-- Summary row -->
        <div class="stats-grid" style="max-width:600px;margin-bottom:20px;">
          <div class="stat-card"><div class="stat-icon" style="background:var(--primary-light);"><span class="material-icons-round" style="color:var(--primary);">people</span></div><div class="stat-value">{{ regs().length }}</div><div class="stat-label">Total Registrations</div></div>
          <div class="stat-card"><div class="stat-icon" style="background:var(--success-light);"><span class="material-icons-round" style="color:var(--success);">how_to_reg</span></div><div class="stat-value">{{ activeRegs() }}</div><div class="stat-label">Active</div></div>
          <div class="stat-card"><div class="stat-icon" style="background:var(--warning-light);"><span class="material-icons-round" style="color:var(--warning);">payments</span></div><div class="stat-value">₹{{ totalRevenue() | number:'1.0-0' }}</div><div class="stat-label">Revenue</div></div>
        </div>

        @if (loadingRegs()) {
          <div class="loading-center"><div class="spinner"></div></div>
        } @else if (regs().length === 0) {
          <div class="empty-state"><span class="material-icons-round empty-icon">people_outline</span><h3>No registrations yet</h3><p>Registrations will appear here once attendees sign up.</p></div>
        } @else {
          <div class="table-wrapper">
            <table>
              <thead><tr><th>#</th><th>User ID</th><th>Registered At</th><th>Status</th><th>Payment</th></tr></thead>
              <tbody>
                @for (r of regs(); track r.registrationId) {
                  <tr>
                    <td style="color:var(--text-muted);">#{{ r.registrationId }}</td>
                    <td>User #{{ r.userId }}</td>
                    <td style="color:var(--text-muted);">{{ r.registeredAt | date:'MMM d, y, h:mm a' }}</td>
                    <td><span class="badge" [class]="r.status === RegistrationStatus.REGISTERED ? 'badge-success' : 'badge-danger'">{{ r.status === RegistrationStatus.REGISTERED ? 'Active' : 'Cancelled' }}</span></td>
                    <td>
                      @if (getPayment(r.userId)) {
                        <span class="badge" [class]="payBadge(getPayment(r.userId)!.status)">
                          ₹{{ getPayment(r.userId)!.amountPaid | number:'1.0-0' }} — {{ payLabel(getPayment(r.userId)!.status) }}
                        </span>
                      } @else if (selectedEvent()?.isPaidEvent) {
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
      }
    </div>
  `
})
export class EventRegistrationsComponent implements OnInit {
  private eventSvc = inject(EventService);
  private regSvc   = inject(RegistrationService);
  private paySvc   = inject(PaymentService);
  RegistrationStatus = RegistrationStatus;

  myEvents      = signal<EventResponse[]>([]);
  regs          = signal<EventRegistrationResponse[]>([]);
  payments      = signal<PaymentResponse[]>([]);
  loadingRegs   = signal(false);
  selectedEventId: number | '' = '';

  selectedEvent = () => this.myEvents().find(e => e.eventId === +this.selectedEventId) ?? null;
  activeRegs    = () => this.regs().filter(r => r.status === RegistrationStatus.REGISTERED).length;
  totalRevenue  = () => this.payments().filter(p => p.status === PaymentStatus.SUCCESS).reduce((s, p) => s + p.amountPaid, 0);
  getPayment    = (uid: number) => this.payments().find(p => p.status === PaymentStatus.SUCCESS && this.regs().find(r => r.userId === uid)?.eventId === p.eventId);

  ngOnInit() {
    this.eventSvc.getMyEvents().subscribe({ next: evs => this.myEvents.set(evs), error: () => {} });
  }

  onEventChange(id: number | '') {
    if (!id) return;
    this.loadingRegs.set(true);
    this.regs.set([]);
    this.payments.set([]);
    this.regSvc.getByEvent(+id).subscribe({ next: rs => { this.regs.set(rs); this.loadingRegs.set(false); }, error: () => this.loadingRegs.set(false) });
    this.paySvc.getByEvent(+id).subscribe({ next: ps => this.payments.set(ps), error: () => {} });
  }

  payLabel(s: PaymentStatus) { return { 1: 'Pending', 2: 'Paid', 3: 'Failed', 4: 'Refunded' }[s] ?? s; }
  payBadge(s: PaymentStatus) { return { 1: 'badge-warning', 2: 'badge-success', 3: 'badge-danger', 4: 'badge-info' }[s] ?? 'badge-gray'; }
}
