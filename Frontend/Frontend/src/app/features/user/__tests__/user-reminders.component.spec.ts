import { TestBed, ComponentFixture } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { UserRemindersComponent } from '../reminders/user-reminders.component';
import { ReminderService } from '../../../core/services/reminder.service';
import { ToastService } from '../../../core/services/toast.service';
import { of, throwError } from 'rxjs';

const mockReminder = (id: number, title: string, dt?: string) => ({
  reminderId: id, userId: 1, eventId: undefined,
  reminderTitle: title,
  reminderDateTime: dt ?? '2026-04-01T10:00:00Z',
  createdAt: '2026-03-20T00:00:00Z'
});

describe('UserRemindersComponent', () => {
  let fixture: ComponentFixture<UserRemindersComponent>;
  let component: UserRemindersComponent;
  let reminderSvc: ReminderService;
  let toastSvc: ToastService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UserRemindersComponent, HttpClientTestingModule], providers: [provideRouter([])]
    }).compileComponents();
    fixture      = TestBed.createComponent(UserRemindersComponent);
    component    = fixture.componentInstance;
    reminderSvc  = TestBed.inject(ReminderService);
    toastSvc     = TestBed.inject(ToastService);
    spyOn(reminderSvc, 'getMyReminders').and.returnValue(of([mockReminder(1, 'Team Meeting')]));
    fixture.detectChanges();
  });

  // ── Init ──────────────────────────────────────────────────────────────────
  it('creates the component', () => {
    expect(component).toBeTruthy();
  });

  it('loads reminders on init', () => {
    expect(reminderSvc.getMyReminders).toHaveBeenCalled();
    expect(component.reminders().length).toBe(1);
  });

  it('sets loading to false after load', () => {
    expect(component.loading()).toBeFalse();
  });

  it('loading stays false even on error', async () => {
    (reminderSvc.getMyReminders as jasmine.Spy).and.returnValue(
      throwError(() => new Error('Network'))
    );
    component.load();
    expect(component.loading()).toBeFalse();
  });

  // ── showForm toggle ───────────────────────────────────────────────────────
  it('showForm starts as false', () => {
    expect(component.showForm()).toBeFalse();
  });

  it('showForm toggles on button click', () => {
    component.showForm.set(true);
    expect(component.showForm()).toBeTrue();
    component.showForm.set(false);
    expect(component.showForm()).toBeFalse();
  });

  // ── Form validation ───────────────────────────────────────────────────────
  it('reminderTitle is required', () => {
    component.form.get('reminderTitle')!.setValue('');
    component.form.get('reminderTitle')!.markAsTouched();
    expect(component.fi('reminderTitle')).toBeTrue();
  });

  it('form is invalid when title is empty', () => {
    expect(component.form.invalid).toBeTrue();
  });

  it('form is valid when title is set', () => {
    component.form.patchValue({ reminderTitle: 'Doctor visit' });
    expect(component.form.invalid).toBeFalse();
  });

  // ── submit() validation rules ─────────────────────────────────────────────
  it('submit() marks all as touched when form invalid', () => {
    component.submit();
    expect(component.form.get('reminderTitle')!.touched).toBeTrue();
  });

  it('submit() shows error toast when BOTH dateTime AND minutesBefore provided', () => {
    const toastSpy = spyOn(toastSvc, 'error');
    component.form.patchValue({
      reminderTitle: 'Test',
      reminderDateTime: '2026-04-01T10:00',
      minutesBefore: 30,
      eventId: 5
    });
    component.submit();
    expect(toastSpy).toHaveBeenCalledWith(
      jasmine.stringContaining('not both'), jasmine.any(String)
    );
  });

  it('submit() shows error when NEITHER dateTime NOR minutesBefore provided', () => {
    const toastSpy = spyOn(toastSvc, 'error');
    component.form.patchValue({
      reminderTitle: 'Test',
      reminderDateTime: '',
      minutesBefore: null,
      eventId: null
    });
    component.submit();
    expect(toastSpy).toHaveBeenCalled();
  });

  // ── submit() happy paths ──────────────────────────────────────────────────
  it('submit() with dateTime only calls reminderSvc.create with ISO datetime', () => {
    const spy = spyOn(reminderSvc, 'create').and.returnValue(of(mockReminder(2, 'Test')));
    spyOn(toastSvc, 'success');
    component.form.patchValue({
      reminderTitle: 'Test',
      reminderDateTime: '2026-04-01T10:00',
      minutesBefore: null, eventId: null
    });
    component.submit();
    const call = (spy.calls.mostRecent().args[0] as any);
    expect(call.reminderTitle).toBe('Test');
    expect(call.reminderDateTime).toBeDefined();
    expect(call.minutesBefore).toBeUndefined();
  });

  it('submit() with eventId+minutesBefore sends correct payload', () => {
    const spy = spyOn(reminderSvc, 'create').and.returnValue(of(mockReminder(2, 'Test')));
    spyOn(toastSvc, 'success');
    component.form.patchValue({
      reminderTitle: 'Test',
      reminderDateTime: '',
      minutesBefore: 30,
      eventId: 5
    });
    component.submit();
    const call = (spy.calls.mostRecent().args[0] as any);
    expect(call.minutesBefore).toBe(30);
    expect(call.eventId).toBe(5);
    expect(call.reminderDateTime).toBeUndefined();
  });

  it('submit() prepends new reminder to list on success', () => {
    spyOn(reminderSvc, 'create').and.returnValue(of(mockReminder(99, 'New')));
    spyOn(toastSvc, 'success');
    component.form.patchValue({
      reminderTitle: 'New',
      reminderDateTime: '2026-04-01T10:00',
      minutesBefore: null, eventId: null
    });
    component.submit();
    expect(component.reminders()[0].reminderTitle).toBe('New');
    expect(component.reminders().length).toBe(2);
  });

  it('submit() resets form and hides panel on success', () => {
    spyOn(reminderSvc, 'create').and.returnValue(of(mockReminder(2, 'New')));
    spyOn(toastSvc, 'success');
    component.showForm.set(true);
    component.form.patchValue({
      reminderTitle: 'New',
      reminderDateTime: '2026-04-01T10:00',
      minutesBefore: null, eventId: null
    });
    component.submit();
    expect(component.showForm()).toBeFalse();
    expect(component.form.get('reminderTitle')!.value).toBeFalsy();
  });

  it('submit() resets saving to false on error', () => {
    spyOn(reminderSvc, 'create').and.returnValue(throwError(() => new Error('fail')));
    component.form.patchValue({
      reminderTitle: 'Test',
      reminderDateTime: '2026-04-01T10:00',
      minutesBefore: null, eventId: null
    });
    component.submit();
    expect(component.saving()).toBeFalse();
  });

  // ── del() ─────────────────────────────────────────────────────────────────
  it('del() calls reminderSvc.delete with correct id', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    spyOn(reminderSvc, 'delete').and.returnValue(of(undefined));
    spyOn(toastSvc, 'success');
    component.del(1);
    expect(reminderSvc.delete).toHaveBeenCalledWith(1);
  });

  it('del() removes reminder from list on success', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    spyOn(reminderSvc, 'delete').and.returnValue(of(undefined));
    spyOn(toastSvc, 'success');
    component.del(1);
    expect(component.reminders().find(r => r.reminderId === 1)).toBeUndefined();
    expect(component.reminders().length).toBe(0);
  });

  it('del() does not call service when confirm is cancelled', () => {
    spyOn(window, 'confirm').and.returnValue(false);
    const spy = spyOn(reminderSvc, 'delete');
    component.del(1);
    expect(spy).not.toHaveBeenCalled();
  });

  it('del() resets deleting to null on error', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    spyOn(reminderSvc, 'delete').and.returnValue(throwError(() => new Error('fail')));
    component.del(1);
    expect(component.deleting()).toBeNull();
  });

  // ── fi() helper ───────────────────────────────────────────────────────────
  it('fi() returns falsy when field is valid and untouched', () => {
    component.form.get('reminderTitle')!.setValue('something');
    expect(component.fi('reminderTitle')).toBeFalsy();
  });

  it('fi() returns true when field is invalid and touched', () => {
    component.form.get('reminderTitle')!.setValue('');
    component.form.get('reminderTitle')!.markAsTouched();
    expect(component.fi('reminderTitle')).toBeTrue();
  });
});
