import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../../features/auth/services/auth.service';

function isAnonymousAuthPath(urlLower: string): boolean {
  if (urlLower.includes('/api/auth/login') || urlLower.includes('/api/auth/webauthn')) {
    return true;
  }
  // Public self-registration only — not register-admin (substring match would wrongly skip Bearer on register-admin)
  if (urlLower.includes('/api/auth/register-admin')) {
    return false;
  }
  if (urlLower.includes('/api/auth/register')) {
    return true;
  }
  return false;
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const url = req.url.toLowerCase();
  if (isAnonymousAuthPath(url)) {
    return next(req);
  }

  const authService = inject(AuthService);
  const token = authService.getToken();

  if (token) {
    const authReq = req.clone({
      headers: req.headers.set('Authorization', `Bearer ${token}`)
    });
    return next(authReq);
  }

  return next(req);
};
