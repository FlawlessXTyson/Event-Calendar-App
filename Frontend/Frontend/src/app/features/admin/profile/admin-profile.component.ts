import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { UserService } from '../../../core/services/user.service';
import { ToastService } from '../../../core/services/toast.service';
import { UserDto } from '../../../core/models/models';

@Component({
  selector: 'app-admin-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div style="max-width:600px;">
      <div style="margin-bottom:24px;"><h1 style="font-size:1.5rem;">Admin Profile</h1><p>Manage your administrator account</p></div>
      @if (loading()) { <div class="loading-center"><div class="spinner"></div></div> }
      @else if (user()) {
        <div class="card card-body" style="margin-bottom:20px;text-align:center;">
          <div style="width:72px;height:72px;border-radius:50%;background:var(--danger);color:#fff;display:flex;align-items:center;justify-content:center;font-size:2rem;font-weight:700;margin:0 auto 14px;">{{ user()!.name[0].toUpperCase() }}</div>
          <div style="font-size:1.1rem;font-weight:700;">{{ user()!.name }}</div>
          <div style="color:var(--text-muted);font-size:.9rem;">{{ user()!.email }}</div>
          <div style="margin-top:10px;"><span class="badge badge-danger">ADMINISTRATOR</span></div>
          <div style="font-size:.8rem;color:var(--text-muted);margin-top:8px;">Member since {{ user()!.createdAt | date:'MMMM y' }}</div>
        </div>
        <div class="card card-body">
          <h3 style="margin-bottom:20px;">Edit Profile</h3>
          <form [formGroup]="form" (ngSubmit)="save()">
            <div class="form-group">
              <label class="form-label">Full Name <span style="color:var(--danger)">*</span></label>
              <input formControlName="name" type="text" class="form-control" [class.is-invalid]="fi('name')" />
              @if (fi('name')) { <div class="form-error"><span class="material-icons-round" style="font-size:14px;">error</span>Name is required</div> }
            </div>
            <div class="form-group">
              <label class="form-label">Email Address</label>
              <input type="email" class="form-control" [value]="user()!.email" readonly
                style="background:var(--surface-2);cursor:not-allowed;color:var(--text-muted);" />
              <div class="form-hint" style="display:flex;align-items:center;gap:4px;">
                <span class="material-icons-round" style="font-size:14px;color:var(--warning);">lock</span>
                Email cannot be changed. To use a different email, please create a new account.
              </div>
            </div>
            <button type="submit" class="btn btn-primary" [disabled]="saving()">
              @if (saving()) { <div class="spinner spinner-sm"></div> } @else { <span class="material-icons-round">save</span> }
              Save Changes
            </button>
          </form>
        </div>
      }
    </div>
  `
})
export class AdminProfileComponent implements OnInit {
  private userSvc = inject(UserService);
  private toast   = inject(ToastService);
  private fb      = inject(FormBuilder);
  user    = signal<UserDto | null>(null);
  loading = signal(true);
  saving  = signal(false);
  form    = this.fb.group({ name: ['', Validators.required] });
  fi(f: string) { const c = this.form.get(f); return c?.invalid && c?.touched; }
  ngOnInit() {
    this.userSvc.getMe().subscribe({ next: u => { this.user.set(u); this.form.patchValue({ name: u.name }); this.loading.set(false); }, error: () => this.loading.set(false) });
  }
  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    const v = this.form.value;
    this.userSvc.updateMe({ name: v.name! }).subscribe({
      next: u => { this.user.set(u); this.toast.success('Profile updated!', 'Saved'); this.saving.set(false); },
      error: () => this.saving.set(false)
    });
  }
}
