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
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.css']
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
