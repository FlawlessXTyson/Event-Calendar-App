import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-not-found',
  standalone: true,
  templateUrl: './not-found.component.html',
  styleUrls: ['./not-found.component.css']
})
export class NotFoundComponent {
  private router = inject(Router);
  private auth   = inject(AuthService);
  goBack() { history.back(); }
  goHome() {
    if (this.auth.isLoggedIn()) this.auth.redirectByRole();
    else this.router.navigate(['/']);
  }
}
