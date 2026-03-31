import { Component } from '@angular/core';
import { DashboardCalendarComponent } from '../../../shared/components/dashboard-calendar/dashboard-calendar.component';

@Component({
  selector: 'app-organizer-calendar',
  standalone: true,
  imports: [DashboardCalendarComponent],
  templateUrl: './organizer-calendar.component.html',
  styleUrl: './organizer-calendar.component.css'
})
export class OrganizerCalendarComponent {}
