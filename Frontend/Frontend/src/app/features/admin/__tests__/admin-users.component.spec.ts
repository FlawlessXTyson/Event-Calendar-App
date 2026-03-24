import { TestBed, ComponentFixture } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AdminUsersComponent } from '../users/admin-users.component';
import { UserService } from '../../../core/services/user.service';
import { ToastService } from '../../../core/services/toast.service';
import { UserRole, AccountStatus } from '../../../core/models/models';
import { of, throwError } from 'rxjs';

const u = (id: number, name: string, role: UserRole) => ({
  userId: id, name, email: `${name.toLowerCase()}@gmail.com`,
  role, status: AccountStatus.ACTIVE, createdAt: '2026-03-20T00:00:00Z'
});

describe('AdminUsersComponent', () => {
  let fixture: ComponentFixture<AdminUsersComponent>;
  let component: AdminUsersComponent;
  let userSvc: UserService;
  let toastSvc: ToastService;

  const mockUsers = [
    u(1, 'Admin', UserRole.ADMIN),
    u(2, 'Tyson', UserRole.USER),
    u(3, 'Divesha', UserRole.USER),
    u(4, 'organizer1', UserRole.ORGANIZER),
    u(5, 'organizer2', UserRole.ORGANIZER),
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminUsersComponent, HttpClientTestingModule], providers: [provideRouter([])]
    }).compileComponents();
    fixture   = TestBed.createComponent(AdminUsersComponent);
    component = fixture.componentInstance;
    userSvc   = TestBed.inject(UserService);
    toastSvc  = TestBed.inject(ToastService);
    spyOn(userSvc, 'getAll').and.returnValue(of(mockUsers));
    fixture.detectChanges();
  });

  // ── Init ──────────────────────────────────────────────────────────────────
  it('creates the component', () => {
    expect(component).toBeTruthy();
  });

  it('loads users on init', () => {
    expect(userSvc.getAll).toHaveBeenCalled();
    expect(component.users().length).toBe(5);
  });

  it('sets loading to false after load', () => {
    expect(component.loading()).toBeFalse();
  });

  // ── Filter by role (THE KEY BUG FIX — uses signals) ──────────────────────
  it('filtered() returns all users when filterRole is empty', () => {
    component.filterRole.set('');
    expect(component.filtered().length).toBe(5);
  });

  it('filtered() returns only USERs when filterRole=USER', () => {
    component.filterRole.set(UserRole.USER);
    const result = component.filtered();
    expect(result.length).toBe(2);
    expect(result.every(u => u.role === UserRole.USER)).toBeTrue();
  });

  it('filtered() returns only ORGANIZERs when filterRole=ORGANIZER', () => {
    component.filterRole.set(UserRole.ORGANIZER);
    const result = component.filtered();
    expect(result.length).toBe(2);
    expect(result.every(u => u.role === UserRole.ORGANIZER)).toBeTrue();
  });

  it('filtered() returns only ADMINs when filterRole=ADMIN', () => {
    component.filterRole.set(UserRole.ADMIN);
    const result = component.filtered();
    expect(result.length).toBe(1);
    expect(result[0].name).toBe('Admin');
  });

  // ── Search filter ─────────────────────────────────────────────────────────
  it('filtered() filters by name (case-insensitive)', () => {
    component.search.set('tyson');
    const result = component.filtered();
    expect(result.length).toBe(1);
    expect(result[0].name).toBe('Tyson');
  });

  it('filtered() filters by email', () => {
    component.search.set('organizer1');
    const result = component.filtered();
    expect(result.length).toBe(1);
    expect(result[0].name).toBe('organizer1');
  });

  it('filtered() returns empty when no match', () => {
    component.search.set('nonexistentperson');
    expect(component.filtered().length).toBe(0);
  });

  // ── Combined filter + search ──────────────────────────────────────────────
  it('combined search + role filter works correctly', () => {
    component.filterRole.set(UserRole.ORGANIZER);
    component.search.set('organizer1');
    const result = component.filtered();
    expect(result.length).toBe(1);
    expect(result[0].name).toBe('organizer1');
  });

  it('combined filter returns empty when role does not match search', () => {
    component.filterRole.set(UserRole.ADMIN);
    component.search.set('tyson'); // Tyson is USER not ADMIN
    expect(component.filtered().length).toBe(0);
  });

  // ── roleLabel / roleBadge ─────────────────────────────────────────────────
  it('roleLabel returns correct strings', () => {
    expect(component.roleLabel(UserRole.USER)).toBe('User');
    expect(component.roleLabel(UserRole.ORGANIZER)).toBe('Organizer');
    expect(component.roleLabel(UserRole.ADMIN)).toBe('Admin');
  });

  it('roleBadge returns correct CSS classes', () => {
    expect(component.roleBadge(UserRole.USER)).toBe('badge-primary');
    expect(component.roleBadge(UserRole.ORGANIZER)).toBe('badge-warning');
    expect(component.roleBadge(UserRole.ADMIN)).toBe('badge-danger');
  });

  // ── createUser() form validation ──────────────────────────────────────────
  it('createUser() marks form as touched when invalid', () => {
    component.createUser();
    expect(component.createForm.get('name')!.touched).toBeTrue();
  });

  it('createUser() does not call service when form invalid', () => {
    const spy = spyOn(userSvc, 'create');
    component.createUser();
    expect(spy).not.toHaveBeenCalled();
  });

  it('createUser() calls userSvc.create with correct payload', () => {
    const newUser = u(6, 'NewGuy', UserRole.USER);
    const spy = spyOn(userSvc, 'create').and.returnValue(of(newUser));
    spyOn(toastSvc, 'success');
    component.createForm.patchValue({
      name: 'NewGuy', email: 'newguy@gmail.com',
      password: 'pass123', role: UserRole.USER
    });
    component.createUser();
    expect(spy).toHaveBeenCalledWith({
      name: 'NewGuy', email: 'newguy@gmail.com',
      password: 'pass123', role: UserRole.USER
    });
  });

  it('createUser() prepends new user to list', () => {
    const newUser = u(6, 'NewGuy', UserRole.USER);
    spyOn(userSvc, 'create').and.returnValue(of(newUser));
    spyOn(toastSvc, 'success');
    component.createForm.patchValue({
      name: 'NewGuy', email: 'newguy@gmail.com',
      password: 'pass123', role: UserRole.USER
    });
    component.createUser();
    expect(component.users()[0].name).toBe('NewGuy');
    expect(component.users().length).toBe(6);
  });

  it('createUser() hides form on success', () => {
    const newUser = u(6, 'NewGuy', UserRole.USER);
    spyOn(userSvc, 'create').and.returnValue(of(newUser));
    spyOn(toastSvc, 'success');
    component.showCreate.set(true);
    component.createForm.patchValue({
      name: 'NewGuy', email: 'newguy@gmail.com',
      password: 'pass123', role: UserRole.USER
    });
    component.createUser();
    expect(component.showCreate()).toBeFalse();
  });

  it('createForm email validates format', () => {
    component.createForm.get('email')!.setValue('notvalid');
    component.createForm.get('email')!.markAsTouched();
    expect(component.cfi('email')).toBeTrue();
  });

  it('createForm password validates minimum length', () => {
    component.createForm.get('password')!.setValue('abc');
    component.createForm.get('password')!.markAsTouched();
    expect(component.cfi('password')).toBeTrue();
  });

  // ── deleteUser() ──────────────────────────────────────────────────────────
  it('deleteUser() calls userSvc.delete and removes user from list', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    spyOn(userSvc, 'delete').and.returnValue(of(undefined));
    spyOn(toastSvc, 'success');
    component.deleteUser(mockUsers[1]); // Tyson (userId=2)
    expect(userSvc.delete).toHaveBeenCalledWith(2);
    expect(component.users().find(u => u.userId === 2)).toBeUndefined();
    expect(component.users().length).toBe(4);
  });

  it('deleteUser() does NOT call service when confirm cancelled', () => {
    spyOn(window, 'confirm').and.returnValue(false);
    const spy = spyOn(userSvc, 'delete');
    component.deleteUser(mockUsers[0]);
    expect(spy).not.toHaveBeenCalled();
  });

  it('deleteUser() resets deleting signal on error', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    spyOn(userSvc, 'delete').and.returnValue(throwError(() => new Error('fail')));
    component.deleteUser(mockUsers[0]);
    expect(component.deleting()).toBeNull();
  });
});
