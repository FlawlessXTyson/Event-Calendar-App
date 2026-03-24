import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { CreateReminderRequest, ReminderResponse } from '../models/models';

@Injectable({ providedIn: 'root' })
export class ReminderService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/Reminder`;

  /** POST /api/Reminder — create a new reminder */
  create(dto: CreateReminderRequest) {
    return this.http.post<ReminderResponse>(this.base, dto);
  }

  /** GET /api/Reminder/me — all reminders for the logged-in user */
  getMyReminders() {
    return this.http.get<ReminderResponse[]>(`${this.base}/me`);
  }

  /**
   * GET /api/Reminder/due
   * Returns reminders whose ReminderDateTime falls within the
   * last 1 minute (server-side window). Used for polling notifications.
   */
  getDueReminders() {
    return this.http.get<ReminderResponse[]>(`${this.base}/due`);
  }

  /** DELETE /api/Reminder/{id} — only owner can delete */
  delete(id: number) {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
