import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { ApiService } from './api.service';
import { StorageKeys, StorageService } from './storage.service';
import {
  ADMIN_ROLES,
  AuthSession,
  LoginRequest,
  LoginResponse,
  ResetPasswordRequest,
} from '../models/auth.models';

/**
 * Session owner.
 *
 * The backend issues a JWT whose `role` claims drive `[Authorize(Roles=...)]`
 * on the API. We mirror those roles client-side purely for navigation and to
 * hide controls the user cannot use - the API remains the real gate.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiService);
  private readonly storage = inject(StorageService);
  private readonly router = inject(Router);

  private readonly _session = signal<AuthSession | null>(this.restore());

  /** Current session, or null when signed out. */
  readonly session = this._session.asReadonly();
  readonly isAuthenticated = computed(() => this._session() !== null);
  readonly roles = computed(() => this._session()?.roles ?? []);
  readonly displayName = computed(() => this._session()?.username ?? 'Guest');
  readonly headOfficeId = computed(() => this._session()?.headOfficeId ?? null);
  readonly branches = computed(() => this._session()?.branches ?? []);

  /** True when the user may open /admin at all. */
  readonly canAccessAdmin = computed(() => this.hasAnyRole(ADMIN_ROLES));
  /** True when the user may create/edit catalogue records. */
  readonly canManageCatalog = computed(() =>
    this.hasAnyRole(['SystemAdmin', 'SuperAdmin', 'Admin']),
  );
  /** Only the top two roles manage users and roles. */
  readonly canManageUsers = computed(() => this.hasAnyRole(['SystemAdmin', 'SuperAdmin', 'Admin']));

  login(payload: LoginRequest): Observable<LoginResponse> {
    return this.api.post<LoginResponse>('/api/Auth/login', payload).pipe(
      tap((res) => {
        if (!res?.token) throw new Error('Login succeeded but no token was returned.');
        const session: AuthSession = {
          userId: res.userId,
          username: res.username,
          email: res.email,
          headOfficeId: res.headOfficeId ?? null,
          headOfficeName: res.headOfficeName ?? null,
          branches: res.branches ?? [],
          roles: (res.roles ?? []).map((r) => r.roleName ?? '').filter(Boolean),
          token: res.token,
          expiresAt: readJwtExpiry(res.token),
        };
        this._session.set(session);
        this.storage.set(StorageKeys.session, session);
      }),
    );
  }

  resetPassword(payload: ResetPasswordRequest) {
    return this.api.post<unknown>('/api/Users/reset-password', payload);
  }

  logout(redirectTo: string | null = '/auth/login'): void {
    this._session.set(null);
    this.storage.remove(StorageKeys.session);
    if (redirectTo) void this.router.navigateByUrl(redirectTo);
  }

  /** Raw bearer token for the interceptor. */
  token(): string | null {
    const s = this._session();
    if (!s) return null;
    if (s.expiresAt && Date.now() >= s.expiresAt) {
      // Token already expired - drop it so we do not send a 401-guaranteed header.
      this.logout(null);
      return null;
    }
    return s.token;
  }

  hasAnyRole(roles: readonly string[]): boolean {
    const mine = this._session()?.roles ?? [];
    return roles.some((r) => mine.includes(r));
  }

  private restore(): AuthSession | null {
    const s = this.storage.get<AuthSession | null>(StorageKeys.session, null);
    if (!s) return null;
    if (s.expiresAt && Date.now() >= s.expiresAt) {
      this.storage.remove(StorageKeys.session);
      return null;
    }
    return s;
  }
}

/**
 * Reads `exp` from a JWT without pulling in a library.
 * Falls back to "12 hours from now" (the API's Jwt:ExpireMinutes default)
 * if the token cannot be parsed.
 */
function readJwtExpiry(token: string): number {
  try {
    const payload = token.split('.')[1];
    const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
    const claims = JSON.parse(json) as { exp?: number };
    if (typeof claims.exp === 'number') return claims.exp * 1000;
  } catch {
    /* fall through */
  }
  return Date.now() + 12 * 60 * 60 * 1000;
}
