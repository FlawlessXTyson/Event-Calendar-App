import { Component, inject, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-public-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CommonModule],
  templateUrl: './public-layout.component.html',
  styles: [`
    @media(max-width:640px){
      .desk-nav { display:none!important; }
      .nav-actions { display:none!important; }
      .mobile-menu-btn { display:flex!important; }
    }
  `]
})
export class PublicLayoutComponent {
  readonly auth = inject(AuthService);
  mobileOpen = signal(false);
}
