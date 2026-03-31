import { Component } from '@angular/core';
import { DashboardCalendarComponent } from '../../../shared/components/dashboard-calendar/dashboard-calendar.component';

@Component({
  selector: 'app-user-calendar',
  standalone: true,
  imports: [DashboardCalendarComponent],
  templateUrl: './user-calendar.component.html',
  styleUrl: './user-calendar.component.css'
})
export class UserCalendarComponent {}
