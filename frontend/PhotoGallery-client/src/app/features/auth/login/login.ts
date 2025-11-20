import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { LoginRequest } from '../../../core/models/auth.models';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {
  private authService = inject(AuthService);
  private router = inject(Router);

  loginData = signal<LoginRequest>({
    email: '',
    password: ''
  });

  isLoading = signal(false);
  errorMessage = signal('');

  onSubmit() {
    this.isLoading.set(true);
    this.errorMessage.set('');

    this.authService.login(this.loginData()).subscribe({
      next: () => {
        this.router.navigate(['/albums']);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.message || 'Invalid email or password');
      },
      complete: () => {
        this.isLoading.set(false);
      }
    });
  }

  updateEmail(value: string) {
    this.loginData.update(data => ({ ...data, email: value }));
  }

  updatePassword(value: string) {
    this.loginData.update(data => ({ ...data, password: value }));
  }
}