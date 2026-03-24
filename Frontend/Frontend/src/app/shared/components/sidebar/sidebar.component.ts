import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { RegistrationStateService } from '../../../core/services/registration-state.service';
import { CommonModule } from '@angular/common';

export interface NavItem {
  label: string;
  icon: string;
  route: string;
  badge?: number;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, CommonModule],
  template: `
    <aside class="sidebar" [class.open]="open">
      <div class="sidebar-brand">
        <div class="logo">
          <span class="material-icons-round">calendar_month</span>
        </div>
        <div>
          <div class="brand-name">EventCalenderApp</div>
          <div class="brand-sub">{{ roleLabel }}</div>
        </div>
      </div>

      <nav class="sidebar-nav">
        @for (item of items; track item.route) {
          <a [routerLink]="regState.isNavigationBlocked() ? null : item.route"
             routerLinkActive="active"
             [style.opacity]="regState.isNavigationBlocked() ? '0.5' : '1'"
             [style.cursor]="regState.isNavigationBlocked() ? 'not-allowed' : 'pointer'"
             (click)="onNavClick(item)">
            <span class="material-icons-round">{{ item.icon }}</span>
            <span style="flex:1">{{ item.label }}</span>
            @if (item.badge) {
              <span style="background:var(--primary);color:#fff;border-radius:9999px;padding:2px 7px;font-size:.7rem;font-weight:700">
                {{ item.badge }}
              </span>
            }
          </a>
        }
      </nav>

      <div class="sidebar-user">
        <div class="avatar">{{ nameInitial() }}</div>
        <div class="user-info">
          <div class="name">{{ auth.userName() }}</div>
          <div class="role">{{ roleLabel }}</div>
        </div>
        <button type="button" (click)="auth.logout()" title="Sign out"
          style="background:none;border:none;cursor:pointer;color:rgba(255,255,255,.45);margin-left:auto;">
          <span class="material-icons-round" style="font-size:20px;">logout</span>
        </button>
      </div>
    </aside>
  `
})
export class SidebarComponent {
  @Input() items: NavItem[]  = [];
  @Input() roleLabel         = '';
  @Input() open              = false;
  @Output() navClick         = new EventEmitter<void>();

  readonly auth     = inject(AuthService);
  readonly regState = inject(RegistrationStateService);
  private toast     = inject(ToastService);

  nameInitial() { return (this.auth.userName()?.[0] ?? '?').toUpperCase(); }

  onNavClick(item: NavItem) {
    if (this.regState.isNavigationBlocked()) {
      this.toast.warning(
        'Please complete payment or cancel registration before leaving.',
        'Navigation Blocked'
      );
      return;
    }
    this.navClick.emit();
  }
}
