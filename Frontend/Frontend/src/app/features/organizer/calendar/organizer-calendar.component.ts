import { Component } from '@angular/core';
import { DashboardCalendarComponent } from '../../../shared/components/dashboard-calendar/dashboard-calendar.component';

@Component({
  selector: 'app-organizer-calendar',
  standalone: true,
  imports: [DashboardCalendarComponent],
  template: `<app-dashboard-calendar eventRoutePrefix="/events" />`
})
export class OrganizerCalendarComponent {}
