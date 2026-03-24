import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { UserService } from '../../../core/services/user.service';
import { ToastService } from '../../../core/services/toast.service';
import { UserDto, UserRole, AccountStatus } from '../../../core/models/models';
import { strictEmail, minPassword } from '../../../core/validators/custom.validators';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  template: `
    <div>
      <div class="section-header" style="margin-bottom:24px;">
        <div><h1 style="font-size:1.5rem;">User Management</h1><p>{{ users().length }} registered users</p></div>
        <button type="button" class="btn btn-primary btn-sm" (click)="showCreate.set(!showCreate())">
          <span class="material-icons-round">{{ showCreate() ? 'close' : 'person_add' }}</span>
          {{ showCreate() ? 'Cancel' : 'Create User' }}
        </button>
      </div>

      <!-- Create User Form -->
      @if (showCreate()) {
        <div class="card card-body" style="margin-bottom:24px;">
          <h3 style="margin-bottom:16px;">Create New User</h3>
          <form [formGroup]="createForm" (ngSubmit)="createUser()">
            <div class="form-row">
              <div class="form-group">
                <label class="form-label">Full Name <span style="color:var(--danger)">*</span></label>
                <input formControlName="name" type="text" class="form-control" [class.is-invalid]="cfi('name')" placeholder="John Doe" />
                @if (cfi('name')) { <div class="form-error"><span class="material-icons-round" style="font-size:14px;">error</span>Name is required</div> }
              </div>
              <div class="form-group">
                <label class="form-label">Email <span style="color:var(--danger)">*</span></label>
                <input formControlName="email" type="email" class="form-control" [class.is-invalid]="cfi('email')" placeholder="user@gmail.com" />
                @if (cfi('email')) { <div class="form-error"><span class="material-icons-round" style="font-size:14px;">error</span>Valid email required (e.g. name&#64;domain.com)</div> }
              </div>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label class="form-label">Password <span style="color:var(--danger)">*</span></label>
                <input formControlName="password" type="password" class="form-control" [class.is-invalid]="cfi('password')" placeholder="Min. 6 characters" />
                @if (cfi('password')) { <div class="form-error"><span class="material-icons-round" style="font-size:14px;">error</span>Minimum 6 characters</div> }
              </div>
              <div class="form-group">
                <label class="form-label">Role</label>
                <select formControlName="role" class="form-control">
                  <option [value]="UserRole.USER">User</option>
                  <option [value]="UserRole.ORGANIZER">Organizer</option>
                  <option [value]="UserRole.ADMIN">Admin</option>
                </select>
              </div>
            </div>
            <button type="submit" class="btn btn-primary btn-sm" [disabled]="creatingUser()">
              @if (creatingUser()) { <div class="spinner spinner-sm"></div> } @else { <span class="material-icons-round">person_add</span> }
              Create User
            </button>
          </form>
        </div>
      }

      <!-- Search -->
      <div style="margin-bottom:16px;display:flex;gap:12px;flex-wrap:wrap;">
        <div style="position:relative;flex:1;min-width:200px;">
          <span class="material-icons-round" style="position:absolute;left:10px;top:50%;transform:translateY(-50%);color:var(--text-muted);font-size:20px;">search</span>
          <input [ngModel]="search()" (ngModelChange)="search.set($event)" type="search" class="form-control" placeholder="Search by name or email…" style="padding-left:38px;" />
        </div>
        <select [ngModel]="filterRole()" (ngModelChange)="filterRole.set($event)" class="form-control" style="width:auto;min-width:140px;">
          <option value="">All roles</option>
          <option [value]="UserRole.USER">User</option>
          <option [value]="UserRole.ORGANIZER">Organizer</option>
          <option [value]="UserRole.ADMIN">Admin</option>
        </select>
      </div>

      @if (loading()) { <div class="loading-center"><div class="spinner"></div></div> }
      @else {
        <div class="table-wrapper">
          <table>
            <thead><tr><th>#</th><th>Name</th><th>Email</th><th>Role</th><th>Status</th><th>Joined</th><th>Actions</th></tr></thead>
            <tbody>
              @for (u of filtered(); track u.userId) {
                <tr>
                  <td style="color:var(--text-muted);">#{{ u.userId }}</td>
                  <td style="font-weight:600;">{{ u.name }}</td>
                  <td style="color:var(--text-muted);">{{ u.email }}</td>
                  <td><span class="badge" [class]="roleBadge(u.role)">{{ roleLabel(u.role) }}</span></td>
                  <td><span class="badge" [class]="u.status === AccountStatus.ACTIVE ? 'badge-success' : 'badge-danger'">{{ u.status === AccountStatus.ACTIVE ? 'Active' : 'Blocked' }}</span></td>
                  <td style="color:var(--text-muted);">{{ u.createdAt | date:'MMM d, y' }}</td>
                  <td>
                    <button type="button" class="btn btn-ghost btn-sm btn-icon" [disabled]="deleting() === u.userId" (click)="deleteUser(u)" title="Delete user">
                      @if (deleting() === u.userId) { <div class="spinner spinner-sm"></div> }
                      @else { <span class="material-icons-round" style="color:var(--danger);font-size:18px;">delete</span> }
                    </button>
                  </td>
                </tr>
              }
              @if (filtered().length === 0) {
                <tr><td colspan="7" style="text-align:center;padding:32px;color:var(--text-muted);">No users found.</td></tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `
})
export class AdminUsersComponent implements OnInit {
  private userSvc = inject(UserService);
  private toast   = inject(ToastService);
  private fb      = inject(FormBuilder);
  UserRole       = UserRole;
  AccountStatus  = AccountStatus;

  users       = signal<UserDto[]>([]);
  loading     = signal(true);
  showCreate  = signal(false);
  creatingUser= signal(false);
  deleting    = signal<number | null>(null);
  search      = signal('');
  filterRole  = signal<'' | UserRole>('');

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

  roleLabel(r: UserRole)  { return { 1: 'User', 2: 'Organizer', 3: 'Admin' }[r] ?? r; }
  roleBadge(r: UserRole)  { return { 1: 'badge-primary', 2: 'badge-warning', 3: 'badge-danger' }[r] ?? 'badge-gray'; }
}
