import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { LoginRequest, LoginResponse, RegisterRequest, JwtPayload, UserRole } from '../models/models';

const TOKEN_KEY = 'eca_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http   = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly base   = `${environment.apiUrl}/Authentication`;

  private _token = signal<string | null>(localStorage.getItem(TOKEN_KEY));

  // Public signals
  readonly token       = this._token.asReadonly();
  readonly isLoggedIn  = computed(() => !!this._token() && !this._isExpired());
  readonly currentUser = computed(() => this._decode());
  readonly role        = computed(() => this._getRole());
  readonly userId      = computed(() => this._getUserId());
  readonly userName    = computed(() => this._getUserName());
  readonly userEmail   = computed(() => this._getUserEmail());

  readonly isAdmin     = computed(() => this.role() === 'ADMIN');
  readonly isOrganizer = computed(() => this.role() === 'ORGANIZER');
  readonly isUser      = computed(() => this.role() === 'USER');

  /** Registered by ReminderNotificationService — avoids circular DI */
  private _onLogout?: () => void;
  registerLogoutHook(fn: () => void): void { this._onLogout = fn; }

  login(req: LoginRequest) {
    return this.http.post<LoginResponse>(`${this.base}/login`, req).pipe(
      tap(res => this._saveToken(res.token))
    );
  }

  register(req: RegisterRequest) {
    return this.http.post<LoginResponse>(`${this.base}/register`, req).pipe(
      tap(res => this._saveToken(res.token))
    );
  }

  logout(): void {
    this._onLogout?.(); // stop reminder polling BEFORE clearing token
    localStorage.removeItem(TOKEN_KEY);
    this._token.set(null);
    this.router.navigate(['/']);
  }

  redirectByRole(): void {
    const r = this.role();
    if (r === 'ADMIN')          this.router.navigate(['/admin/dashboard']);
    else if (r === 'ORGANIZER') this.router.navigate(['/organizer/dashboard']);
    else                        this.router.navigate(['/user/dashboard']);
  }

  private _saveToken(token: string): void {
    localStorage.setItem(TOKEN_KEY, token);
    this._token.set(token);
  }

  private _decode(): JwtPayload | null {
    const t = this._token();
    if (!t) return null;
    try {
      return JSON.parse(atob(t.split('.')[1])) as JwtPayload;
    } catch { return null; }
  }

  private _isExpired(): boolean {
    const p = this._decode();
    return !p || Date.now() / 1000 > p.exp;
  }

  private _getRole(): string | null {
    const p = this._decode();
    return p?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
      ?? p?.role
      ?? null;
  }

  private _getUserId(): number | null {
    const p = this._decode();
    const raw = p?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']
      ?? p?.nameid;
    return raw ? parseInt(raw, 10) : null;
  }

  private _getUserName(): string {
    const p = this._decode();
    return p?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name']
      ?? p?.unique_name
      ?? '';
  }

  private _getUserEmail(): string {
    const p = this._decode();
    return p?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress']
      ?? p?.email
      ?? '';
  }
}
