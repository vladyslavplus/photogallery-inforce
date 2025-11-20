import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { RegisterRequest } from '../../../core/models/auth.models';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css'
})
export class Register {
  private authService = inject(AuthService);
  private router = inject(Router);

  registerData = signal<RegisterRequest>({
    email: '',
    userName: '',
    password: ''
  });

  isLoading = signal(false);
  errorMessage = signal('');
  successMessage = signal('');

  onSubmit() {
    this.isLoading.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.authService.register(this.registerData()).subscribe({
      next: () => {
        this.successMessage.set('Registration successful! Redirecting...');
        setTimeout(() => {
          this.router.navigate(['/albums']);
        }, 1500);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.message || 'Registration failed. Please try again.');
      },
      complete: () => {
        this.isLoading.set(false);
      }
    });
  }

  updateEmail(value: string) {
    this.registerData.update(data => ({ ...data, email: value }));
  }

  updateUserName(value: string) {
    this.registerData.update(data => ({ ...data, userName: value }));
  }

  updatePassword(value: string) {
    this.registerData.update(data => ({ ...data, password: value }));
  }
}