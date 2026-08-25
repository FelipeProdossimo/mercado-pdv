import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  templateUrl: './login.html',
})
export class Login {
  private authService = inject(AuthService);
  private router = inject(Router);

  email = '';
  senha = '';
  loading = false;
  erro = '';

  login(): void {
    this.erro = '';
    this.loading = true;

    this.authService.login({
      email: this.email,
      senha: this.senha
    }).subscribe({

      next: () => {
        this.router.navigate(['/dashboard']);
      },

      error: () => {
        this.erro = 'Email ou senha inválidos';
        this.loading = false;
      }
    });
  }
}