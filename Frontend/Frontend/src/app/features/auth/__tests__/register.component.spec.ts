import { TestBed, ComponentFixture } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { RegisterComponent } from '../register/register.component';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { ReminderNotificationService } from '../../../core/services/reminder-notification.service';
import { of, throwError } from 'rxjs';

describe('RegisterComponent', () => {
  let fixture: ComponentFixture<RegisterComponent>;
  let component: RegisterComponent;
  let authSvc: AuthService;
  let toastSvc: ToastService;
  let reminderSvc: ReminderNotificationService;

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [RegisterComponent, HttpClientTestingModule], providers: [provideRouter([])]
    }).compileComponents();
    fixture     = TestBed.createComponent(RegisterComponent);
    component   = fixture.componentInstance;
    authSvc     = TestBed.inject(AuthService);
    toastSvc    = TestBed.inject(ToastService);
    reminderSvc = TestBed.inject(ReminderNotificationService);
    fixture.detectChanges();
  });

  // ── Form initial state ────────────────────────────────────────────────────
  it('creates the component', () => {
    expect(component).toBeTruthy();
  });

  it('form is invalid when empty', () => {
    expect(component.form.invalid).toBeTrue();
  });

  it('username requires minimum 3 characters', () => {
    component.form.get('username')!.setValue('ab');
    expect(component.form.get('username')!.hasError('minlength')).toBeTrue();
  });

  it('username accepts 3+ characters', () => {
    component.form.get('username')!.setValue('Tyson');
    expect(component.form.get('username')!.errors).toBeNull();
  });

  it('email requires valid format', () => {
    component.form.get('email')!.setValue('notanemail');
    expect(component.form.get('email')!.errors?.['invalidEmail']).toBeDefined();
  });

  it('email accepts valid format', () => {
    component.form.get('email')!.setValue('tyson@gmail.com');
    expect(component.form.get('email')!.errors).toBeNull();
  });

  it('password requires minimum 6 characters', () => {
    component.form.get('password')!.setValue('abc');
    expect(component.form.get('password')!.errors?.['weakPassword']).toBeDefined();
  });

  it('password accepts 6+ characters', () => {
    component.form.get('password')!.setValue('secure123');
    expect(component.form.get('password')!.errors).toBeNull();
  });

  it('form-level mismatch error when passwords do not match', () => {
    component.form.patchValue({
      username: 'Tyson', email: 'tyson@gmail.com',
      password: 'pass123', confirmPassword: 'different'
    });
    expect(component.form.hasError('mismatch')).toBeTrue();
  });

  it('no mismatch error when passwords match', () => {
    component.form.patchValue({
      username: 'Tyson', email: 'tyson@gmail.com',
      password: 'pass123', confirmPassword: 'pass123'
    });
    expect(component.form.hasError('mismatch')).toBeFalse();
  });

  it('form is valid with all correct values', () => {
    component.form.patchValue({
      username: 'Tyson', email: 'tyson@gmail.com',
      password: 'pass123', confirmPassword: 'pass123'
    });
    expect(component.form.valid).toBeTrue();
  });

  // ── getError() helper ─────────────────────────────────────────────────────
  it('getError() returns required message for empty field', () => {
    component.form.get('username')!.setValue('');
    component.form.get('username')!.markAsTouched();
    expect(component.getError('username')).toContain('required');
  });

  it('getError() returns minlength message for short username', () => {
    component.form.get('username')!.setValue('ab');
    component.form.get('username')!.markAsTouched();
    expect(component.getError('username')).toContain('Minimum');
  });

  // ── submit() ──────────────────────────────────────────────────────────────
  it('submit() marks all as touched when invalid', () => {
    component.submit();
    expect(component.form.get('username')!.touched).toBeTrue();
    expect(component.form.get('email')!.touched).toBeTrue();
    expect(component.form.get('password')!.touched).toBeTrue();
  });

  it('submit() does not call auth.register when form is invalid', () => {
    const spy = spyOn(authSvc, 'register');
    component.submit();
    expect(spy).not.toHaveBeenCalled();
  });

  it('submit() on success calls reminder.start() and redirectByRole()', () => {
    spyOn(authSvc, 'register').and.returnValue(of({ token: 'fake' }) as any);
    const reminderSpy = spyOn(reminderSvc, 'start');
    const redirectSpy = spyOn(authSvc, 'redirectByRole');
    const toastSpy    = spyOn(toastSvc, 'success');
    component.form.patchValue({
      username: 'Tyson', email: 'tyson@gmail.com',
      password: 'pass123', confirmPassword: 'pass123'
    });
    component.submit();
    expect(reminderSpy).toHaveBeenCalled();
    expect(redirectSpy).toHaveBeenCalled();
    expect(toastSpy).toHaveBeenCalled();
  });

  it('submit() calls register with correct username, email, password', () => {
    const registerSpy = spyOn(authSvc, 'register').and.returnValue(of({ token: 'fake' }) as any);
    spyOn(reminderSvc, 'start');
    spyOn(authSvc, 'redirectByRole');
    component.form.patchValue({
      username: 'Tyson', email: 'tyson@gmail.com',
      password: 'pass123', confirmPassword: 'pass123'
    });
    component.submit();
    expect(registerSpy).toHaveBeenCalledWith({
      username: 'Tyson', email: 'tyson@gmail.com', password: 'pass123'
    });
  });

  it('submit() on error resets loading', () => {
    spyOn(authSvc, 'register').and.returnValue(throwError(() => ({ status: 400 })));
    component.form.patchValue({
      username: 'Tyson', email: 'tyson@gmail.com',
      password: 'pass123', confirmPassword: 'pass123'
    });
    component.submit();
    expect(component.loading()).toBeFalse();
  });

  // ── Benefits list ─────────────────────────────────────────────────────────
  it('benefits array has 5 items', () => {
    expect(component.benefits.length).toBe(5);
  });
});
