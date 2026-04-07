import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { NotificationDto } from '../models/models';
import { catchError, EMPTY } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/Notification`;

  readonly notifications = signal<NotificationDto[]>([]);
  readonly unreadCount   = computed(() => this.notifications().filter(n => !n.isRead).length);

  /** Load all notifications for the current user */
  loadNotifications() {
    this.http.get<NotificationDto[]>(this.base)
      .pipe(catchError(() => EMPTY))
      .subscribe(list => this.notifications.set(list));
  }

  /** Mark a single notification as read */
  markAsRead(id: number) {
    this.http.put<void>(`${this.base}/${id}/read`, {})
      .pipe(catchError(() => EMPTY))
      .subscribe(() => {
        this.notifications.update(list =>
          list.map(n => n.notificationId === id ? { ...n, isRead: true } : n)
        );
      });
  }

  /** Mark all as read */
  markAllAsRead() {
    this.http.put<void>(`${this.base}/read-all`, {})
      .pipe(catchError(() => EMPTY))
      .subscribe(() => {
        this.notifications.update(list => list.map(n => ({ ...n, isRead: true })));
      });
  }
}
