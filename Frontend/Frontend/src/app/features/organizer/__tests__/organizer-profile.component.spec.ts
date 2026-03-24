import { TestBed, ComponentFixture } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { OrganizerProfileComponent } from '../profile/organizer-profile.component';
import { UserService } from '../../../core/services/user.service';
import { ToastService } from '../../../core/services/toast.service';
import { UserRole, AccountStatus } from '../../../core/models/models';
import { of, throwError } from 'rxjs';

const mockOrganizer = {
  userId: 7, name: 'organizer1', email: 'org1@gmail.com',
  role: UserRole.ORGANIZER, status: AccountStatus.ACTIVE,
  createdAt: '2026-03-21T00:00:00Z'
};

describe('OrganizerProfileComponent', () => {
  let fixture: ComponentFixture<OrganizerProfileComponent>;
  let component: OrganizerProfileComponent;
  let userSvc: UserService;
  let toastSvc: ToastService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OrganizerProfileComponent, HttpClientTestingModule], providers: [provideRouter([])]
    }).compileComponents();
    fixture   = TestBed.createComponent(OrganizerProfileComponent);
    component = fixture.componentInstance;
    userSvc   = TestBed.inject(UserService);
    toastSvc  = TestBed.inject(ToastService);
    spyOn(userSvc, 'getMe').and.returnValue(of(mockOrganizer));
    fixture.detectChanges();
  });

  it('creates the component', () => expect(component).toBeTruthy());

  it('loads user on init', () => {
    expect(component.user()?.name).toBe('organizer1');
    expect(component.user()?.email).toBe('org1@gmail.com');
  });

  it('form has ONLY name field — email is locked', () => {
    expect(component.form.contains('name')).toBeTrue();
    expect(component.form.contains('email')).toBeFalse();
  });

  it('form patches name from loaded user', () => {
    expect(component.form.get('name')!.value).toBe('organizer1');
  });

  it('name field is required', () => {
    component.form.get('name')!.setValue('');
    component.form.get('name')!.markAsTouched();
    expect(component.fi('name')).toBeTrue();
  });

  it('save() does not call updateMe when name is empty', () => {
    const spy = spyOn(userSvc, 'updateMe');
    component.form.get('name')!.setValue('');
    component.save();
    expect(spy).not.toHaveBeenCalled();
  });

  it('save() calls updateMe with only name — no email in payload', () => {
    const updated = { ...mockOrganizer, name: 'OrgUpdated' };
    const spy = spyOn(userSvc, 'updateMe').and.returnValue(of(updated));
    spyOn(toastSvc, 'success');
    component.form.get('name')!.setValue('OrgUpdated');
    component.save();
    expect(spy).toHaveBeenCalledWith({ name: 'OrgUpdated' });
    const payload = spy.calls.mostRecent().args[0];
    expect((payload as any).email).toBeUndefined();
  });

  it('save() shows success toast', () => {
    spyOn(userSvc, 'updateMe').and.returnValue(of(mockOrganizer));
    const toastSpy = spyOn(toastSvc, 'success');
    component.form.get('name')!.setValue('organizer1');
    component.save();
    expect(toastSpy).toHaveBeenCalledWith('Profile updated!', 'Saved');
  });

  it('save() resets saving to false on error', () => {
    spyOn(userSvc, 'updateMe').and.returnValue(throwError(() => new Error('fail')));
    component.form.get('name')!.setValue('organizer1');
    component.save();
    expect(component.saving()).toBeFalse();
  });

  it('sets loading to false after load error', () => {
    (userSvc.getMe as jasmine.Spy).and.returnValue(throwError(() => new Error('fail')));
    component.ngOnInit();
    expect(component.loading()).toBeFalse();
  });
});
