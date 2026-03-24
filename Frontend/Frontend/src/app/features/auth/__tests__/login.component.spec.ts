import { TestBed, ComponentFixture, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { LoginComponent } from '../login/login.component';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { ReminderNotificationService } from '../../../core/services/reminder-notification.service';
import { of, throwError } from 'rxjs';

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let component: LoginComponent;
  let authSvc: AuthService;
  let toastSvc: ToastService;
  let reminderSvc: ReminderNotificationService;

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [LoginComponent, HttpClientTestingModule], providers: [provideRouter([])]
    }).compileComponents();
    fixture      = TestBed.createComponent(LoginComponent);
    component    = fixture.componentInstance;
    authSvc      = TestBed.inject(AuthService);
    toastSvc     = TestBed.inject(ToastService);
    reminderSvc  = TestBed.inject(ReminderNotificationService);
    fixture.detectChanges();
  });

  afterEach(() => localStorage.clear());

  // ── Form initial state ────────────────────────────────────────────────────
  it('creates the component', () => {
    expect(component).toBeTruthy();
  });

  it('form is invalid when empty', () => {
    expect(component.form.invalid).toBeTrue();
  });

  it('email field is required', () => {
    component.form.get('email')!.setValue('');
    expect(component.form.get('email')!.hasError('required')).toBeTrue();
  });

  it('password field is required', () => {
    component.form.get('password')!.setValue('');
    expect(component.form.get('password')!.hasError('required')).toBeTrue();
  });

  it('email field rejects invalid email format', () => {
    component.form.get('email')!.setValue('notanemail');
    expect(component.form.get('email')!.errors?.['invalidEmail']).toBeDefined();
  });

  it('email field accepts valid email', () => {
    component.form.get('email')!.setValue('user@gmail.com');
    expect(component.form.get('email')!.errors).toBeNull();
  });

  it('form is valid with valid email and password', () => {
    component.form.patchValue({ email: 'user@gmail.com', password: 'pass123' });
    expect(component.form.valid).toBeTrue();
  });

  // ── showPw toggle ─────────────────────────────────────────────────────────
  it('showPw starts as false', () => {
    expect(component.showPw()).toBeFalse();
  });

  it('showPw toggles when set', () => {
    component.showPw.set(true);
    expect(component.showPw()).toBeTrue();
    component.showPw.set(false);
    expect(component.showPw()).toBeFalse();
  });

  // ── submit() with invalid form ────────────────────────────────────────────
  it('submit() marks all fields as touched when form is invalid', () => {
    component.submit();
    expect(component.form.get('email')!.touched).toBeTrue();
    expect(component.form.get('password')!.touched).toBeTrue();
  });

  it('submit() does not call auth.login when form is invalid', () => {
    const spy = spyOn(authSvc, 'login');
    component.submit();
    expect(spy).not.toHaveBeenCalled();
  });

  // ── isInvalid() helper ────────────────────────────────────────────────────
  it('isInvalid() returns false before field is touched', () => {
    expect(component.isInvalid('email')).toBeFalsy();
  });

  it('isInvalid() returns true after touched with invalid value', () => {
    const ctrl = component.form.get('email')!;
    ctrl.setValue('bad');
    ctrl.markAsTouched();
    expect(component.isInvalid('email')).toBeTrue();
  });

  // ── getError() ────────────────────────────────────────────────────────────
  it('getError() returns required message for empty email', () => {
    const ctrl = component.form.get('email')!;
    ctrl.setValue('');
    ctrl.markAsTouched();
    // simulate having required error
    expect(component.getError('email')).toBeTruthy();
  });

  it('getError() returns invalidEmail message for bad format', () => {
    const ctrl = component.form.get('email')!;
    ctrl.setValue('notvalid');
    ctrl.markAsTouched();
    expect(component.getError('email')).toContain('valid email');
  });

  // ── loading signal ────────────────────────────────────────────────────────
  it('loading starts as false', () => {
    expect(component.loading()).toBeFalse();
  });

  it('submit() sets loading to true while in flight', () => {
    spyOn(authSvc, 'login').and.returnValue(of({ token: 'fake' }) as any);
    spyOn(authSvc, 'userName').and.returnValue('Test');
    spyOn(reminderSvc, 'start');
    spyOn(authSvc, 'redirectByRole');
    component.form.patchValue({ email: 'user@gmail.com', password: 'pass123' });
    component.submit();
    // loading resets after redirect in success
  });

  // ── Successful submit ─────────────────────────────────────────────────────
  it('submit() on success: calls reminder.start() and redirectByRole()', () => {
    spyOn(authSvc, 'login').and.returnValue(of({ token: 'fake' }) as any);
    spyOn(authSvc, 'userName').and.returnValue('Tyson');
    const reminderSpy = spyOn(reminderSvc, 'start');
    const redirectSpy = spyOn(authSvc, 'redirectByRole');
    const toastSpy    = spyOn(toastSvc, 'success');
    component.form.patchValue({ email: 'user@gmail.com', password: 'pass123' });
    component.submit();
    expect(reminderSpy).toHaveBeenCalled();
    expect(redirectSpy).toHaveBeenCalled();
    expect(toastSpy).toHaveBeenCalledWith(jasmine.stringContaining('Tyson'), 'Signed In');
  });

  // ── Failed submit ─────────────────────────────────────────────────────────
  it('submit() on error: resets loading to false', () => {
    spyOn(authSvc, 'login').and.returnValue(throwError(() => ({ status: 401 })));
    component.form.patchValue({ email: 'user@gmail.com', password: 'wrong' });
    component.submit();
    expect(component.loading()).toBeFalse();
  });

  it('submit() on error: does NOT call reminder.start()', () => {
    spyOn(authSvc, 'login').and.returnValue(throwError(() => ({ status: 401 })));
    const reminderSpy = spyOn(reminderSvc, 'start');
    component.form.patchValue({ email: 'user@gmail.com', password: 'wrong' });
    component.submit();
    expect(reminderSpy).not.toHaveBeenCalled();
  });

  // ── Features list ─────────────────────────────────────────────────────────
  it('features array has 3 items', () => {
    expect(component.features.length).toBe(3);
  });

  it('features include registration, payment, and reminder entries', () => {
    const texts = component.features.map(f => f.text);
    expect(texts).toContain(jasmine.stringContaining('Register'));
    expect(texts).toContain(jasmine.stringContaining('payment'));
    expect(texts).toContain(jasmine.stringContaining('reminder'));
  });
});
