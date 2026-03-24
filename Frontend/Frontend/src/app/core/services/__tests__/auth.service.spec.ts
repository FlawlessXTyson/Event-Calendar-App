import { TestBed, fakeAsync } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from '../auth.service';
import { Router } from '@angular/router';

// A valid JWT for a USER with exp far in future
// Payload: { "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier": "42",
//            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "Tyson",
//            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress": "tyson@gmail.com",
//            "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "USER",
//            "exp": 9999999999 }
const USER_JWT = [
  'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9',
  btoa(JSON.stringify({
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': '42',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'Tyson',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress': 'tyson@gmail.com',
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'USER',
    exp: 9999999999, iat: 1700000000
  })).replace(/=/g, ''),
  'fakesignature'
].join('.');

const ADMIN_JWT = [
  'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9',
  btoa(JSON.stringify({
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': '1',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'Admin',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress': 'admin@gmail.com',
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'ADMIN',
    exp: 9999999999, iat: 1700000000
  })).replace(/=/g, ''),
  'fakesignature'
].join('.');

const ORGANIZER_JWT = [
  'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9',
  btoa(JSON.stringify({
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': '7',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'organizer1',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress': 'org1@gmail.com',
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'ORGANIZER',
    exp: 9999999999, iat: 1700000000
  })).replace(/=/g, ''),
  'fakesignature'
].join('.');

