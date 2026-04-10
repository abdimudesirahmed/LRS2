import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, BehaviorSubject, switchMap, from, map, catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { create, get, type CredentialCreationOptionsJSON, type CredentialRequestOptionsJSON } from '@github/webauthn-json';

/**
 * Fido2/ASP.NET returns AssertionOptions / CredentialCreateOptions as a flat JSON object
 * (challenge, rpId, …). @github/webauthn-json expects Credential*OptionsJSON with a `publicKey` wrapper.
 */
function normalizeCredentialRequestOptions(server: unknown): CredentialRequestOptionsJSON {
  const o = server as Record<string, unknown> | null;
  if (o && typeof o === 'object' && 'publicKey' in o && o['publicKey'] != null) {
    return prepareAssertionOptionsJson(server as CredentialRequestOptionsJSON);
  }
  return prepareAssertionOptionsJson({
    publicKey: server as NonNullable<CredentialRequestOptionsJSON['publicKey']>
  });
}

function normalizeCredentialCreationOptions(server: unknown): CredentialCreationOptionsJSON {
  const o = server as Record<string, unknown> | null;
  if (o && typeof o === 'object' && 'publicKey' in o && o['publicKey'] != null) {
    return server as CredentialCreationOptionsJSON;
  }
  return {
    publicKey: server as NonNullable<CredentialCreationOptionsJSON['publicKey']>
  };
}

/** WebAuthn: if allowCredentials is present but empty, browsers must reject (NotAllowedError). Omit it for discoverable login. */
function prepareAssertionOptionsJson(
  serverJson: CredentialRequestOptionsJSON
): CredentialRequestOptionsJSON {
  const clone = structuredClone(serverJson) as CredentialRequestOptionsJSON;
  const pk = clone.publicKey as Record<string, unknown> | undefined;
  if (pk) {
    const ac = pk['allowCredentials'];
    if (Array.isArray(ac) && ac.length === 0) {
      delete pk['allowCredentials'];
    }
    const t = pk['timeout'];
    if (t == null || (typeof t === 'number' && t < 120_000)) {
      pk['timeout'] = 120_000;
    }
  }
  return clone;
}

function webAuthnErrorMessage(err: unknown, context: 'assert' | 'create'): string {
  if (err instanceof DOMException) {
    switch (err.name) {
      case 'NotAllowedError':
        return context === 'assert'
          ? 'Sign-in was cancelled or not allowed. For fingerprint on this PC: register a passkey here first (with password), use the same browser, and open the app at http://localhost:4200 (not 127.0.0.1).'
          : 'Registration was cancelled or not allowed. Ensure Windows Hello / fingerprint is set up, or try another authenticator.';
      case 'InvalidStateError':
        return 'This authenticator is already registered or cannot be used in its current state.';
      case 'NotSupportedError':
        return 'This security key or browser does not support the requested operation.';
      case 'SecurityError':
        return 'WebAuthn was blocked (origin / security). Use http://localhost:4200 to match the API configuration.';
      case 'AbortError':
        return 'The operation was aborted.';
      default:
        return err.message || err.name;
    }
  }
  return err instanceof Error ? err.message : 'WebAuthn request failed.';
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = '/api/auth';
  private webAuthnApiUrl = '/api/auth/webauthn';
  private readonly TOKEN_KEY = 'jwt_token';

  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.hasToken());
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  constructor(private http: HttpClient, private router: Router) {}

  login(credentials: { email: string; password: string }): Observable<{ token: string }> {
    return this.http.post<{ token: string }>(`${this.apiUrl}/login`, credentials).pipe(
      tap((response) => {
        if (response && response.token) {
          sessionStorage.setItem(this.TOKEN_KEY, response.token);
          localStorage.removeItem(this.TOKEN_KEY);
          this.isAuthenticatedSubject.next(true);
        }
      })
    );
  }

  registerPasskey(email: string, password: string): Observable<void> {
    return this.http
      .post<unknown>(`${this.webAuthnApiUrl}/registerOptions`, {
        username: email,
        password
      })
      .pipe(
        switchMap((raw) =>
          from(create(normalizeCredentialCreationOptions(raw))).pipe(
            catchError((e) => throwError(() => ({ error: { message: webAuthnErrorMessage(e, 'create') } })))
          )
        ),
        switchMap((credential) =>
          this.http.post<{ status: string }>(`${this.webAuthnApiUrl}/register`, {
            username: email,
            response: credential
          })
        ),
        map(() => void 0)
      );
  }

  /** Discoverable passkey: no email — Windows Hello / platform shows account picker. */
  loginWithDeviceBiometric(): Observable<{ token: string }> {
    return this.http
      .post<unknown>(`${this.webAuthnApiUrl}/loginOptions`, { username: '' })
      .pipe(
        switchMap((raw) =>
          from(get(normalizeCredentialRequestOptions(raw))).pipe(
            catchError((e) => throwError(() => ({ error: { message: webAuthnErrorMessage(e, 'assert') } })))
          )
        ),
        switchMap((assertion) =>
          this.http.post<{ token: string }>(`${this.webAuthnApiUrl}/login`, {
            username: '',
            response: assertion
          })
        ),
        tap((response) => {
          if (response && response.token) {
            sessionStorage.setItem(this.TOKEN_KEY, response.token);
            localStorage.removeItem(this.TOKEN_KEY);
            this.isAuthenticatedSubject.next(true);
          }
        })
      );
  }

  logout(): void {
    sessionStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.TOKEN_KEY);
    this.isAuthenticatedSubject.next(false);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    const token = sessionStorage.getItem(this.TOKEN_KEY);
    if (!token) {
      localStorage.removeItem(this.TOKEN_KEY);
      return null;
    }

    if (!this.isTokenValid(token)) {
      sessionStorage.removeItem(this.TOKEN_KEY);
      localStorage.removeItem(this.TOKEN_KEY);
      this.isAuthenticatedSubject.next(false);
      return null;
    }

    return token;
  }

  hasToken(): boolean {
    return !!this.getToken();
  }

  /** Reads role from JWT (supports `role` and .NET long claim name). */
  isAdmin(): boolean {
    const token = this.getToken();
    if (!token) {
      return false;
    }
    const payload = this.parseJwtPayload(token);
    if (!payload) {
      return false;
    }
    const longRole =
      'http://schemas.microsoft.com/ws/2008/06/identity/claims/role' as const;
    const raw = payload['role'] ?? payload[longRole];
    if (Array.isArray(raw)) {
      return raw.some((r) => String(r).toLowerCase() === 'admin');
    }
    return String(raw ?? '').toLowerCase() === 'admin';
  }

  registerAdmin(email: string, password: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/register-admin`, { email, password });
  }

  private parseJwtPayload(token: string): Record<string, unknown> | null {
    try {
      const part = token.split('.')[1];
      if (!part) {
        return null;
      }
      const normalized = part.replace(/-/g, '+').replace(/_/g, '/');
      const padded = normalized + '='.repeat((4 - (normalized.length % 4)) % 4);
      const decoded = atob(padded);
      return JSON.parse(decoded) as Record<string, unknown>;
    } catch {
      return null;
    }
  }

  private isTokenValid(token: string): boolean {
    const payload = this.parseJwtPayload(token);
    const exp = payload?.['exp'];
    if (typeof exp !== 'number') {
      return false;
    }
    return exp > Math.floor(Date.now() / 1000);
  }
}
