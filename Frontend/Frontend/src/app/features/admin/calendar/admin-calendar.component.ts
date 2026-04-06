import { Component } from '@angular/core';
import { DashboardCalendarComponent } from '../../../shared/components/dashboard-calendar/dashboard-calendar.component';

@Component({
  selector: 'app-admin-calendar',
  standalone: true,
  imports: [DashboardCalendarComponent],
  templateUrl: './admin-calendar.component.html'
})
export class AdminCalendarComponent {}
