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
  templateUrl: './home.component.html',
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
