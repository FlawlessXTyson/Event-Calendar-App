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
  template: `
    <div style="padding:40px max(5%,24px);">
      <div style="margin-bottom:28px;">
        <h1>All Events</h1>
        <p style="margin-top:6px;">Browse all upcoming approved events. Use search to filter.</p>
      </div>

      <!-- Search + Filter -->
      <div style="display:flex;gap:12px;margin-bottom:28px;flex-wrap:wrap;">
        <div style="flex:1;min-width:200px;position:relative;">
          <span class="material-icons-round" style="position:absolute;left:12px;top:50%;transform:translateY(-50%);color:var(--text-muted);font-size:20px;">search</span>
          <input [(ngModel)]="keyword" type="text" class="form-control"
            style="padding-left:42px;" placeholder="Search by event name…"
            (keyup.enter)="search()" />
        </div>
        <button type="button" class="btn btn-primary" (click)="search()">
          <span class="material-icons-round">search</span> Search
        </button>
        @if (keyword) {
          <button type="button" class="btn btn-ghost" (click)="clearSearch()">
            <span class="material-icons-round">close</span> Clear
          </button>
        }
      </div>

      @if (loading()) {
        <div class="events-grid">
          @for (i of [1,2,3,4,5,6]; track i) {
            <div class="card card-body" style="height:220px;display:flex;flex-direction:column;gap:12px;">
              <div class="skeleton" style="height:20px;width:60%;"></div>
              <div class="skeleton" style="height:14px;width:40%;"></div>
              <div class="skeleton" style="height:56px;"></div>
            </div>
          }
        </div>
      } @else if (events().length === 0) {
        <div class="empty-state">
          <span class="material-icons-round empty-icon">search_off</span>
          <h3>No events found</h3>
          <p>{{ keyword ? 'Try a different keyword.' : 'No upcoming approved events at the moment.' }}</p>
        </div>
      } @else {
        <div style="font-size:.875rem;color:var(--text-muted);margin-bottom:16px;">
          Showing {{ events().length }} event{{ events().length !== 1 ? 's' : '' }}
        </div>
        <div class="events-grid">
          @for (ev of events(); track ev.eventId) {
            <a [routerLink]="['/events', ev.eventId]" class="event-card card-hover" style="text-decoration:none;color:inherit;">
              <div class="event-card-header">
                <div class="event-card-meta">
                  <span class="badge" [ngClass]="categoryBadge(ev.category)">{{ categoryLabel(ev.category) }}</span>
                  @if (ev.isPaidEvent) { <span class="badge badge-purple">₹{{ ev.ticketPrice | number }}</span> }
                  @else { <span class="badge badge-success">Free</span> }
                </div>
                <div class="event-card-title">{{ ev.title }}</div>
              </div>
              <div class="event-card-body">
                <p>{{ ev.description || 'Click to view details and register.' }}</p>
              </div>
              <div class="event-card-footer">
                <div>
                  <div class="event-detail-row">
                    <span class="material-icons-round">calendar_today</span>
                    {{ ev.eventDate | date:'EEE, MMM d, y' }}
                  </div>
                  @if (ev.location) {
                    <div class="event-detail-row" style="margin-top:3px;">
                      <span class="material-icons-round">location_on</span>
                      {{ ev.location }}
                    </div>
                  }
                </div>
                <span class="btn btn-primary btn-sm">View Details</span>
              </div>
            </a>
          }
        </div>
      }
    </div>
  `
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
        const today = new Date(); today.setHours(0,0,0,0);
        this.events.set(evs.filter(e => new Date(e.eventDate) >= today));
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  search() {
    if (!this.keyword.trim()) { this.load(); return; }
    this.loading.set(true);
    this.svc.search(this.keyword.trim()).subscribe({
      next: evs => { this.events.set(evs); this.loading.set(false); },
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
}
