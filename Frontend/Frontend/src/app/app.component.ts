import { Component, inject, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastContainerComponent } from './shared/components/toast/toast-container.component';
import { AuthService } from './core/services/auth.service';
import { ReminderNotificationService } from './core/services/reminder-notification.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, ToastContainerComponent],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  private auth            = inject(AuthService);
  private reminderService = inject(ReminderNotificationService);

  ngOnInit(): void {
    if (this.auth.isLoggedIn()) {
      this.reminderService.start();
    }
  }
}
