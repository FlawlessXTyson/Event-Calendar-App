import { Component, inject, signal } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { SidebarComponent, NavItem } from '../../../shared/components/sidebar/sidebar.component';
import { AuthService } from '../../../core/services/auth.service';
import { RegistrationStateService } from '../../../core/services/registration-state.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-user-layout',
  standalone: true,
  imports: [RouterOutlet, CommonModule, SidebarComponent],
  template: `
    <div class="dash-layout">
      @if (sidebarOpen()) {
        <div style="position:fixed;inset:0;background:rgba(0,0,0,.5);z-index:299;" (click)="sidebarOpen.set(false)"></div>
      }
      <app-sidebar [items]="nav" roleLabel="User Account" [open]="sidebarOpen()" (navClick)="sidebarOpen.set(false)" />
      <div class="dash-main">
        <header class="dash-header">
          <button type="button" class="btn btn-ghost btn-icon" style="display:none;" id="hamburger" (click)="sidebarOpen.set(true)">
            <span class="material-icons-round">menu</span>
          </button>
          <div class="page-title">User Dashboard</div>
          <div style="display:flex;align-items:center;gap:10px;">
            <div style="text-align:right;">
              <div style="font-size:.875rem;font-weight:600;color:var(--text-primary);">{{ auth.userName() }}</div>
              <div style="font-size:.75rem;color:var(--text-muted);">{{ auth.userEmail() }}</div>
            </div>
            <span class="badge badge-primary">USER</span>
            <button type="button" class="btn btn-ghost btn-icon btn-sm" title="Sign Out" (click)="auth.logout()" style="color:var(--danger);">
              <span class="material-icons-round">logout</span>
            </button>
          </div>
        </header>
        <div class="dash-content"><router-outlet /></div>
      </div>
    </div>
  `,
  styles: [`@media(max-width:768px){ #hamburger{ display:flex!important; } }`]
})
export class UserLayoutComponent {
  auth        = inject(AuthService);
  regState    = inject(RegistrationStateService);
  private toast = inject(ToastService);
  sidebarOpen = signal(false);
  nav: NavItem[] = [
    { label:'Dashboard',     icon:'dashboard',         route:'/user/dashboard' },
    { label:'Browse Events',    icon:'search',             route:'/user/my-events' },
    { label:'Events Attended',  icon:'verified',           route:'/user/events-attended' },
    { label:'My Wallet',        icon:'account_balance_wallet', route:'/user/wallet' },
    { label:'My Payments',      icon:'payment',            route:'/user/payments' },
    { label:'Calendar',         icon:'calendar_month',     route:'/user/calendar' },
    { label:'Reminders',     icon:'notifications',      route:'/user/reminders' },
    { label:'To-Do List',    icon:'checklist',          route:'/user/todos' },
    { label:'Profile',       icon:'manage_accounts',    route:'/user/profile' },
    { label:'Upgrade Role',  icon:'upgrade',            route:'/user/request-role' },
  ];
}
