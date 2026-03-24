import { Component, inject, signal } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { ReminderNotificationService } from '../../../core/services/reminder-notification.service';
import { strictEmail, minPassword, passwordsMatch } from '../../../core/validators/custom.validators';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div style="min-height:100vh;display:flex;">
      <div style="flex:0 0 42%;background:linear-gradient(145deg,var(--primary-dark),var(--primary),var(--secondary));color:#fff;display:flex;flex-direction:column;justify-content:center;padding:60px;position:relative;overflow:hidden;" class="hide-mobile">
        <div style="position:relative;">
          <a routerLink="/" style="display:flex;align-items:center;gap:12px;margin-bottom:48px;text-decoration:none;color:inherit;">
            <div style="width:44px;height:44px;background:rgba(255,255,255,.15);border-radius:12px;display:flex;align-items:center;justify-content:center;">
              <span class="material-icons-round" style="font-size:24px;">calendar_month</span>
            </div>
            <span style="font-family:var(--font-display);font-weight:800;font-size:1.1rem;">EventCalenderApp</span>
          </a>
          <h2 style="font-size:2rem;margin-bottom:16px;">Join Our Community</h2>
          <p style="opacity:.8;font-size:1rem;line-height:1.7;margin-bottom:36px;">
            Create your free account and start exploring thousands of events today.
          </p>
          <div style="display:flex;flex-direction:column;gap:10px;">
            @for (b of benefits; track b) {
              <div style="display:flex;align-items:center;gap:10px;font-size:.9rem;">
                <span class="material-icons-round" style="font-size:18px;color:rgba(255,255,255,.8);">check_circle</span>
                {{ b }}
              </div>
            }
          </div>
        </div>
      </div>

      <div style="flex:1;display:flex;flex-direction:column;align-items:center;justify-content:center;padding:40px 24px;overflow-y:auto;position:relative;">

        <!-- Back to Home -->
        <div style="position:absolute;top:24px;left:24px;">
          <a routerLink="/"
            style="display:inline-flex;align-items:center;gap:6px;color:var(--text-muted);font-size:.875rem;font-weight:500;text-decoration:none;padding:8px 14px;border-radius:8px;border:1px solid var(--border);background:var(--surface);transition:all .15s ease;"
            onmouseover="this.style.color='var(--primary)';this.style.borderColor='var(--primary)';this.style.background='var(--primary-light)'"
            onmouseout="this.style.color='var(--text-muted)';this.style.borderColor='var(--border)';this.style.background='var(--surface)'">
            <span class="material-icons-round" style="font-size:16px;">arrow_back</span>
            Back to Home
          </a>
        </div>

        <div style="width:100%;max-width:420px;">
          <div style="text-align:center;margin-bottom:32px;">
            <h1 style="font-size:1.75rem;margin-bottom:8px;">Create Account</h1>
            <p>Already have an account?
              <a routerLink="/auth/login" style="color:var(--primary);font-weight:600;">Sign in</a>
            </p>
          </div>

          <form [formGroup]="form" (ngSubmit)="submit()">
            <div class="form-group">
              <label class="form-label">Full Name <span class="required">*</span></label>
              <input formControlName="username" type="text" class="form-control"
                [class.is-invalid]="isInvalid('username')"
                placeholder="John Doe" autocomplete="name" />
              @if (isInvalid('username')) {
                <div class="form-error"><span class="material-icons-round" style="font-size:14px;">error</span>{{ getError('username') }}</div>
              }
            </div>

            <div class="form-group">
              <label class="form-label">Email Address <span class="required">*</span></label>
              <input formControlName="email" type="email" class="form-control"
                [class.is-invalid]="isInvalid('email')"
                placeholder="user@gmail.com" autocomplete="email" />
              @if (isInvalid('email')) {
                <div class="form-error"><span class="material-icons-round" style="font-size:14px;">error</span>{{ getError('email') }}</div>
              }
              <div class="form-hint">Must be a valid email like: userMust be a valid email like: user&#64;example.com#64;gmail.com</div>
            </div>

            <div class="form-group">
              <label class="form-label">Password <span class="required">*</span></label>
              <div style="position:relative;">
                <input formControlName="password" [type]="showPw() ? 'text' : 'password'"
                  class="form-control" [class.is-invalid]="isInvalid('password')"
                  placeholder="Minimum 6 characters" autocomplete="new-password"
                  style="padding-right:44px;" />
                <button type="button" (click)="showPw.set(!showPw())"
                  style="position:absolute;right:10px;top:50%;transform:translateY(-50%);background:none;border:none;cursor:pointer;color:var(--text-muted);display:flex;">
                  <span class="material-icons-round" style="font-size:20px;">{{ showPw() ? 'visibility_off' : 'visibility' }}</span>
                </button>
              </div>
              @if (isInvalid('password')) {
                <div class="form-error"><span class="material-icons-round" style="font-size:14px;">error</span>{{ getError('password') }}</div>
              }
            </div>

            <div class="form-group">
              <label class="form-label">Confirm Password <span class="required">*</span></label>
              <input formControlName="confirmPassword" [type]="showPw() ? 'text' : 'password'"
                class="form-control"
                [class.is-invalid]="form.get('confirmPassword')?.touched && form.hasError('mismatch')"
                placeholder="Repeat your password" autocomplete="new-password" />
              @if (form.get('confirmPassword')?.touched && form.hasError('mismatch')) {
                <div class="form-error"><span class="material-icons-round" style="font-size:14px;">error</span>Passwords do not match</div>
              }
            </div>

            <button type="submit" class="btn btn-primary btn-w-full btn-lg" [disabled]="loading()">
              @if (loading()) { <div class="spinner spinner-sm"></div> }
              @else { <span class="material-icons-round">person_add</span> }
              Create Account
            </button>
          </form>
        </div>
      </div>
    </div>
  `,
  styles: [`.hide-mobile { @media(max-width:768px){ display:none!important; } }`]
})
export class RegisterComponent {
  private fb       = inject(FormBuilder);
  private auth     = inject(AuthService);
  private router   = inject(Router);
  private toast    = inject(ToastService);
  private reminder = inject(ReminderNotificationService);

  loading = signal(false);
  showPw  = signal(false);

  benefits = [
    'Register for any approved event',
    'Secure online payments with refund support',
    'Set smart reminders for your events',
    'Personal to-do list & task tracker',
    'Request Organizer role to host events',
  ];

  form = this.fb.group({
    username:        ['', [Validators.required, Validators.minLength(3)]],
    email:           ['', [Validators.required, strictEmail()]],
    password:        ['', [Validators.required, minPassword()]],
    confirmPassword: ['', Validators.required],
  }, { validators: passwordsMatch });

  isInvalid(f: string) { const c = this.form.get(f); return c?.invalid && c?.touched; }
  getError(f: string): string {
    const e = this.form.get(f)?.errors;
    if (!e) return '';
    if (e['required'])      return 'This field is required';
    if (e['minlength'])     return `Minimum ${e['minlength'].requiredLength} characters required`;
    if (e['invalidEmail'])  return e['invalidEmail'].message;
    if (e['weakPassword'])  return e['weakPassword'].message;
    return 'Invalid value';
  }

  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading.set(true);
    const { username, email, password } = this.form.value;
    this.auth.register({ username: username!, email: email!, password: password! }).subscribe({
      next: () => {
        this.toast.success('Your account has been created successfully!', 'Welcome 🎉');
        this.reminder.start();
        this.auth.redirectByRole();
      },
      error: () => this.loading.set(false)
    });
  }
}
