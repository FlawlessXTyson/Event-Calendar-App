import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { SidebarComponent, NavItem } from '../../../shared/components/sidebar/sidebar.component';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-organizer-layout',
  standalone: true,
  imports: [RouterOutlet, CommonModule, SidebarComponent],
  template: `
    <div class="dash-layout">
      @if (sidebarOpen()) { <div style="position:fixed;inset:0;background:rgba(0,0,0,.5);z-index:299;" (click)="sidebarOpen.set(false)"></div> }
      <app-sidebar [items]="nav" roleLabel="Organizer" [open]="sidebarOpen()" (navClick)="sidebarOpen.set(false)" />
      <div class="dash-main">
        <header class="dash-header">
          <button type="button" class="btn btn-ghost btn-icon" style="display:none;" id="hamburger" (click)="sidebarOpen.set(true)">
            <span class="material-icons-round">menu</span>
          </button>
          <div class="page-title">Organizer Dashboard</div>
          <div style="display:flex;align-items:center;gap:10px;">
            <span style="font-size:.85rem;color:var(--text-muted);">{{ auth.userEmail() }}</span>
            <span class="badge badge-warning">ORGANIZER</span>
          </div>
        </header>
        <div class="dash-content"><router-outlet /></div>
      </div>
    </div>
  `,
  styles: [`@media(max-width:768px){ #hamburger{ display:flex!important; } }`]
})
export class OrganizerLayoutComponent {
  auth        = inject(AuthService);
  sidebarOpen = signal(false);
  nav: NavItem[] = [
    { label:'Dashboard',         icon:'dashboard',             route:'/organizer/dashboard' },
    { label:'Create Event',      icon:'add_circle',            route:'/organizer/create-event' },
    { label:'My Events',         icon:'event_note',            route:'/organizer/my-events' },
    { label:'Calendar',          icon:'calendar_month',        route:'/organizer/calendar' },
    { label:'Registrations',     icon:'people',                route:'/organizer/registrations' },
    { label:'Earnings',          icon:'account_balance_wallet', route:'/organizer/earnings' },
    { label:'Profile',           icon:'manage_accounts',       route:'/organizer/profile' },
  ];
}
