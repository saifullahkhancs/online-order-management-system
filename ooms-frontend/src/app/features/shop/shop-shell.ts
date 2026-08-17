import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { MoneyPipe } from '../../core/utils/format';
import { BranchContextService } from './branch-context.service';

/**
 * Storefront chrome: sticky header with branch picker and cart button,
 * a mobile bottom bar, and the footer.
 */
@Component({
  selector: 'app-shop-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MoneyPipe],
  template: `
    <div class="flex min-h-dvh flex-col bg-ink-50">
      <!-- ================= header ================= -->
      <header class="sticky top-0 z-40 border-b border-ink-200/80 bg-white/85 backdrop-blur-md">
        <div class="mx-auto flex h-16 max-w-7xl items-center gap-3 px-4 sm:px-6 lg:px-8">
          <a routerLink="/" class="flex shrink-0 items-center gap-2.5">
            <span
              class="grid h-9 w-9 place-items-center rounded-xl bg-brand-500 text-white shadow-sm"
              aria-hidden="true"
            >
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                   stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M3 11h18M5 11V7a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2v4M4 11v5a4 4 0 0 0 4 4h8a4 4 0 0 0 4-4v-5" />
              </svg>
            </span>
            <span class="hidden text-lg leading-none font-bold tracking-tight text-ink-900 sm:block">
              OOMS
            </span>
          </a>

          <!-- branch picker -->
          <a
            routerLink="/branches"
            class="group flex min-w-0 items-center gap-2 rounded-lg px-2.5 py-1.5 text-left transition hover:bg-ink-100"
          >
            <svg class="shrink-0 text-brand-500" width="16" height="16" viewBox="0 0 24 24" fill="none"
                 stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0Z" /><circle cx="12" cy="10" r="3" />
            </svg>
            <span class="min-w-0">
              <span class="block text-[10px] leading-none font-semibold tracking-wide text-ink-400 uppercase">
                Delivering from
              </span>
              <span class="block truncate text-sm font-semibold text-ink-800">
                {{ branchCtx.selected()?.branchName ?? 'Choose a branch' }}
              </span>
            </span>
            <svg class="shrink-0 text-ink-400 transition group-hover:text-ink-600" width="14" height="14"
                 viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round">
              <path d="m6 9 6 6 6-6" />
            </svg>
          </a>

          <nav class="ml-auto hidden items-center gap-1 md:flex">
            <a routerLink="/menu" routerLinkActive="bg-ink-100 text-ink-900" class="btn-ghost">Menu</a>
            <a routerLink="/orders" routerLinkActive="bg-ink-100 text-ink-900" class="btn-ghost">My orders</a>
            @if (auth.canAccessAdmin()) {
              <a routerLink="/admin" class="btn-ghost">Admin</a>
            } @else {
              <a routerLink="/auth/login" class="btn-ghost">Staff sign in</a>
            }
          </nav>

          <!-- cart -->
          <a
            routerLink="/cart"
            class="relative ml-auto flex items-center gap-2 rounded-lg bg-ink-900 px-3 py-2 text-white
                   transition hover:bg-ink-800 md:ml-0"
            [attr.aria-label]="'Cart, ' + cart.count() + ' items'"
          >
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                 stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <circle cx="9" cy="20" r="1.4" /><circle cx="18" cy="20" r="1.4" />
              <path d="M1 1h3.2l2.5 12.4a2 2 0 0 0 2 1.6h8.8a2 2 0 0 0 2-1.6L21 6H5.5" />
            </svg>
            <span class="hidden text-sm font-semibold tabular-nums sm:block">
              {{ cart.grandTotal() | money }}
            </span>
            @if (cart.count() > 0) {
              <span
                class="absolute -top-1.5 -right-1.5 grid h-5 min-w-5 place-items-center rounded-full
                       bg-brand-500 px-1 text-[11px] font-bold text-white ring-2 ring-white"
              >
                {{ cart.count() }}
              </span>
            }
          </a>
        </div>
      </header>

      <!-- ================= content ================= -->
      <main class="flex-1 pb-20 md:pb-0">
        <router-outlet />
      </main>

      <!-- ================= footer ================= -->
      <footer class="mt-auto hidden border-t border-ink-200 bg-white md:block">
        <div class="mx-auto flex max-w-7xl flex-col gap-4 px-4 py-8 sm:flex-row sm:items-center sm:justify-between sm:px-6 lg:px-8">
          <div>
            <p class="text-sm font-semibold text-ink-800">Online Order Management System</p>
            <p class="mt-1 text-xs text-ink-500">
              Angular storefront for the Restaurant .NET 8 Clean Architecture API.
            </p>
          </div>
          <div class="flex gap-4 text-xs text-ink-500">
            <a routerLink="/menu" class="hover:text-ink-800">Menu</a>
            <a routerLink="/branches" class="hover:text-ink-800">Branches</a>
            <a routerLink="/orders" class="hover:text-ink-800">Track an order</a>
            <a routerLink="/auth/login" class="hover:text-ink-800">Staff</a>
          </div>
        </div>
      </footer>

      <!-- ============ mobile bottom bar ============ -->
      <nav
        class="fixed inset-x-0 bottom-0 z-40 grid grid-cols-4 border-t border-ink-200 bg-white/95 backdrop-blur md:hidden"
      >
        <a routerLink="/" routerLinkActive="text-brand-600" [routerLinkActiveOptions]="{ exact: true }"
           class="flex flex-col items-center gap-0.5 py-2.5 text-[11px] font-medium text-ink-500">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
               stroke-linecap="round" stroke-linejoin="round">
            <path d="m3 10 9-7 9 7v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2Z" /><path d="M9 21v-7h6v7" />
          </svg>
          Home
        </a>
        <a routerLink="/menu" routerLinkActive="text-brand-600"
           class="flex flex-col items-center gap-0.5 py-2.5 text-[11px] font-medium text-ink-500">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
               stroke-linecap="round" stroke-linejoin="round">
            <path d="M4 6h16M4 12h16M4 18h10" />
          </svg>
          Menu
        </a>
        <a routerLink="/cart" routerLinkActive="text-brand-600"
           class="relative flex flex-col items-center gap-0.5 py-2.5 text-[11px] font-medium text-ink-500">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
               stroke-linecap="round" stroke-linejoin="round">
            <circle cx="9" cy="20" r="1.4" /><circle cx="18" cy="20" r="1.4" />
            <path d="M1 1h3.2l2.5 12.4a2 2 0 0 0 2 1.6h8.8a2 2 0 0 0 2-1.6L21 6H5.5" />
          </svg>
          Cart
          @if (cart.count() > 0) {
            <span class="absolute top-1.5 right-[22%] grid h-4 min-w-4 place-items-center rounded-full
                         bg-brand-500 px-1 text-[10px] font-bold text-white">
              {{ cart.count() }}
            </span>
          }
        </a>
        <a routerLink="/orders" routerLinkActive="text-brand-600"
           class="flex flex-col items-center gap-0.5 py-2.5 text-[11px] font-medium text-ink-500">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
               stroke-linecap="round" stroke-linejoin="round">
            <path d="M9 3h6l1 3H8ZM5 6h14l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2Z" /><path d="m9 12 2 2 4-4" />
          </svg>
          Orders
        </a>
      </nav>
    </div>
  `,
})
export class ShopShell implements OnInit {
  readonly branchCtx = inject(BranchContextService);
  readonly cart = inject(CartService);
  readonly auth = inject(AuthService);

  ngOnInit(): void {
    // Warm the branch list and hydrate the cart badge on first paint.
    this.branchCtx.load().subscribe();
    this.cart.refresh().subscribe();
  }
}
