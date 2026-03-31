import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { SidebarComponent, NavItem } from '../../../shared/components/sidebar/sidebar.component';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterOutlet, CommonModule, SidebarComponent],
  templateUrl: './admin-layout.component.html',
  styleUrls: ['./admin-layout.component.css']
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
    { label: 'My Wallet',     icon: 'wallet',             route: '/admin/wallet' },
    { label: 'Role Requests', icon: 'upgrade',            route: '/admin/role-requests' },
    { label: 'Audit Logs',    icon: 'receipt_long',     route: '/admin/audit-logs' },
    { label: 'Profile',       icon: 'manage_accounts',  route: '/admin/profile' },
  ];
}
