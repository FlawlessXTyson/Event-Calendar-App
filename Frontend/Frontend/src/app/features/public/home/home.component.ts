import { Component, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { EventService } from '../../../core/services/event.service';
import { AuthService } from '../../../core/services/auth.service';
import { EventResponse, ApprovalStatus } from '../../../core/models/models';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <!-- Hero -->
    <section class="hero">
      <div class="hero-content">
        <div class="hero-badges">
          <div class="hero-badge"><span class="material-icons-round" style="font-size:16px;">verified</span> Trusted Platform</div>
          <div class="hero-badge"><span class="material-icons-round" style="font-size:16px;">bolt</span> Instant Registration</div>
        </div>
        <h1>Discover &amp; Manage<br>Events Effortlessly</h1>
        <p>Every great moment starts with a single registration. Find your next experience, connect with your community, and make memories that last.</p>
        <div style="display:flex;gap:12px;flex-wrap:wrap;">
          <a routerLink="/events" class="btn btn-lg" style="background:#fff;color:var(--primary);box-shadow:0 4px 16px rgba(0,0,0,.15);">
            <span class="material-icons-round">explore</span> Browse Events
          </a>
          @if (!auth.isLoggedIn()) {
            <a routerLink="/auth/register" class="btn btn-lg"
              style="background:rgba(255,255,255,.15);color:#fff;border:1.5px solid rgba(255,255,255,.5);transition:background .2s,color .2s;"
              onmouseover="this.style.background='#fff';this.style.color='var(--primary-dark)'"
              onmouseout="this.style.background='rgba(255,255,255,.15)';this.style.color='#fff'">
              <span class="material-icons-round">person_add</span> Get Started Free
            </a>
          } @else {
            <button type="button" class="btn btn-lg" style="background:rgba(255,255,255,.15);color:#fff;border:1.5px solid rgba(255,255,255,.3);" (click)="auth.redirectByRole()">
              <span class="material-icons-round">dashboard</span> My Dashboard
            </button>
          }
        </div>
      </div>
    </section>

    <!-- Stats -->
    <section style="padding:32px max(5%,24px);background:var(--surface);border-bottom:1px solid var(--border);">
      <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(150px,1fr));gap:16px;max-width:800px;margin:0 auto;text-align:center;">
        @for (s of stats; track s.label) {
          <div>
            <div style="font-family:var(--font-display);font-size:2rem;font-weight:800;color:var(--primary);">{{ s.value }}</div>
            <div style="font-size:.875rem;color:var(--text-muted);margin-top:4px;">{{ s.label }}</div>
          </div>
        }
      </div>
    </section>

    <!-- Features -->
    <section style="padding:64px max(5%,24px);">
      <div style="text-align:center;margin-bottom:48px;">
        <h2 style="font-size:2rem;margin-bottom:12px;">Everything You Need</h2>
        <p style="max-width:480px;margin:0 auto;">From discovery to registration to payment — a seamless event experience.</p>
      </div>
      <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(240px,1fr));gap:20px;max-width:1000px;margin:0 auto;">
        @for (f of featureCards; track f.title) {
          <div class="card card-body" style="text-align:center;">
            <div style="width:56px;height:56px;background:var(--primary-light);border-radius:14px;display:flex;align-items:center;justify-content:center;margin:0 auto 16px;">
              <span class="material-icons-round" style="color:var(--primary);font-size:26px;">{{ f.icon }}</span>
            </div>
            <h3 style="font-size:1rem;margin-bottom:8px;">{{ f.title }}</h3>
            <p style="font-size:.875rem;">{{ f.desc }}</p>
          </div>
        }
      </div>
    </section>

    <!-- Upcoming Events -->
    <section style="padding:0 max(5%,24px) 64px;">
      <div class="section-header" style="margin-bottom:24px;">
        <div>
          <h2 style="font-size:1.5rem;">Upcoming Events</h2>
          <p style="font-size:.875rem;margin-top:4px;">Approved events open for registration</p>
        </div>
        <a routerLink="/events" class="btn btn-outline">View All <span class="material-icons-round" style="font-size:17px;">arrow_forward</span></a>
      </div>

      @if (loading()) {
        <div class="events-grid">
          @for (i of [1,2,3,4,5,6]; track i) {
            <div class="card"><div class="card-body"><div class="skeleton" style="height:16px;margin-bottom:12px;"></div><div class="skeleton" style="height:12px;width:70%;"></div></div></div>
          }
        </div>
      } @else if (events().length === 0) {
        <div class="empty-state">
          <span class="material-icons-round empty-icon">event_busy</span>
          <h3>No upcoming events</h3>
          <p>Check back soon — new events are added regularly.</p>
        </div>
      } @else {
        <div class="events-grid">
          @for (ev of events().slice(0,6); track ev.eventId) {
            <div class="card event-card card-hover" (click)="router.navigate(['/events', ev.eventId])">
              <div class="event-card-header">
                <div class="event-card-meta">
                  <span class="badge" [ngClass]="approvalBadge(ev)">{{ approvalLabel(ev) }}</span>
                  @if (ev.isPaidEvent) {
                    <span class="badge badge-purple">₹{{ ev.ticketPrice }}</span>
                  } @else {
                    <span class="badge badge-success">Free</span>
                  }
                </div>
                <div class="event-card-title">{{ ev.title }}</div>
              </div>
                <div class="event-card-body">
                <p>{{ ev.description || 'Join this exciting event. Click to learn more.' }}</p>
                <div style="display:flex;flex-direction:column;gap:4px;margin-top:10px;">
                  <div class="event-detail-row">
                    <span class="material-icons-round">calendar_today</span>
                    {{ ev.eventDate | date:'EEE, MMM d, y' }}
                  </div>
                  @if (ev.startTime) {
                    <div class="event-detail-row">
                      <span class="material-icons-round">schedule</span>
                      {{ fmt(ev.startTime) }}{{ ev.endTime ? ' – ' + fmt(ev.endTime) : '' }}
                    </div>
                  }
                  @if (ev.location) {
                    <div class="event-detail-row">
                      <span class="material-icons-round">location_on</span>
                      {{ ev.location }}
                    </div>
                  }
                  @if (isClosed(ev)) {
                    <div class="event-detail-row" style="margin-top:2px;">
                      <span class="material-icons-round" style="color:#991B1B;">lock</span>
                      <span class="badge badge-danger" style="font-size:.72rem;">Registration Closed</span>
                    </div>
                  } @else if (ev.seatsLeft !== undefined && ev.seatsLeft >= 0) {
                    <div class="event-detail-row" style="margin-top:2px;">
                      <span class="material-icons-round">event_seat</span>
                      <span [class]="seatBadge(ev.seatsLeft)" style="font-size:.72rem;">{{ seatLabel(ev.seatsLeft) }}</span>
                    </div>
                  }
                </div>
              </div>
              <div class="event-card-footer">
                <span class="text-muted text-sm">Click to view details</span>
                @if (!isClosed(ev)) {
                  <button type="button" class="btn btn-primary btn-sm">Register</button>
                } @else {
                  <span class="badge badge-danger" style="font-size:.75rem;">Closed</span>
                }
              </div>
            </div>
          }
        </div>
      }
    </section>

    <!-- CTA -->
    @if (!auth.isLoggedIn()) {
      <section style="background:linear-gradient(135deg,var(--primary-dark),var(--primary));padding:64px max(5%,24px);text-align:center;color:#fff;">
        <h2 style="font-size:2rem;margin-bottom:12px;">Ready to Get Started?</h2>
        <p style="opacity:.85;max-width:420px;margin:0 auto 28px;">Join thousands of users discovering and attending events every day.</p>
        <a routerLink="/auth/register" class="btn btn-lg" style="background:#fff;color:var(--primary);font-weight:700;">
          <span class="material-icons-round">rocket_launch</span> Create Free Account
        </a>
      </section>
    }
  `
})
export class HomeComponent implements OnInit {
  readonly auth   = inject(AuthService);
  readonly router = inject(Router);
  private eventSvc = inject(EventService);

  events  = signal<EventResponse[]>([]);
  loading = signal(true);

  stats = [
    { value: '1,200+', label: 'Events Listed' },
    { value: '15,000+', label: 'Registrations' },
    { value: '500+', label: 'Organisers' },
    { value: '98%', label: 'Satisfaction' },
  ];

  featureCards = [
    { icon: 'event_available', title: 'Easy Registration',  desc: 'Register for events with a single click. Your seat is confirmed instantly.' },
    { icon: 'payment',         title: 'Secure Payments',    desc: 'Pay safely for paid events. Automatic refunds when events are cancelled.' },
    { icon: 'notifications',   title: 'Smart Reminders',    desc: 'Never miss a moment — set custom reminders and get notified before events start.' },
    { icon: 'checklist',       title: 'Personal To-Do',     desc: 'Stay organised with a built-in task manager for all your event prep.' },
  ];

  ngOnInit(): void {
    this.eventSvc.getAll().subscribe({
      next: evs => {
        const today = new Date(); today.setHours(0, 0, 0, 0);
        const filtered = evs
          .filter(e => new Date(e.eventDate) >= today && e.isRegistrationOpen !== false)
          .sort((a, b) => new Date(a.eventDate).getTime() - new Date(b.eventDate).getTime());
        this.events.set(filtered);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  approvalLabel(ev: EventResponse) {
    return ev.approvalStatus === ApprovalStatus.APPROVED ? 'Approved' : 'Pending';
  }
  approvalBadge(ev: EventResponse) {
    return ev.approvalStatus === ApprovalStatus.APPROVED ? 'badge-success' : 'badge-warning';
  }

  isClosed(ev: EventResponse): boolean {
    const now = new Date();
    if (ev.registrationDeadline) {
      const dl = ev.registrationDeadline;
      const deadlineDate = new Date(dl.endsWith('Z') || dl.includes('+') ? dl : dl + 'Z');
      if (deadlineDate <= now) return true;
    }
    if (ev.hasStarted === true || ev.hasEnded === true) return true;
    if (!ev.registrationDeadline && ev.isRegistrationOpen === false) return true;
    return false;
  }

  fmt(t: string): string {
    const [h, m] = t.split(':').map(Number);
    const ampm = h >= 12 ? 'PM' : 'AM';
    return `${h % 12 || 12}:${String(m).padStart(2, '0')} ${ampm}`;
  }

  seatLabel(left: number): string {
    if (left === 0) return '🚫 Event Full';
    if (left <= 5)  return `🔥 Only ${left} seats left!`;
    return `${left} seats left`;
  }
  seatBadge(left: number): string {
    if (left === 0) return 'badge badge-danger';
    if (left <= 5)  return 'badge badge-warning';
    return 'badge badge-success';
  }
}
