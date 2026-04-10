import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent implements OnInit {
  loginForm!: FormGroup;
  errorMessage: string = '';
  successMessage: string = '';
  isLoading: boolean = false;
  isPasskeyRegistering: boolean = false;
  isBiometricLoading: boolean = false;
  passkeySupported: boolean = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.passkeySupported = typeof window !== 'undefined' && !!window.PublicKeyCredential;

    if (this.authService.hasToken()) {
      this.router.navigate(['/scan']);
    }

    this.loginForm = this.fb.group({
      email: ['', [Validators.email]],
      password: ['']
    });
  }

  onSubmit(): void {
    const email = String(this.loginForm.get('email')?.value ?? '').trim();
    const password = String(this.loginForm.get('password')?.value ?? '');

    if (!email || !password) {
      this.loginForm.get('email')?.markAsTouched();
      this.loginForm.get('password')?.markAsTouched();
      this.errorMessage = 'Enter email and password to sign in.';
      this.successMessage = '';
      return;
    }

    if (this.loginForm.get('email')?.invalid) {
      this.loginForm.get('email')?.markAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.authService.login({ email, password }).subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigate(['/scan']);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message || 'Login failed. Please check your credentials.';
      }
    });
  }

  registerPasskey(): void {
    const emailControl = this.loginForm.get('email');
    if (!this.passkeySupported) {
      this.errorMessage = 'Passkey is not supported on this browser/device.';
      this.successMessage = '';
      return;
    }

    if (!emailControl?.value?.toString().trim() || emailControl.invalid) {
      emailControl?.markAsTouched();
      this.errorMessage = 'Enter a valid email before registering a passkey.';
      this.successMessage = '';
      return;
    }

    const passwordControl = this.loginForm.get('password');
    if (!passwordControl?.value?.toString().trim()) {
      passwordControl?.markAsTouched();
      this.errorMessage = 'Enter your password to register a passkey (proves it is your account).';
      this.successMessage = '';
      return;
    }

    this.isPasskeyRegistering = true;
    this.errorMessage = '';
    this.successMessage = '';

    const email = String(emailControl.value).trim();
    const password = String(passwordControl.value);
    this.authService.registerPasskey(email, password).subscribe({
      next: () => {
        this.isPasskeyRegistering = false;
        this.successMessage = 'Passkey registered. You can use Login using biometric next time.';
      },
      error: (err) => {
        this.isPasskeyRegistering = false;
        this.errorMessage = err.error?.message || 'Passkey registration failed. Please try again.';
      }
    });
  }

  loginUsingBiometric(): void {
    if (!this.passkeySupported) {
      this.errorMessage = 'Biometric login is not supported on this browser/device.';
      this.successMessage = '';
      return;
    }

    this.isBiometricLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.authService.loginWithDeviceBiometric().subscribe({
      next: () => {
        this.isBiometricLoading = false;
        this.router.navigate(['/scan']);
      },
      error: (err) => {
        this.isBiometricLoading = false;
        this.errorMessage = err.error?.message || 'Biometric login failed. Register a passkey on this device first.';
      }
    });
  }
}
