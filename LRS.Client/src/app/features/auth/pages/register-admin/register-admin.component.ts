import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register-admin',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './register-admin.component.html',
  styleUrl: './register-admin.component.css'
})
export class RegisterAdminComponent implements OnInit {
  form: FormGroup;
  passkeyForm: FormGroup;
  errorMessage = '';
  successMessage = '';
  isSubmitting = false;

  /** Email of the admin just created — next step can register a passkey for that account on this device. */
  pendingPasskeyEmail: string | null = null;
  passkeyError = '';
  passkeySuccess = '';
  isRegisteringPasskey = false;
  passkeySupported = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService
  ) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', Validators.required]
    });
    this.passkeyForm = this.fb.group({
      verifyPassword: ['', [Validators.required, Validators.minLength(8)]]
    });
  }

  ngOnInit(): void {
    this.passkeySupported = typeof window !== 'undefined' && !!window.PublicKeyCredential;
  }

  onSubmit(): void {
    this.errorMessage = '';
    this.successMessage = '';
    this.passkeyError = '';
    this.passkeySuccess = '';
    this.pendingPasskeyEmail = null;

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { email, password, confirmPassword } = this.form.value;
    if (password !== confirmPassword) {
      this.errorMessage = 'Passwords do not match.';
      return;
    }

    const emailTrim = String(email).trim();
    this.isSubmitting = true;
    this.authService.registerAdmin(emailTrim, String(password)).subscribe({
      next: (res) => {
        this.isSubmitting = false;
        this.successMessage = res.message ?? 'Administrator created.';
        this.pendingPasskeyEmail = emailTrim;
        this.form.reset();
        this.passkeyForm.reset();
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = this.extractHttpError(err);
      }
    });
  }

  registerBiometric(): void {
    this.passkeyError = '';
    this.passkeySuccess = '';

    if (!this.pendingPasskeyEmail) {
      return;
    }

    if (!this.passkeySupported) {
      this.passkeyError = 'This browser does not support passkeys / biometrics.';
      return;
    }

    if (this.passkeyForm.invalid) {
      this.passkeyForm.markAllAsTouched();
      return;
    }

    const pwd = String(this.passkeyForm.value.verifyPassword);
    this.isRegisteringPasskey = true;
    this.authService.registerPasskey(this.pendingPasskeyEmail, pwd).subscribe({
      next: () => {
        this.isRegisteringPasskey = false;
        this.passkeySuccess = 'Biometric / passkey registered for this administrator. They can sign in with “Login using biometric” on this device.';
        this.passkeyForm.reset();
      },
      error: (err) => {
        this.isRegisteringPasskey = false;
        this.passkeyError = err.error?.message ?? 'Passkey registration failed.';
      }
    });
  }

  skipPasskey(): void {
    this.pendingPasskeyEmail = null;
    this.passkeyError = '';
    this.passkeySuccess = '';
    this.passkeyForm.reset();
  }

  private extractHttpError(err: unknown): string {
    const e = err as {
      error?: { message?: string; title?: string; errors?: string };
      status?: number;
      message?: string;
    };
    const body = e?.error;
    if (typeof body === 'string' && body== null) {
      return body;
    }
    if (body && typeof body === 'object') {
      if (typeof body.message === 'string' && body.message.length > 0) {
        return body.message;
      }
      if (typeof body.title === 'string' && body.title.length > 0) {
        return body.title;
      }
    }
    if (e?.status === 403) {
      return 'You do not have permission to create administrators. Sign out and sign in again as an admin so your token includes the Admin role.';
    }
    if (e?.status === 401) {
      return 'Your session expired. Please sign in again.';
    }
    return 'Could not create administrator.';
  }
}
