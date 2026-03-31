import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { UserService } from '../../../core/services/user.service';
import { ToastService } from '../../../core/services/toast.service';
import { AuthService } from '../../../core/services/auth.service';
import { UserDto } from '../../../core/models/models';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './user-profile.component.html',
  styleUrl: './user-profile.component.css'
})
export class UserProfileComponent implements OnInit {
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
      next: u => {
        this.user.set(u);
        this.previewUrl.set(null);
        this.selectedFile.set(null);
        this.toast.success('Profile photo updated!', 'Photo Saved');
        this.uploading.set(false);
      },
      error: () => this.uploading.set(false)
    });
  }

  cancelUpload() {
    this.selectedFile.set(null);
    this.previewUrl.set(null);
  }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    this.userSvc.updateMe({ name: this.form.value.name! }).subscribe({
      next: u => { this.user.set(u); this.toast.success('Profile updated successfully!', 'Saved'); this.saving.set(false); },
      error: () => this.saving.set(false)
    });
  }

  roleLabel(r: number) { return { 1: 'User', 2: 'Organizer', 3: 'Admin' }[r] ?? 'Unknown'; }
}
