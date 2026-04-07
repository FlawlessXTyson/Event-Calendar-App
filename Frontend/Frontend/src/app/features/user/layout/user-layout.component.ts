import { Component, inject, signal } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { SidebarComponent, NavItem } from '../../../shared/components/sidebar/sidebar.component';
import { AuthService } from '../../../core/services/auth.service';
import { RegistrationStateService } from '../../../core/services/registration-state.service';
import { ToastService } from '../../../core/services/toast.service';
import { NotificationBellComponent } from '../../../shared/components/notification-bell/notification-bell.component';

@Component({
  selector: 'app-user-layout',
  standalone: true,
  imports: [RouterOutlet, CommonModule, SidebarComponent, NotificationBellComponent],
  templateUrl: './user-layout.component.html',
  styleUrl: './user-layout.component.css'
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
