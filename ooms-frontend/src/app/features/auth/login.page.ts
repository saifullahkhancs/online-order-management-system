import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { SpinnerComponent } from '../../shared/components/ui.components';

/**
 * Staff sign in.
 *
 * Customers never need this - the storefront is fully anonymous. This is the
 * door to /admin, gated by the JWT's role claims.
 */
@Component({
  selector: 'app-login-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, SpinnerComponent],
  template: `
    <div class="grid min-h-dvh lg:grid-cols-2">
      <!-- ========================== form ========================== -->
      <div class="flex items-center justify-center px-5 py-12 sm:px-10">
        <div class="w-full max-w-sm">
          <a routerLink="/" class="mb-9 inline-flex items-center gap-2.5">
            <span class="grid h-9 w-9 place-items-center rounded-xl bg-brand-500 text-white shadow-sm">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                   stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M3 11h18M5 11V7a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2v4M4 11v5a4 4 0 0 0 4 4h8a4 4 0 0 0 4-4v-5" />
              </svg>
            </span>
            <span class="text-lg font-bold tracking-tight text-ink-900">OOMS</span>
          </a>

          <h1 class="text-2xl font-bold tracking-tight text-ink-900">Sign in</h1>
          <p class="mt-1.5 text-sm text-ink-500">
            Staff access to the order and catalogue console.
          </p>

          <form [formGroup]="form" (ngSubmit)="submit()" class="mt-7 space-y-4">
            <div>
              <label class="label" for="email">Email or username</label>
              <input
                id="email"
                type="text"
                class="input"
                formControlName="email"
                autocomplete="username"
                placeholder="you@restaurant.com"
                [attr.aria-invalid]="invalid('email')"
              />
              @if (invalid('email')) { <p class="field-error">Enter your email or username.</p> }
            </div>

            <div>
              <label class="label" for="password">Password</label>
              <div class="relative">
                <input
                  id="password"
                  [type]="showPassword() ? 'text' : 'password'"
                  class="input pr-11"
                  formControlName="password"
                  autocomplete="current-password"
                  placeholder="••••••••"
                  [attr.aria-invalid]="invalid('password')"
                />
                <button
                  type="button"
                  class="absolute top-1/2 right-2 -translate-y-1/2 rounded-md p-1.5 text-ink-400 transition hover:text-ink-700"
                  (click)="showPassword.set(!showPassword())"
                  [attr.aria-label]="showPassword() ? 'Hide password' : 'Show password'"
                >
                  @if (showPassword()) {
                    <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                         stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                      <path d="M10.6 10.6a2 2 0 0 0 2.8 2.8M9.4 5.2A9.5 9.5 0 0 1 12 5c6 0 10 7 10 7a17 17 0 0 1-3 3.7M6.6 6.6A17 17 0 0 0 2 12s4 7 10 7a9.6 9.6 0 0 0 4.3-1M2 2l20 20" />
                    </svg>
                  } @else {
                    <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                         stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                      <path d="M2 12s4-7 10-7 10 7 10 7-4 7-10 7-10-7-10-7Z" /><circle cx="12" cy="12" r="3" />
                    </svg>
                  }
                </button>
              </div>
              @if (invalid('password')) { <p class="field-error">Enter your password.</p> }
            </div>

            @if (error(); as e) {
              <div class="flex items-start gap-2.5 rounded-lg bg-rose-50 p-3 text-sm text-rose-800 ring-1 ring-rose-200">
                <svg class="mt-0.5 shrink-0" width="16" height="16" viewBox="0 0 24 24" fill="none"
                     stroke="currentColor" stroke-width="2.2" stroke-linecap="round">
                  <circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" />
                </svg>
                <span>{{ e }}</span>
              </div>
            }

            <button type="submit" class="btn-primary w-full" [disabled]="busy()">
              @if (busy()) { <app-spinner [size]="15" /> }
              {{ busy() ? 'Signing in…' : 'Sign in' }}
            </button>
          </form>

          <div class="mt-6 rounded-lg bg-ink-100 p-3.5 text-xs leading-relaxed text-ink-600">
            <p class="font-semibold text-ink-800">Default seeded account</p>
            <p class="mt-1">
              The API seeds a <code class="rounded bg-white px-1 py-0.5 font-mono">SystemAdmin</code> on first
              run. Check <code class="rounded bg-white px-1 py-0.5 font-mono">SeedData.cs</code> for the
              credentials, and change the password after signing in.
            </p>
          </div>

          <a routerLink="/" class="mt-6 inline-flex items-center gap-1.5 text-sm font-semibold text-ink-500 hover:text-ink-800">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4"
                 stroke-linecap="round" stroke-linejoin="round"><path d="m15 18-6-6 6-6" /></svg>
            Back to the storefront
          </a>
        </div>
      </div>

      <!-- ========================== art ========================== -->
      <div class="relative hidden overflow-hidden bg-ink-900 lg:block">
        <div
          class="absolute inset-0 opacity-30"
          style="background-image:radial-gradient(circle at 30% 30%, #f97316 0, transparent 50%),
                                  radial-gradient(circle at 70% 70%, #2563eb 0, transparent 45%)"
        ></div>
        <div class="relative flex h-full flex-col justify-end p-12">
          <blockquote class="max-w-md">
            <p class="text-2xl leading-snug font-bold text-white">
              Every live order, every menu change, every branch — from one console.
            </p>
            <footer class="mt-4 text-sm text-ink-400">
              Online Order Management System · .NET 8 Clean Architecture + Angular
            </footer>
          </blockquote>
        </div>
      </div>
    </div>
  `,
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly showPassword = signal(false);

  readonly form = this.fb.nonNullable.group({
    email: ['', Validators.required],
    password: ['', Validators.required],
  });

  invalid(name: 'email' | 'password'): boolean {
    const c = this.form.controls[name];
    return c.invalid && (c.touched || c.dirty);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.busy.set(true);
    this.error.set(null);

    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.busy.set(false);
        if (!this.auth.canAccessAdmin()) {
          this.error.set(
            'Signed in, but this account has no admin role (SystemAdmin, SuperAdmin, Admin or OrderTaker).',
          );
          this.auth.logout(null);
          return;
        }
        this.toast.success(`Welcome back, ${this.auth.displayName()}.`);
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/admin';
        void this.router.navigateByUrl(returnUrl);
      },
      error: (err: Error) => {
        this.busy.set(false);
        this.error.set(err.message);
      },
    });
  }
}
