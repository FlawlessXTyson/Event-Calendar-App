import { Component } from '@angular/core';
import { AuthService } from '../../core/services/auth-service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './register.html'
})
export class RegisterComponent {

  user = {
    userName: '',
    email: '',
    password: ''
  };

  constructor(private auth: AuthService) {}

  onRegister() {
    this.auth.register(this.user).subscribe({
      next: (res) => {
        alert('Registered successfully');
        console.log(res);
      },
      error: (err) => {
        alert('Error');
        console.error(err);
      }
    });
  }
}