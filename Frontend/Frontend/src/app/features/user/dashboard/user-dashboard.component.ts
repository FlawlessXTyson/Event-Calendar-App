import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { EventService } from '../../../core/services/event.service';
import { PaymentService } from '../../../core/services/payment.service';
import { TodoService } from '../../../core/services/todo.service';
import { EventResponse, PaymentResponse, TodoResponse, PaymentStatus, TodoStatus } from '../../../core/models/models';

@Component({
  selector: 'app-user-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div>
      <div style="margin-bottom:28px;">
        <h1 style="font-size:1.5rem;">Welcome back, {{ auth.userName() }}! 👋</h1>
        <p>Here's an overview of your events, payments, and tasks.</p>
      </div>

      <!-- Stats -->
      <div class="stats-grid">
        <div class="stat-card">
          <div class="stat-icon" style="background:var(--primary-light);"><span class="material-icons-round" style="color:var(--primary);">event</span></div>
          <div class="stat-value">{{ registeredEvents().length }}</div>
          <div class="stat-label">Registered Events</div>
        </div>
        <div class="stat-card">
          <div class="stat-icon" style="background:var(--success-light);"><span class="material-icons-round" style="color:var(--success);">payments</span></div>
          <div class="stat-value">₹{{ totalSpent() | number:'1.0-0' }}</div>
          <div class="stat-label">Total Spent</div>
        </div>
        <div class="stat-card">
          <div class="stat-icon" style="background:var(--warning-light);"><span class="material-icons-round" style="color:var(--warning);">checklist</span></div>
          <div class="stat-value">{{ pendingTodos() }}</div>
          <div class="stat-label">Pending Tasks</div>
        </div>
        <div class="stat-card">
          <div class="stat-icon" style="background:var(--info-light);"><span class="material-icons-round" style="color:var(--info);">history</span></div>
          <div class="stat-value">{{ payments().length }}</div>
          <div class="stat-label">Total Payments</div>
        </div>
      </div>

      <div style="display:grid;grid-template-columns:1fr 1fr;gap:20px;margin-top:8px;" class="dash-grid-2">
        <!-- Upcoming registered events -->
        <div class="card">
          <div class="card-header" style="display:flex;align-items:center;justify-content:space-between;">
            <h3>My Upcoming Events</h3>
            <a routerLink="/user/my-events" class="btn btn-ghost btn-sm">View All →</a>
          </div>
          <div class="card-body" style="padding:0;">
            @if (registeredEvents().length === 0) {
              <div class="empty-state" style="padding:32px;"><span class="material-icons-round empty-icon" style="font-size:40px;">event_busy</span><p style="font-size:.875rem;">No registered events yet.</p></div>
            } @else {
              @for (ev of registeredEvents().slice(0,4); track ev.eventId) {
                <div style="padding:12px 16px;border-bottom:1px solid var(--border);display:flex;align-items:center;justify-content:space-between;gap:12px;">
                  <div>
                    <div style="font-weight:600;font-size:.9rem;">{{ ev.title }}</div>
                    <div style="font-size:.8rem;color:var(--text-muted);">{{ ev.eventDate | date:'MMM d, y' }}</div>
                  </div>
                  @if (ev.isPaidEvent) { <span class="badge badge-warning">Paid</span> }
                  @else { <span class="badge badge-success">Free</span> }
                </div>
              }
            }
          </div>
        </div>

        <!-- Recent payments -->
        <div class="card">
          <div class="card-header" style="display:flex;align-items:center;justify-content:space-between;">
            <h3>Recent Payments</h3>
            <a routerLink="/user/payments" class="btn btn-ghost btn-sm">View All →</a>
          </div>
          <div class="card-body" style="padding:0;">
            @if (payments().length === 0) {
              <div class="empty-state" style="padding:32px;"><span class="material-icons-round empty-icon" style="font-size:40px;">receipt_long</span><p style="font-size:.875rem;">No payments yet.</p></div>
            } @else {
              @for (p of payments().slice(0,4); track p.paymentId) {
                <div style="padding:12px 16px;border-bottom:1px solid var(--border);display:flex;align-items:center;justify-content:space-between;">
                  <div>
                    <div style="font-weight:600;font-size:.9rem;">{{ p.eventTitle || 'Event #' + p.eventId }}</div>
                    <div style="font-size:.8rem;color:var(--text-muted);">{{ p.paymentDate | date:'MMM d, y' }}</div>
                  </div>
                  <div style="text-align:right;">
                    <div style="font-weight:700;">₹{{ p.amountPaid | number:'1.0-0' }}</div>
                    <span class="badge" [class]="statusBadge(p.status)">{{ statusLabel(p.status) }}</span>
                  </div>
                </div>
              }
            }
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`@media(max-width:768px){ .dash-grid-2{ grid-template-columns:1fr!important; } }`]
})
export class UserDashboardComponent implements OnInit {
  auth = inject(AuthService);
  private eventSvc = inject(EventService);
  private paySvc   = inject(PaymentService);
  private todoSvc  = inject(TodoService);

  registeredEvents = signal<EventResponse[]>([]);
  payments         = signal<PaymentResponse[]>([]);
  todos            = signal<TodoResponse[]>([]);

  totalSpent   = () => this.payments().filter(p => p.status === PaymentStatus.SUCCESS).reduce((s,p) => s+p.amountPaid, 0);
  pendingTodos = () => this.todos().filter(t => t.status === TodoStatus.PENDING).length;

  ngOnInit() {
    this.eventSvc.getRegistered().subscribe({
      next: evs => {
        // Only keep events that haven't ended yet, sorted by date
        const now = new Date();
        const upcoming = evs
          .filter(e => !e.hasEnded && new Date(e.eventDate) >= new Date(now.toDateString()))
          .sort((a, b) => new Date(a.eventDate).getTime() - new Date(b.eventDate).getTime());
        this.registeredEvents.set(upcoming);
      },
      error: () => {}
    });
    this.paySvc.getMyPayments().subscribe({ next: ps => this.payments.set(ps), error: () => {} });
    this.todoSvc.getMyTodos().subscribe({ next: ts => this.todos.set(ts), error: () => {} });
  }

  statusLabel(s: PaymentStatus) { return {1:'Pending',2:'Success',3:'Failed',4:'Refunded'}[s] ?? s; }
  statusBadge(s: PaymentStatus) { return {1:'badge-warning',2:'badge-success',3:'badge-danger',4:'badge-info'}[s] ?? 'badge-gray'; }
}
