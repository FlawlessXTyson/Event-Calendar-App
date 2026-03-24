import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ReminderService } from '../reminder.service';
import { environment } from '../../../../environments/environment';

describe('ReminderService', () => {
  let service: ReminderService;
  let http: HttpTestingController;
  const base = `${environment.apiUrl}/Reminder`;

  const mockReminder = {
    reminderId: 1, userId: 42, eventId: null,
    reminderTitle: 'Team Standup', reminderDateTime: '2026-03-22T09:00:00Z',
    createdAt: '2026-03-20T10:00:00Z'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(ReminderService);
    http    = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  // ── create() ──────────────────────────────────────────────────────────────
  it('create() sends POST to /api/Reminder with correct body', () => {
    const dto = { reminderTitle: 'Team Standup', reminderDateTime: '2026-03-22T09:00:00Z' };
    service.create(dto).subscribe();
    const req = http.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(dto);
    req.flush(mockReminder);
  });

  it('create() returns the created ReminderResponse', () => {
    let result: any;
    service.create({ reminderTitle: 'Test', reminderDateTime: '2026-03-22T09:00:00Z' })
      .subscribe(r => result = r);
    http.expectOne(base).flush(mockReminder);
    expect(result.reminderId).toBe(1);
    expect(result.reminderTitle).toBe('Team Standup');
  });

  it('create() sends event-based reminder with eventId and minutesBefore', () => {
    const dto = { reminderTitle: 'Before event', eventId: 5, minutesBefore: 30 };
    service.create(dto).subscribe();
    const req = http.expectOne(base);
    expect(req.request.body.eventId).toBe(5);
    expect(req.request.body.minutesBefore).toBe(30);
    req.flush(mockReminder);
  });

  // ── getMyReminders() ──────────────────────────────────────────────────────
  it('getMyReminders() sends GET to /api/Reminder/me', () => {
    service.getMyReminders().subscribe();
    const req = http.expectOne(`${base}/me`);
    expect(req.request.method).toBe('GET');
    req.flush([mockReminder]);
  });

  it('getMyReminders() returns array of reminders', () => {
    let result: any[];
    service.getMyReminders().subscribe(rs => result = rs);
    http.expectOne(`${base}/me`).flush([mockReminder, { ...mockReminder, reminderId: 2 }]);
    expect(result!.length).toBe(2);
  });

  // ── getDueReminders() ─────────────────────────────────────────────────────
  it('getDueReminders() sends GET to /api/Reminder/due', () => {
    service.getDueReminders().subscribe();
    const req = http.expectOne(`${base}/due`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('getDueReminders() returns empty array when no due reminders', () => {
    let result: any[];
    service.getDueReminders().subscribe(rs => result = rs);
    http.expectOne(`${base}/due`).flush([]);
    expect(result!).toEqual([]);
  });

  it('getDueReminders() returns due reminders from server', () => {
    let result: any[];
    service.getDueReminders().subscribe(rs => result = rs);
    http.expectOne(`${base}/due`).flush([mockReminder]);
    expect(result!.length).toBe(1);
    expect(result![0].reminderTitle).toBe('Team Standup');
  });

  // ── delete() ──────────────────────────────────────────────────────────────
  it('delete() sends DELETE to /api/Reminder/{id}', () => {
    service.delete(1).subscribe();
    const req = http.expectOne(`${base}/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });
  });

  it('delete() sends correct id in URL', () => {
    service.delete(99).subscribe();
    const req = http.expectOne(`${base}/99`);
    expect(req.request.url).toContain('/99');
    req.flush(null, { status: 204, statusText: 'No Content' });
  });
});
