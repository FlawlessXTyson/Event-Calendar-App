import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/services/auth-service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './login.html'
})
export class LoginComponent {

  user = {
    email: '',
    password: ''
  };

  constructor(private auth: AuthService, private router: Router) {}

  onLogin() {
    this.auth.login(this.user).subscribe({
      next: (res: any) => {

        //store token
        this.auth.saveToken(res.token);

        alert('Login success');

        console.log(res);

        //redirect to home
        this.router.navigate(['/']);
      },
      error: (err) => {
        alert('Invalid credentials');
        console.error(err);
      }
    });
  }
}