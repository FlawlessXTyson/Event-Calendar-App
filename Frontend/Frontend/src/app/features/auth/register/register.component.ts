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
  templateUrl: './register.component.html',
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
