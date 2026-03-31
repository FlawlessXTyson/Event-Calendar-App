import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { SidebarComponent, NavItem } from '../../../shared/components/sidebar/sidebar.component';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-organizer-layout',
  standalone: true,
  imports: [RouterOutlet, CommonModule, SidebarComponent],
  templateUrl: './organizer-layout.component.html',
  styleUrl: './organizer-layout.component.css'
})
export class OrganizerLayoutComponent {
  auth        = inject(AuthService);
  sidebarOpen = signal(false);
  nav: NavItem[] = [
    { label:'Dashboard',         icon:'dashboard',              route:'/organizer/dashboard' },
    { label:'Create Event',      icon:'add_circle',             route:'/organizer/create-event' },
    { label:'My Events',         icon:'event_note',             route:'/organizer/my-events' },
    { label:'Registrations',     icon:'people',                 route:'/organizer/registrations' },
    { label:'Earnings',          icon:'account_balance_wallet', route:'/organizer/earnings' },
    { label:'Refunds',           icon:'currency_rupee',         route:'/organizer/refunds' },
    { label:'My Wallet',         icon:'wallet',                 route:'/organizer/wallet' },
    { label:'Calendar',          icon:'calendar_month',         route:'/organizer/calendar' },
    { label:'Profile',           icon:'manage_accounts',        route:'/organizer/profile' },
  ];
}
