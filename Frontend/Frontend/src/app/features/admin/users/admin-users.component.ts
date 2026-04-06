import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { UserService } from '../../../core/services/user.service';
import { ToastService } from '../../../core/services/toast.service';
import { AuthService } from '../../../core/services/auth.service';
import { UserDto, UserRole, AccountStatus } from '../../../core/models/models';
import { strictEmail, minPassword } from '../../../core/validators/custom.validators';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.css'
})
export class AdminUsersComponent implements OnInit {
  private userSvc = inject(UserService);
  private toast   = inject(ToastService);
  auth            = inject(AuthService);
  private fb      = inject(FormBuilder);
  UserRole        = UserRole;
  AccountStatus   = AccountStatus;

  users        = signal<UserDto[]>([]);
  loading      = signal(true);
  showCreate   = signal(false);
  creatingUser = signal(false);
  deleting     = signal<number | null>(null);
  toggling     = signal<number | null>(null);
  search       = signal('');
  filterRole   = signal<'' | UserRole>('');

  filtered = computed(() => {
    const s = this.search().toLowerCase();
    const role = this.filterRole();
    return this.users().filter(u => {
      const matchSearch = !s || u.name.toLowerCase().includes(s) || u.email.toLowerCase().includes(s);
      const matchRole   = !role || u.role === +role;
      return matchSearch && matchRole;
    });
  });

  createForm = this.fb.group({
    name:     ['', Validators.required],
    email:    ['', [Validators.required, strictEmail()]],
    password: ['', [Validators.required, minPassword()]],
    role:     [UserRole.USER]
  });

  cfi(f: string) { const c = this.createForm.get(f); return c?.invalid && c?.touched; }

  ngOnInit() {
    this.userSvc.getAll().subscribe({ next: us => { this.users.set(us); this.loading.set(false); }, error: () => this.loading.set(false) });
  }

  createUser() {
    if (this.createForm.invalid) { this.createForm.markAllAsTouched(); return; }
    this.creatingUser.set(true);
    const v = this.createForm.value;
    this.userSvc.create({ name: v.name!, email: v.email!, password: v.password!, role: +v.role! as UserRole }).subscribe({
      next: u => {
        this.users.update(us => [u, ...us]);
        this.toast.success(`User "${u.name}" created with role ${this.roleLabel(u.role)}.`, 'User Created');
        this.createForm.reset({ role: UserRole.USER });
        this.showCreate.set(false);
        this.creatingUser.set(false);
      },
      error: () => this.creatingUser.set(false)
    });
  }

  deleteUser(u: UserDto) {
    if (!confirm(`Delete user "${u.name}" (${u.email})?\n\nThis action cannot be undone.`)) return;
    this.deleting.set(u.userId);
    this.userSvc.delete(u.userId).subscribe({
      next: () => { this.users.update(us => us.filter(x => x.userId !== u.userId)); this.toast.success(`User "${u.name}" deleted.`, 'User Deleted'); this.deleting.set(null); },
      error: () => this.deleting.set(null)
    });
  }

  toggleStatus(u: UserDto) {
    if (u.userId === this.auth.userId()) { this.toast.error('You cannot disable your own account.', 'Not Allowed'); return; }
    const isActive = u.status === AccountStatus.ACTIVE;
    if (!confirm(`${isActive ? 'Disable' : 'Enable'} user "${u.name}"?`)) return;
    this.toggling.set(u.userId);
    const call = isActive ? this.userSvc.disable(u.userId) : this.userSvc.enable(u.userId);
    call.subscribe({
      next: updated => {
        this.users.update(us => us.map(x => x.userId === updated.userId ? updated : x));
        this.toast.success(`User "${updated.name}" has been ${isActive ? 'disabled' : 'enabled'}.`, isActive ? 'User Disabled' : 'User Enabled');
        this.toggling.set(null);
      },
      error: () => this.toggling.set(null)
    });
  }

  roleLabel(r: UserRole)  { return { 1: 'User', 2: 'Organizer', 3: 'Admin' }[r] ?? r; }
  roleBadge(r: UserRole)  { return { 1: 'badge-primary', 2: 'badge-warning', 3: 'badge-danger' }[r] ?? 'badge-gray'; }
}
