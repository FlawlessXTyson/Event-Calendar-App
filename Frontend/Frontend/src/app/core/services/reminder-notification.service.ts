import { Injectable, inject, OnDestroy } from '@angular/core';
import { interval, Subscription, switchMap, EMPTY } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ReminderService } from './reminder.service';
import { ToastService } from './toast.service';
import { AuthService } from './auth.service';
import { ReminderResponse } from '../models/models';

@Injectable({ providedIn: 'root' })
export class ReminderNotificationService implements OnDestroy {
  private readonly reminderSvc = inject(ReminderService);
  private readonly toast       = inject(ToastService);
  private readonly auth        = inject(AuthService);

  private readonly POLL_MS = 30_000; // 30 seconds
  private pollSub?: Subscription;

  /**
   * Tracks reminder IDs already shown in this session.
   * Prevents duplicate toasts — backend returns reminders within
   * a 1-minute window so the same reminder can appear in 2 consecutive polls.
   */
  private readonly fired = new Set<number>();

  constructor() {
    // Register stop() as the logout hook so polling stops on sign-out
    this.auth.registerLogoutHook(() => this.stop());
  }

  /** Call on login and on app-init when already logged in */
  start(): void {
    if (this.pollSub && !this.pollSub.closed) return; // already polling

    // Ask for browser notification permission once
    if ('Notification' in window && Notification.permission === 'default') {
      Notification.requestPermission();
    }

    // Fire immediately — don't wait 30s on first load
    this._checkNow();

    // Then every 30 seconds via RxJS interval
    this.pollSub = interval(this.POLL_MS).pipe(
      switchMap(() => {
        if (!this.auth.isLoggedIn()) {
          this.stop();
          return EMPTY;
        }
        return this.reminderSvc.getDueReminders().pipe(
          catchError(() => EMPTY) // silent on network error
        );
      })
    ).subscribe(reminders => this._notify(reminders));
  }

  /** Call on logout — clears polling and fired set */
  stop(): void {
    this.pollSub?.unsubscribe();
    this.pollSub = undefined;
    this.fired.clear();
  }

  private _checkNow(): void {
    if (!this.auth.isLoggedIn()) return;
    this.reminderSvc.getDueReminders()
      .pipe(catchError(() => EMPTY))
      .subscribe(reminders => this._notify(reminders));
  }

  private _notify(reminders: ReminderResponse[]): void {
    for (const r of reminders) {
      if (this.fired.has(r.reminderId)) continue; // deduplicate
      this.fired.add(r.reminderId);

      // In-app toast (12 seconds visible)
      this.toast.reminder(`⏰ ${r.reminderTitle}`, 'Reminder');

      // Browser push notification
      if ('Notification' in window && Notification.permission === 'granted') {
        new Notification('⏰ EventCalenderApp Reminder', {
          body:             r.reminderTitle,
          icon:             '/favicon.ico',
          tag:              `eca-reminder-${r.reminderId}`,
          requireInteraction: true
        });
      }
    }
  }

  ngOnDestroy(): void { this.stop(); }
}
