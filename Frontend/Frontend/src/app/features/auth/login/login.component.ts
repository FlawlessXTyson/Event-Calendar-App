import { Component, inject, signal } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule, AbstractControl } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { ReminderNotificationService } from '../../../core/services/reminder-notification.service';
import { strictEmail } from '../../../core/validators/custom.validators';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styles: [`.hide-mobile { @media(max-width:768px){ display:none!important; } }`]
})
export class LoginComponent {
  private fb       = inject(FormBuilder);
  private auth     = inject(AuthService);
  private router   = inject(Router);
  private toast    = inject(ToastService);
  private reminder = inject(ReminderNotificationService);

  loading = signal(false);
  showPw  = signal(false);

  features = [
    { icon: 'event_available', text: 'Register for upcoming events' },
    { icon: 'payment',         text: 'Secure payment processing' },
    { icon: 'notifications',   text: 'Smart event reminders' },
  ];

  form = this.fb.group({
    email:    ['', [Validators.required, strictEmail()]],
    password: ['', Validators.required]
  });

  isInvalid(f: string) { const c = this.form.get(f); return c?.invalid && c?.touched; }
  getError(f: string): string {
    const e = this.form.get(f)?.errors;
    if (!e) return '';
    if (e['required'])      return 'This field is required';
    if (e['invalidEmail'])  return e['invalidEmail'].message;
    return 'Invalid value';
  }

  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading.set(true);
    const { email, password } = this.form.value;
    this.auth.login({ email: email!, password: password! }).subscribe({
      next: () => {
        this.toast.success(`Welcome back, ${this.auth.userName()}!`, 'Signed In');
        this.reminder.start();
        this.auth.redirectByRole();
      },
      error: (err) => {
        this.loading.set(false);
        const status: number = err?.status;
        const msg: string = (err?.error?.message ?? '').toLowerCase();
        if (status === 401 || status === 400) {
          if (msg.includes('disabled') || msg.includes('not active')) {
            this.toast.error('Your account has been disabled. Please contact support.', 'Account Disabled');
          } else {
            this.toast.error('Invalid email or password. Please try again.', 'Sign In Failed');
          }
        }
      }
    });
  }
}
