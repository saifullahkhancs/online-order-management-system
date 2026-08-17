import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../services/toast.service';

/** Requires any authenticated session. */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated()) return true;
  return router.createUrlTree(['/auth/login'], {
    queryParams: { returnUrl: state.url },
  });
};

/** Requires one of the admin-console roles. */
export const adminGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const toast = inject(ToastService);

  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/auth/login'], {
      queryParams: { returnUrl: state.url },
    });
  }
  if (!auth.canAccessAdmin()) {
    toast.error('Your account does not have access to the admin console.');
    return router.createUrlTree(['/']);
  }
  return true;
};

/**
 * Requires a specific role list, declared on the route:
 *   { path: 'users', canActivate: [roleGuard], data: { roles: ['SystemAdmin'] } }
 */
export const roleGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const toast = inject(ToastService);
  const required = (route.data?.['roles'] as string[] | undefined) ?? [];

  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/auth/login'], {
      queryParams: { returnUrl: state.url },
    });
  }
  if (required.length && !auth.hasAnyRole(required)) {
    toast.error('You do not have permission to open that page.');
    return router.createUrlTree(['/admin']);
  }
  return true;
};

/** Keeps signed-in admins out of the login page. */
export const guestOnlyGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isAuthenticated() && auth.canAccessAdmin()
    ? router.createUrlTree(['/admin'])
    : true;
};
