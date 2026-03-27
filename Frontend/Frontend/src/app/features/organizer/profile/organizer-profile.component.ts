import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { UserService } from '../../../core/services/user.service';
import { ToastService } from '../../../core/services/toast.service';
import { UserDto } from '../../../core/models/models';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-organizer-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div style="max-width:600px;">
      <div style="margin-bottom:24px;"><h1 style="font-size:1.5rem;">My Profile</h1><p>Update your organizer account</p></div>
      @if (loading()) { <div class="loading-center"><div class="spinner"></div></div> }
      @else if (user()) {
        <div class="card card-body" style="margin-bottom:20px;text-align:center;">
          <div style="position:relative;width:96px;margin:0 auto 14px;">
            @if (previewUrl() || user()!.profileImageUrl) {
              <img [src]="previewUrl() || apiBase + user()!.profileImageUrl"
                style="width:96px;height:96px;border-radius:50%;object-fit:cover;border:3px solid var(--warning);" />
            } @else {
              <div style="width:96px;height:96px;border-radius:50%;background:var(--warning);color:#fff;display:flex;align-items:center;justify-content:center;font-size:2.5rem;font-weight:700;">
                {{ user()!.name[0].toUpperCase() }}
              </div>
            }
            <label style="position:absolute;bottom:0;right:0;width:30px;height:30px;background:var(--warning);border-radius:50%;display:flex;align-items:center;justify-content:center;cursor:pointer;border:2px solid #fff;box-shadow:0 2px 6px rgba(0,0,0,.2);" title="Change photo">
              <span class="material-icons-round" style="font-size:16px;color:#fff;">photo_camera</span>
              <input type="file" accept="image/jpeg,image/png,image/webp" style="display:none;" (change)="onFileSelected($event)" />
            </label>
          </div>
          @if (selectedFile()) {
            <div style="display:flex;gap:8px;justify-content:center;margin-bottom:8px;">
              <button type="button" class="btn btn-primary btn-sm" [disabled]="uploading()" (click)="uploadImage()">
                @if (uploading()) { <div class="spinner spinner-sm"></div> } @else { <span class="material-icons-round">upload</span> }
                Upload Photo
              </button>
              <button type="button" class="btn btn-ghost btn-sm" (click)="cancelUpload()">Cancel</button>
            </div>
          }
          <div style="font-size:1.1rem;font-weight:700;">{{ user()!.name }}</div>
          <div style="color:var(--text-muted);font-size:.9rem;">{{ user()!.email }}</div>
          <div style="margin-top:10px;"><span class="badge badge-warning">ORGANIZER</span></div>
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
                Email cannot be changed.
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
export class OrganizerProfileComponent implements OnInit {
  private userSvc = inject(UserService);
  private toast   = inject(ToastService);
  private fb      = inject(FormBuilder);

  user         = signal<UserDto | null>(null);
  loading      = signal(true);
  saving       = signal(false);
  uploading    = signal(false);
  selectedFile = signal<File | null>(null);
  previewUrl   = signal<string | null>(null);
  readonly apiBase = environment.apiUrl.replace('/api', '');

  form = this.fb.group({ name: ['', Validators.required] });
  fi(f: string) { const c = this.form.get(f); return c?.invalid && c?.touched; }

  ngOnInit() {
    this.userSvc.getMe().subscribe({
      next: u => { this.user.set(u); this.form.patchValue({ name: u.name }); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  onFileSelected(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.selectedFile.set(file);
    const reader = new FileReader();
    reader.onload = e => this.previewUrl.set(e.target?.result as string);
    reader.readAsDataURL(file);
  }

  uploadImage() {
    const file = this.selectedFile();
    if (!file) return;
    this.uploading.set(true);
    this.userSvc.uploadProfileImage(file).subscribe({
      next: u => { this.user.set(u); this.previewUrl.set(null); this.selectedFile.set(null); this.toast.success('Profile photo updated!', 'Photo Saved'); this.uploading.set(false); },
      error: () => this.uploading.set(false)
    });
  }

  cancelUpload() { this.selectedFile.set(null); this.previewUrl.set(null); }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    this.userSvc.updateMe({ name: this.form.value.name! }).subscribe({
      next: u => { this.user.set(u); this.toast.success('Profile updated!', 'Saved'); this.saving.set(false); },
      error: () => this.saving.set(false)
    });
  }
}
