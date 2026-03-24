import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaymentService } from '../../../core/services/payment.service';
import { OrganizerEarnings, EventWiseEarnings } from '../../../core/models/models';

@Component({
  selector: 'app-organizer-earnings',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div>
      <div style="margin-bottom:24px;"><h1 style="font-size:1.5rem;">Earnings</h1><p>Your revenue breakdown and event-wise performance</p></div>
      @if (loading()) { <div class="loading-center"><div class="spinner"></div></div> }
      @else {
        <!-- Summary Cards -->
        @if (summary()) {
          <div class="stats-grid" style="margin-bottom:28px;">
            <div class="stat-card" style="border-top:3px solid var(--success);"><div class="stat-icon" style="background:var(--success-light);"><span class="material-icons-round" style="color:var(--success);">account_balance_wallet</span></div><div class="stat-value" style="color:var(--success);">₹{{ summary()!.netEarnings | number:'1.0-0' }}</div><div class="stat-label">Net Earnings</div></div>
            <div class="stat-card" style="border-top:3px solid var(--primary);"><div class="stat-icon" style="background:var(--primary-light);"><span class="material-icons-round" style="color:var(--primary);">payments</span></div><div class="stat-value">₹{{ summary()!.totalRevenue | number:'1.0-0' }}</div><div class="stat-label">Gross Revenue</div></div>
            <div class="stat-card" style="border-top:3px solid var(--danger);"><div class="stat-icon" style="background:var(--danger-light);"><span class="material-icons-round" style="color:var(--danger);">percent</span></div><div class="stat-value" style="color:var(--danger);">₹{{ summary()!.totalCommission | number:'1.0-0' }}</div><div class="stat-label">Platform Commission (10%)</div></div>
            <div class="stat-card" style="border-top:3px solid var(--info);"><div class="stat-icon" style="background:var(--info-light);"><span class="material-icons-round" style="color:var(--info);">receipt_long</span></div><div class="stat-value">{{ summary()!.totalTransactions }}</div><div class="stat-label">Total Transactions</div></div>
          </div>
        }

        <!-- Event-wise breakdown -->
        <div class="card">
          <div class="card-header"><h3>Event-wise Earnings</h3></div>
          @if (eventEarnings().length === 0) {
            <div class="empty-state" style="padding:40px;"><span class="material-icons-round empty-icon">bar_chart</span><h3>No earnings yet</h3><p>Earnings appear here once attendees pay for your events.</p></div>
          } @else {
            <div class="table-wrapper" style="border:none;border-radius:0;">
              <table>
                <thead><tr><th>Event</th><th>Transactions</th><th>Revenue</th><th>Commission</th><th>Net Earnings</th></tr></thead>
                <tbody>
                  @for (e of eventEarnings(); track e.eventId) {
                    <tr>
                      <td style="font-weight:600;">{{ e.eventTitle }}</td>
                      <td>{{ e.totalTransactions }}</td>
                      <td>₹{{ e.totalRevenue | number:'1.0-0' }}</td>
                      <td style="color:var(--danger);">-₹{{ e.totalCommission | number:'1.0-0' }}</td>
                      <td style="font-weight:700;color:var(--success);">₹{{ e.netEarnings | number:'1.0-0' }}</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        </div>
      }
    </div>
  `
})
export class OrganizerEarningsComponent implements OnInit {
  private paySvc = inject(PaymentService);
  summary       = signal<OrganizerEarnings|null>(null);
  eventEarnings = signal<EventWiseEarnings[]>([]);
  loading       = signal(true);

  ngOnInit() {
    let done = 0;
    const check = () => { if (++done === 2) this.loading.set(false); };
    this.paySvc.getOrganizerEarnings().subscribe({ next: e => { this.summary.set(e); check(); }, error: () => check() });
    this.paySvc.getEventWiseEarnings().subscribe({ next: e => { this.eventEarnings.set(e); check(); }, error: () => check() });
  }
}
