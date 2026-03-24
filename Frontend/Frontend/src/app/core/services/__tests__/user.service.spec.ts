import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { UserService } from '../user.service';
import { environment } from '../../../../environments/environment';
import { UserRole, AccountStatus } from '../../models/models';

describe('UserService', () => {
  let service: UserService;
  let http: HttpTestingController;
  const base = `${environment.apiUrl}/User`;

  const mockUser = {
    userId: 42, name: 'Tyson', email: 'tyson@gmail.com',
    role: UserRole.USER, status: AccountStatus.ACTIVE,
    createdAt: '2026-03-20T00:00:00Z'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(UserService);
    http    = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('getMe() sends GET to /api/User/me', () => {
    service.getMe().subscribe();
    const req = http.expectOne(`${base}/me`);
    expect(req.request.method).toBe('GET');
    req.flush(mockUser);
  });

  it('getMe() returns UserDto', () => {
    let result: any;
    service.getMe().subscribe(u => result = u);
    http.expectOne(`${base}/me`).flush(mockUser);
    expect(result.name).toBe('Tyson');
    expect(result.email).toBe('tyson@gmail.com');
    expect(result.role).toBe(UserRole.USER);
  });

  it('updateMe() sends PUT to /api/User/me with name only', () => {
    service.updateMe({ name: 'Tyson Updated' }).subscribe();
    const req = http.expectOne(`${base}/me`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ name: 'Tyson Updated' });
    req.flush({ ...mockUser, name: 'Tyson Updated' });
  });

  it('updateMe() returns updated UserDto', () => {
    let result: any;
    service.updateMe({ name: 'NewName' }).subscribe(u => result = u);
    http.expectOne(`${base}/me`).flush({ ...mockUser, name: 'NewName' });
    expect(result.name).toBe('NewName');
  });

  it('getAll() sends GET to /api/User (admin only)', () => {
    service.getAll().subscribe();
    const req = http.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([mockUser]);
  });

  it('getAll() returns array of users', () => {
    let result: any[];
    service.getAll().subscribe(us => result = us);
    http.expectOne(base).flush([mockUser, { ...mockUser, userId: 2 }]);
    expect(result!.length).toBe(2);
  });

  it('create() sends POST to /api/User with correct payload', () => {
    const dto = { name: 'New User', email: 'new@gmail.com', password: 'pass123', role: UserRole.USER };
    service.create(dto).subscribe();
    const req = http.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(dto);
    req.flush({ ...mockUser, ...dto });
  });

  it('delete() sends DELETE to /api/User/{id}', () => {
    service.delete(42).subscribe();
    const req = http.expectOne(`${base}/42`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });
  });

  it('delete() uses correct user id in URL', () => {
    service.delete(7).subscribe();
    const req = http.expectOne(`${base}/7`);
    expect(req.request.url).toContain('/7');
    req.flush(null, { status: 204, statusText: 'No Content' });
  });
});
