import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { EventService } from '../../../core/services/event.service';
import { EventResponse, EventCategory } from '../../../core/models/models';

@Component({
  selector: 'app-events',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './events.component.html',
})
export class EventsComponent implements OnInit {
  private svc = inject(EventService);
  events  = signal<EventResponse[]>([]);
  loading = signal(true);
  keyword = '';

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.svc.getAll().subscribe({
      next: evs => {
        const today = new Date(); today.setHours(0, 0, 0, 0);
        this.events.set(
          evs
            .filter(e => new Date(e.eventDate) >= today && e.isRegistrationOpen !== false)
            .sort((a, b) => new Date(a.eventDate).getTime() - new Date(b.eventDate).getTime())
        );
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  search() {
    if (!this.keyword.trim()) { this.load(); return; }
    this.loading.set(true);
    this.svc.search(this.keyword.trim()).subscribe({
      next: evs => {
        const today = new Date(); today.setHours(0, 0, 0, 0);
        this.events.set(
          evs
            .filter(e => new Date(e.eventDate) >= today && e.isRegistrationOpen !== false)
            .sort((a, b) => new Date(a.eventDate).getTime() - new Date(b.eventDate).getTime())
        );
        this.loading.set(false);
      },
      error: () => { this.events.set([]); this.loading.set(false); }
    });
  }

  clearSearch() { this.keyword = ''; this.load(); }

  categoryLabel(c: EventCategory): string {
    const m: Record<number,string> = { 1:'Holiday', 2:'Awareness', 3:'Public', 4:'Personal' };
    return m[c] ?? 'Event';
  }
  categoryBadge(c: EventCategory): string {
    const m: Record<number,string> = { 1:'badge-warning', 2:'badge-info', 3:'badge-primary', 4:'badge-orange' };
    return m[c] ?? 'badge-gray';
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

  isDeadlinePassed(ev: EventResponse): boolean {
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

  toUtc(dt: string): Date {
    return new Date(dt.endsWith('Z') || dt.includes('+') ? dt : dt + 'Z');
  }

  fmt(t: string): string {
    const [h, m] = t.split(':').map(Number);
    const ampm = h >= 12 ? 'PM' : 'AM';
    return `${h % 12 || 12}:${String(m).padStart(2, '0')} ${ampm}`;
  }
}
