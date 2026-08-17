import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../services/toast.service';

/**
 * Attaches the bearer token and reacts to auth failures.
 *
 * - 401: the token is missing/expired/invalid -> sign out and bounce to login.
 * - 403: authenticated but the role is insufficient -> keep the session, warn.
 *
 * Public storefront endpoints (menu, cart, place-order) are `[AllowAnonymous]`,
 * so requests simply go out without a header when nobody is signed in.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const toast = inject(ToastService);

  const token = auth.token();
  const authed = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authed).pipe(
    catchError((err) => {
      if (err?.status === 401) {
        if (auth.isAuthenticated()) {
          auth.logout(null);
          toast.error('Your session expired. Please sign in again.');
        }
        if (router.url.startsWith('/admin')) {
          void router.navigate(['/auth/login'], {
            queryParams: { returnUrl: router.url },
          });
        }
      } else if (err?.status === 403) {
        toast.error('You do not have permission to perform that action.');
      }
      return throwError(() => err);
    }),
  );
};