const EXPIRED_JWT = [
  'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9',
  btoa(JSON.stringify({
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'USER',
    exp: 1, iat: 1
  })).replace(/=/g, ''),
  'fakesignature'
].join('.');

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;
  let router: Router;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule], providers: [provideRouter([])]
    });
    service = TestBed.inject(AuthService);
    http    = TestBed.inject(HttpTestingController);
    router  = TestBed.inject(Router);
    spyOn(router, 'navigate').and.returnValue(Promise.resolve(true));
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  // ── Initial state (no token) ──────────────────────────────────────────────
  it('isLoggedIn is false when no token in localStorage', () => {
    expect(service.isLoggedIn()).toBeFalse();
  });

  it('role is null when not logged in', () => {
    expect(service.role()).toBeNull();
  });

  it('userName returns empty string when not logged in', () => {
    expect(service.userName()).toBe('');
  });

  it('userEmail returns empty string when not logged in', () => {
    expect(service.userEmail()).toBe('');
  });

  it('userId returns null when not logged in', () => {
    expect(service.userId()).toBeNull();
  });

  // ── login() ───────────────────────────────────────────────────────────────
  it('login() sends POST to /api/Authentication/login with credentials', () => {
    service.login({ email: 'tyson@gmail.com', password: 'pass123' }).subscribe();
    const req = http.expectOne(r => r.url.includes('/Authentication/login'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'tyson@gmail.com', password: 'pass123' });
    req.flush({ token: USER_JWT });
  });

  it('login() saves token to localStorage after success', () => {
    service.login({ email: 'tyson@gmail.com', password: 'pass123' }).subscribe();
    const req = http.expectOne(r => r.url.includes('/Authentication/login'));
    req.flush({ token: USER_JWT });
    expect(localStorage.getItem('eca_token')).toBe(USER_JWT);
  });

  it('after login: isLoggedIn is true', () => {
    service.login({ email: 'tyson@gmail.com', password: 'pass123' }).subscribe();
    http.expectOne(r => r.url.includes('/Authentication/login')).flush({ token: USER_JWT });
    expect(service.isLoggedIn()).toBeTrue();
  });

  it('after USER login: role = USER, isUser = true, isAdmin = false', () => {
    service.login({ email: 'tyson@gmail.com', password: 'pass123' }).subscribe();
    http.expectOne(r => r.url.includes('/Authentication/login')).flush({ token: USER_JWT });
    expect(service.role()).toBe('USER');
    expect(service.isUser()).toBeTrue();
    expect(service.isAdmin()).toBeFalse();
    expect(service.isOrganizer()).toBeFalse();
  });

  it('after ADMIN login: isAdmin = true', () => {
    service.login({ email: 'admin@gmail.com', password: 'pass' }).subscribe();
    http.expectOne(r => r.url.includes('/Authentication/login')).flush({ token: ADMIN_JWT });
    expect(service.isAdmin()).toBeTrue();
    expect(service.isUser()).toBeFalse();
  });

  it('after ORGANIZER login: isOrganizer = true', () => {
    service.login({ email: 'org1@gmail.com', password: 'pass' }).subscribe();
    http.expectOne(r => r.url.includes('/Authentication/login')).flush({ token: ORGANIZER_JWT });
    expect(service.isOrganizer()).toBeTrue();
  });

  it('after login: userName and userEmail are populated', () => {
    service.login({ email: 'tyson@gmail.com', password: 'pass123' }).subscribe();
    http.expectOne(r => r.url.includes('/Authentication/login')).flush({ token: USER_JWT });
    expect(service.userName()).toBe('Tyson');
    expect(service.userEmail()).toBe('tyson@gmail.com');
    expect(service.userId()).toBe(42);
  });

  // ── logout() ──────────────────────────────────────────────────────────────
  it('logout() clears localStorage token', () => {
    localStorage.setItem('eca_token', USER_JWT);
    service.logout();
    expect(localStorage.getItem('eca_token')).toBeNull();
  });

  it('logout() sets isLoggedIn to false', () => {
    localStorage.setItem('eca_token', USER_JWT);
    // reinitialize to pick up token
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule], providers: [provideRouter([])] });
    service = TestBed.inject(AuthService);
    router  = TestBed.inject(Router);
    spyOn(router, 'navigate').and.returnValue(Promise.resolve(true));
    service.logout();
    expect(service.isLoggedIn()).toBeFalse();
  });

  it('logout() navigates to /', () => {
    service.logout();
    expect(router.navigate).toHaveBeenCalledWith(['/']);
  });

  it('logout() calls registered logout hook', () => {
    const hook = jasmine.createSpy('logoutHook');
    service.registerLogoutHook(hook);
    service.logout();
    expect(hook).toHaveBeenCalled();
  });

  it('logout() calls hook BEFORE clearing token', () => {
    let tokenDuringHook: string | null | undefined;
    service.registerLogoutHook(() => {
      tokenDuringHook = localStorage.getItem('eca_token');
    });
    localStorage.setItem('eca_token', USER_JWT);
    service.logout();
    // hook was called while token was still present
    expect(tokenDuringHook).toBe(USER_JWT);
  });

  // ── Expired token ─────────────────────────────────────────────────────────
  it('isLoggedIn is false for an expired token', () => {
    localStorage.setItem('eca_token', EXPIRED_JWT);
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule], providers: [provideRouter([])] });
    service = TestBed.inject(AuthService);
    expect(service.isLoggedIn()).toBeFalse();
  });

  // ── redirectByRole() ──────────────────────────────────────────────────────
  it('redirectByRole() navigates admin to /admin/dashboard', () => {
    service.login({ email: 'admin@gmail.com', password: 'p' }).subscribe();
    http.expectOne(r => r.url.includes('/Authentication/login')).flush({ token: ADMIN_JWT });
    service.redirectByRole();
    expect(router.navigate).toHaveBeenCalledWith(['/admin/dashboard']);
  });

  it('redirectByRole() navigates organizer to /organizer/dashboard', () => {
    service.login({ email: 'org1@gmail.com', password: 'p' }).subscribe();
    http.expectOne(r => r.url.includes('/Authentication/login')).flush({ token: ORGANIZER_JWT });
    service.redirectByRole();
    expect(router.navigate).toHaveBeenCalledWith(['/organizer/dashboard']);
  });

  it('redirectByRole() navigates user to /user/dashboard', () => {
    service.login({ email: 'tyson@gmail.com', password: 'p' }).subscribe();
    http.expectOne(r => r.url.includes('/Authentication/login')).flush({ token: USER_JWT });
    service.redirectByRole();
    expect(router.navigate).toHaveBeenCalledWith(['/user/dashboard']);
  });

  // ── register() ────────────────────────────────────────────────────────────
  it('register() sends POST to /api/Authentication/register', () => {
    service.register({ username: 'NewUser', email: 'new@gmail.com', password: 'pass123' }).subscribe();
    const req = http.expectOne(r => r.url.includes('/Authentication/register'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body.username).toBe('NewUser');
    req.flush({ token: USER_JWT });
  });

  it('register() saves token and sets isLoggedIn', () => {
    service.register({ username: 'NewUser', email: 'new@gmail.com', password: 'pass123' }).subscribe();
    http.expectOne(r => r.url.includes('/Authentication/register')).flush({ token: USER_JWT });
    expect(service.isLoggedIn()).toBeTrue();
  });
});
