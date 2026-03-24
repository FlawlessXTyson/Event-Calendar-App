import { TestBed, ComponentFixture } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { UserProfileComponent } from '../profile/user-profile.component';
import { UserService } from '../../../core/services/user.service';
import { ToastService } from '../../../core/services/toast.service';
import { UserRole, AccountStatus } from '../../../core/models/models';
import { of, throwError } from 'rxjs';

const mockUser = {
  userId: 42, name: 'Tyson', email: 'tyson@gmail.com',
  role: UserRole.USER, status: AccountStatus.ACTIVE,
  createdAt: '2026-03-20T00:00:00Z'
};

describe('UserProfileComponent', () => {
  let fixture: ComponentFixture<UserProfileComponent>;
  let component: UserProfileComponent;
  let userSvc: UserService;
  let toastSvc: ToastService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UserProfileComponent, HttpClientTestingModule], providers: [provideRouter([])]
    }).compileComponents();
    fixture   = TestBed.createComponent(UserProfileComponent);
    component = fixture.componentInstance;
    userSvc   = TestBed.inject(UserService);
    toastSvc  = TestBed.inject(ToastService);
    spyOn(userSvc, 'getMe').and.returnValue(of(mockUser));
    fixture.detectChanges();
  });

  // ── Init ──────────────────────────────────────────────────────────────────
  it('creates the component', () => {
    expect(component).toBeTruthy();
  });

  it('calls getMe() on init', () => {
    expect(userSvc.getMe).toHaveBeenCalled();
  });

  it('sets user signal on load', () => {
    expect(component.user()?.name).toBe('Tyson');
    expect(component.user()?.email).toBe('tyson@gmail.com');
  });

  it('patches form with name on load', () => {
    expect(component.form.get('name')!.value).toBe('Tyson');
  });

  it('sets loading to false after load', () => {
    expect(component.loading()).toBeFalse();
  });

  it('sets loading to false even on error', async () => {
    (userSvc.getMe as jasmine.Spy).and.returnValue(throwError(() => new Error('fail')));
    component.ngOnInit();
    expect(component.loading()).toBeFalse();
  });

  // ── Form ──────────────────────────────────────────────────────────────────
  it('form has only name field (no email field)', () => {
    expect(component.form.contains('name')).toBeTrue();
    expect(component.form.contains('email')).toBeFalse();
  });

  it('name field is required', () => {
    component.form.get('name')!.setValue('');
    component.form.get('name')!.markAsTouched();
    expect(component.fi('name')).toBeTrue();
  });

  it('form is valid when name is present', () => {
    component.form.get('name')!.setValue('Tyson');
    expect(component.form.valid).toBeTrue();
  });

  // ── save() ────────────────────────────────────────────────────────────────
  it('save() marks all as touched when invalid', () => {
    component.form.get('name')!.setValue('');
    component.save();
    expect(component.form.get('name')!.touched).toBeTrue();
  });

  it('save() does not call updateMe when name is empty', () => {
    const spy = spyOn(userSvc, 'updateMe');
    component.form.get('name')!.setValue('');
    component.save();
    expect(spy).not.toHaveBeenCalled();
  });

  it('save() calls updateMe with only name (no email)', () => {
    const updated = { ...mockUser, name: 'Tyson Updated' };
    const spy = spyOn(userSvc, 'updateMe').and.returnValue(of(updated));
    spyOn(toastSvc, 'success');
    component.form.get('name')!.setValue('Tyson Updated');
    component.save();
    expect(spy).toHaveBeenCalledWith({ name: 'Tyson Updated' });
  });

  it('save() does NOT pass email in payload (email is locked)', () => {
    const updated = { ...mockUser, name: 'New Name' };
    const spy = spyOn(userSvc, 'updateMe').and.returnValue(of(updated));
    spyOn(toastSvc, 'success');
    component.form.get('name')!.setValue('New Name');
    component.save();
    const payload = spy.calls.mostRecent().args[0];
    expect((payload as any).email).toBeUndefined();
  });

  it('save() updates user signal on success', () => {
    const updated = { ...mockUser, name: 'Updated Name' };
    spyOn(userSvc, 'updateMe').and.returnValue(of(updated));
    spyOn(toastSvc, 'success');
    component.form.get('name')!.setValue('Updated Name');
    component.save();
    expect(component.user()?.name).toBe('Updated Name');
  });

  it('save() shows success toast on update', () => {
    spyOn(userSvc, 'updateMe').and.returnValue(of(mockUser));
    const toastSpy = spyOn(toastSvc, 'success');
    component.form.get('name')!.setValue('Tyson');
    component.save();
    expect(toastSpy).toHaveBeenCalledWith(
      jasmine.stringContaining('successfully'), 'Saved'
    );
  });

  it('save() resets saving to false on error', () => {
    spyOn(userSvc, 'updateMe').and.returnValue(throwError(() => new Error('fail')));
    component.form.get('name')!.setValue('Tyson');
    component.save();
    expect(component.saving()).toBeFalse();
  });

  // ── roleLabel() ───────────────────────────────────────────────────────────
  it('roleLabel(1) returns User', () => {
    expect(component.roleLabel(1)).toBe('User');
  });

  it('roleLabel(2) returns Organizer', () => {
    expect(component.roleLabel(2)).toBe('Organizer');
  });

  it('roleLabel(3) returns Admin', () => {
    expect(component.roleLabel(3)).toBe('Admin');
  });

  it('roleLabel(99) returns Unknown', () => {
    expect(component.roleLabel(99)).toBe('Unknown');
  });
});
