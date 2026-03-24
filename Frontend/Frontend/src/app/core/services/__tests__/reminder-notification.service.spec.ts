import { TestBed, fakeAsync, tick, discardPeriodicTasks } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ReminderNotificationService } from '../reminder-notification.service';
import { ToastService } from '../toast.service';
import { AuthService } from '../auth.service';
import { ReminderService } from '../reminder.service';
import { of, throwError } from 'rxjs';

describe('ReminderNotificationService', () => {
  let service: ReminderNotificationService;
  let toastSvc: ToastService;
  let authSvc: AuthService;
  let reminderSvc: ReminderService;

  const dueReminder = {
    reminderId: 101,
    userId: 1,
    reminderTitle: 'Team Meeting',
    reminderDateTime: new Date().toISOString(),
    createdAt: new Date().toISOString()
  };

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule], providers: [provideRouter([])]
    });
    service     = TestBed.inject(ReminderNotificationService);
    toastSvc    = TestBed.inject(ToastService);
    authSvc     = TestBed.inject(AuthService);
    reminderSvc = TestBed.inject(ReminderService);
  });

  afterEach(() => {
    service.stop();
    localStorage.clear();
  });

  // ── Constructor registers logout hook ─────────────────────────────────────
  it('registers a logout hook with AuthService on construction', () => {
    const hookSpy = spyOn(authSvc, 'registerLogoutHook');
    // Re-instantiate to check constructor
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule], providers: [provideRouter([])] });
    TestBed.inject(ReminderNotificationService);
    expect(hookSpy).toHaveBeenCalledWith(jasmine.any(Function));
  });

  // ── start() fires immediately ─────────────────────────────────────────────
  it('start() calls getDueReminders immediately on first call', () => {
    spyOn(authSvc, 'isLoggedIn').and.returnValue(true);
    const spy = spyOn(reminderSvc, 'getDueReminders').and.returnValue(of([]));
    service.start();
    expect(spy).toHaveBeenCalledTimes(1);
    service.stop();
  });

  // ── No duplicate start ────────────────────────────────────────────────────
  it('start() called twice does not create two polling subscriptions', fakeAsync(() => {
    spyOn(authSvc, 'isLoggedIn').and.returnValue(true);
    let callCount = 0;
    spyOn(reminderSvc, 'getDueReminders').and.callFake(() => { callCount++; return of([]); });
    service.start();
    service.start(); // second call ignored
    const firstCallCount = callCount;
    tick(30000);
    // Should have only 1 poll (immediate) + 1 interval tick = 2 max
    expect(callCount).toBeLessThanOrEqual(firstCallCount + 1);
    discardPeriodicTasks();
    service.stop();
  }));

  // ── Toast notification on due reminder ────────────────────────────────────
  it('shows reminder toast when a due reminder is returned', () => {
    spyOn(authSvc, 'isLoggedIn').and.returnValue(true);
    spyOn(reminderSvc, 'getDueReminders').and.returnValue(of([dueReminder]));
    const toastSpy = spyOn(toastSvc, 'reminder');
    service.start();
    expect(toastSpy).toHaveBeenCalledWith(
      jasmine.stringContaining('Team Meeting'),
      'Reminder'
    );
    service.stop();
  });

  // ── Deduplication ─────────────────────────────────────────────────────────
  it('does NOT show toast for same reminder twice in same session', fakeAsync(() => {
    spyOn(authSvc, 'isLoggedIn').and.returnValue(true);
    spyOn(reminderSvc, 'getDueReminders').and.returnValue(of([dueReminder]));
    const toastSpy = spyOn(toastSvc, 'reminder');
    service.start();
    expect(toastSpy).toHaveBeenCalledTimes(1);
    tick(30000); // second poll
    expect(toastSpy).toHaveBeenCalledTimes(1); // still 1 — deduplicated
    discardPeriodicTasks();
    service.stop();
  }));

  it('shows toasts for two different reminders independently', () => {
    spyOn(authSvc, 'isLoggedIn').and.returnValue(true);
    const r2 = { ...dueReminder, reminderId: 202, reminderTitle: 'Dentist Appointment' };
    spyOn(reminderSvc, 'getDueReminders').and.returnValue(of([dueReminder, r2]));
    const toastSpy = spyOn(toastSvc, 'reminder');
    service.start();
    expect(toastSpy).toHaveBeenCalledTimes(2);
    service.stop();
  });

  // ── No toast when not logged in ───────────────────────────────────────────
  it('does NOT call getDueReminders when user is not logged in', () => {
    spyOn(authSvc, 'isLoggedIn').and.returnValue(false);
    const spy = spyOn(reminderSvc, 'getDueReminders').and.returnValue(of([]));
    service.start();
    expect(spy).not.toHaveBeenCalled();
    service.stop();
  });

  // ── Error silently ignored ────────────────────────────────────────────────
  it('silently ignores network errors and does not crash', () => {
    spyOn(authSvc, 'isLoggedIn').and.returnValue(true);
    spyOn(reminderSvc, 'getDueReminders').and.returnValue(throwError(() => new Error('Network error')));
    expect(() => service.start()).not.toThrow();
    service.stop();
  });

  // ── stop() clears fired set ───────────────────────────────────────────────
  it('stop() clears fired set so reminder shows again after re-start', () => {
    spyOn(authSvc, 'isLoggedIn').and.returnValue(true);
    spyOn(reminderSvc, 'getDueReminders').and.returnValue(of([dueReminder]));
    const toastSpy = spyOn(toastSvc, 'reminder');
    service.start();
    expect(toastSpy).toHaveBeenCalledTimes(1);
    service.stop();
    service.start(); // restart — fired set cleared
    expect(toastSpy).toHaveBeenCalledTimes(2);
    service.stop();
  });

  // ── Polling interval ──────────────────────────────────────────────────────
  it('polls again after 30 seconds', fakeAsync(() => {
    spyOn(authSvc, 'isLoggedIn').and.returnValue(true);
    const r1 = { ...dueReminder, reminderId: 301, reminderTitle: 'First' };
    const r2 = { ...dueReminder, reminderId: 302, reminderTitle: 'Second' };
    let call = 0;
    spyOn(reminderSvc, 'getDueReminders').and.callFake(() => {
      return of(call++ === 0 ? [r1] : [r2]);
    });
    const toastSpy = spyOn(toastSvc, 'reminder');
    service.start();
    expect(toastSpy).toHaveBeenCalledTimes(1);
    tick(30000);
    expect(toastSpy).toHaveBeenCalledTimes(2);
    discardPeriodicTasks();
    service.stop();
  }));

  // ── Empty result no toast ─────────────────────────────────────────────────
  it('shows no toast when getDueReminders returns empty array', () => {
    spyOn(authSvc, 'isLoggedIn').and.returnValue(true);
    spyOn(reminderSvc, 'getDueReminders').and.returnValue(of([]));
    const toastSpy = spyOn(toastSvc, 'reminder');
    service.start();
    expect(toastSpy).not.toHaveBeenCalled();
    service.stop();
  });
});
