import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { SidebarComponent, NavItem } from '../../../shared/components/sidebar/sidebar.component';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterOutlet, CommonModule, SidebarComponent],
  template: `
    <div class="dash-layout">
      @if (sidebarOpen()) { <div style="position:fixed;inset:0;background:rgba(0,0,0,.5);z-index:299;" (click)="sidebarOpen.set(false)"></div> }
      <app-sidebar [items]="nav" roleLabel="Administrator" [open]="sidebarOpen()" (navClick)="sidebarOpen.set(false)" />
      <div class="dash-main">
        <header class="dash-header">
          <button type="button" class="btn btn-ghost btn-icon" style="display:none;" id="hamburger" (click)="sidebarOpen.set(true)">
            <span class="material-icons-round">menu</span>
          </button>
          <div class="page-title">Admin Panel</div>
          <div style="display:flex;align-items:center;gap:10px;">
            <span style="font-size:.85rem;color:var(--text-muted);">{{ auth.userEmail() }}</span>
            <span class="badge badge-danger">ADMIN</span>
          </div>
        </header>
        <div class="dash-content"><router-outlet /></div>
      </div>
    </div>
  `,
  styles: [`@media(max-width:768px){ #hamburger{ display:flex!important; } }`]
})
export class AdminLayoutComponent {
  auth        = inject(AuthService);
  sidebarOpen = signal(false);
  nav: NavItem[] = [
    { label: 'Dashboard',     icon: 'dashboard',          route: '/admin/dashboard' },
    { label: 'Events',        icon: 'event',              route: '/admin/events' },
    { label: 'Users',         icon: 'group',              route: '/admin/users' },
    { label: 'Calendar',      icon: 'calendar_month',     route: '/admin/calendar' },
    { label: 'Payments',      icon: 'account_balance',    route: '/admin/payments' },
    { label: 'Role Requests', icon: 'upgrade',            route: '/admin/role-requests' },
    { label: 'Profile',       icon: 'manage_accounts',    route: '/admin/profile' },
  ];
}
