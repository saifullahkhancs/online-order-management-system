import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

interface NavItem {
  path: string;
  label: string;
  icon: string;
  roles?: string[];
}

/**
 * Admin chrome: collapsible sidebar on desktop, slide-over drawer on mobile,
 * plus the account menu. Nav items are filtered by the signed-in user's roles
 * so nobody is shown a page the API would reject.
 */
@Component({
  selector: 'app-admin-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="min-h-dvh bg-ink-100">
      <!-- ======================= mobile top bar ======================= -->
      <header class="sticky top-0 z-40 flex h-14 items-center gap-3 border-b border-ink-200 bg-white px-4 lg:hidden">
        <button
          type="button"
          class="rounded-lg p-2 text-ink-600 transition hover:bg-ink-100"
          (click)="drawerOpen.set(true)"
          aria-label="Open navigation"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2"
               stroke-linecap="round"><path d="M4 6h16M4 12h16M4 18h16" /></svg>
        </button>
        <span class="font-bold tracking-tight text-ink-900">OOMS admin</span>
        <a routerLink="/" class="ml-auto text-xs font-semibold text-brand-600">View store →</a>
      </header>

      <div class="lg:flex">
        <!-- ========================= sidebar ========================= -->
        @if (drawerOpen()) {
          <div class="fixed inset-0 z-40 bg-ink-950/50 lg:hidden" (click)="drawerOpen.set(false)"></div>
        }

        <aside
          class="fixed inset-y-0 left-0 z-50 flex w-64 shrink-0 flex-col border-r border-ink-200 bg-white
                 transition-transform lg:sticky lg:top-0 lg:z-auto lg:h-dvh lg:translate-x-0"
          [class.translate-x-0]="drawerOpen()"
          [class.-translate-x-full]="!drawerOpen()"
        >
          <!-- brand -->
          <div class="flex h-16 shrink-0 items-center gap-2.5 border-b border-ink-200 px-5">
            <span class="grid h-8 w-8 place-items-center rounded-lg bg-brand-500 text-white">
              <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                   stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M3 11h18M5 11V7a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2v4M4 11v5a4 4 0 0 0 4 4h8a4 4 0 0 0 4-4v-5" />
              </svg>
            </span>
            <div class="min-w-0">
              <p class="truncate text-sm font-bold text-ink-900">OOMS admin</p>
              @if (auth.session()?.headOfficeName) {
                <p class="truncate text-[11px] text-ink-400">{{ auth.session()!.headOfficeName }}</p>
              }
            </div>
            <button
              type="button"
              class="ml-auto rounded-lg p-1.5 text-ink-400 hover:bg-ink-100 lg:hidden"
              (click)="drawerOpen.set(false)"
              aria-label="Close navigation"
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2">
                <path d="M18 6 6 18M6 6l12 12" stroke-linecap="round" />
              </svg>
            </button>
          </div>

          <!-- nav -->
          <nav class="min-h-0 flex-1 overflow-y-auto px-3 py-4">
            <p class="mb-1.5 px-3 text-[11px] font-semibold tracking-wide text-ink-400 uppercase">Operations</p>
            <ul class="space-y-0.5">
              @for (item of operations(); track item.path) {
                <li>
                  <a
                    [routerLink]="item.path"
                    routerLinkActive="bg-brand-50 text-brand-700 ring-1 ring-brand-200"
                    class="flex items-center gap-2.5 rounded-lg px-3 py-2 text-sm font-medium text-ink-600
                           transition hover:bg-ink-100 hover:text-ink-900"
                    (click)="drawerOpen.set(false)"
                  >
                    <span [innerHTML]="item.icon" class="shrink-0"></span>
                    {{ item.label }}
                  </a>
                </li>
              }
            </ul>

            @if (catalogue().length) {
              <p class="mt-5 mb-1.5 px-3 text-[11px] font-semibold tracking-wide text-ink-400 uppercase">
                Catalogue
              </p>
              <ul class="space-y-0.5">
                @for (item of catalogue(); track item.path) {
                  <li>
                    <a
                      [routerLink]="item.path"
                      routerLinkActive="bg-brand-50 text-brand-700 ring-1 ring-brand-200"
                      class="flex items-center gap-2.5 rounded-lg px-3 py-2 text-sm font-medium text-ink-600
                             transition hover:bg-ink-100 hover:text-ink-900"
                      (click)="drawerOpen.set(false)"
                    >
                      <span [innerHTML]="item.icon" class="shrink-0"></span>
                      {{ item.label }}
                    </a>
                  </li>
                }
              </ul>
            }

            @if (settings().length) {
              <p class="mt-5 mb-1.5 px-3 text-[11px] font-semibold tracking-wide text-ink-400 uppercase">
                Settings
              </p>
              <ul class="space-y-0.5">
                @for (item of settings(); track item.path) {
                  <li>
                    <a
                      [routerLink]="item.path"
                      routerLinkActive="bg-brand-50 text-brand-700 ring-1 ring-brand-200"
                      class="flex items-center gap-2.5 rounded-lg px-3 py-2 text-sm font-medium text-ink-600
                             transition hover:bg-ink-100 hover:text-ink-900"
                      (click)="drawerOpen.set(false)"
                    >
                      <span [innerHTML]="item.icon" class="shrink-0"></span>
                      {{ item.label }}
                    </a>
                  </li>
                }
              </ul>
            }
          </nav>

          <!-- account -->
          <div class="shrink-0 border-t border-ink-200 p-3">
            <a routerLink="/" class="mb-1 flex items-center gap-2.5 rounded-lg px-3 py-2 text-sm font-medium
                                     text-ink-600 transition hover:bg-ink-100 hover:text-ink-900">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
                   stroke-linecap="round" stroke-linejoin="round">
                <path d="M3 9h18M3 9l1.5 10a2 2 0 0 0 2 1.8h11a2 2 0 0 0 2-1.8L21 9M3 9l2.2-4.4A2 2 0 0 1 7 3.5h10a2 2 0 0 1 1.8 1.1L21 9" />
              </svg>
              View storefront
            </a>

            <div class="flex items-center gap-2.5 rounded-lg bg-ink-50 px-3 py-2.5">
              <span class="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-ink-800 text-xs font-bold text-white">
                {{ initials() }}
              </span>
              <div class="min-w-0 flex-1">
                <p class="truncate text-xs font-semibold text-ink-900">{{ auth.displayName() }}</p>
                <p class="truncate text-[11px] text-ink-500">{{ auth.roles().join(', ') || 'No role' }}</p>
              </div>
              <button
                type="button"
                class="shrink-0 rounded-md p-1.5 text-ink-400 transition hover:bg-white hover:text-danger"
                (click)="auth.logout()"
                aria-label="Sign out"
                title="Sign out"
              >
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
                     stroke-linecap="round" stroke-linejoin="round">
                  <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4M16 17l5-5-5-5M21 12H9" />
                </svg>
              </button>
            </div>
          </div>
        </aside>

        <!-- ========================= content ========================= -->
        <main class="min-w-0 flex-1">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
})
export class AdminShell {
  readonly auth = inject(AuthService);
  readonly drawerOpen = signal(false);

  private readonly nav: { group: 'ops' | 'catalogue' | 'settings'; item: NavItem }[] = [
    {
      group: 'ops',
      item: {
        path: 'orders',
        label: 'Orders',
        icon: icon(
          '<path d="M9 3h6l1 3H8ZM5 6h14l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2Z"/><path d="m9 12 2 2 4-4"/>',
        ),
      },
    },
    {
      group: 'catalogue',
      item: {
        path: 'products',
        label: 'Products',
        roles: ['SystemAdmin', 'SuperAdmin', 'Admin'],
        icon: icon('<path d="M21 8 12 3 3 8v8l9 5 9-5Z"/><path d="m3 8 9 5 9-5M12 13v8"/>'),
      },
    },
    {
      group: 'catalogue',
      item: {
        path: 'categories',
        label: 'Categories',
        roles: ['SystemAdmin', 'SuperAdmin', 'Admin'],
        icon: icon('<rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5"/>'),
      },
    },
    {
      group: 'catalogue',
      item: {
        path: 'menu',
        label: 'Branch menu',
        roles: ['SystemAdmin', 'SuperAdmin', 'Admin'],
        icon: icon('<path d="M4 6h16M4 12h16M4 18h10"/>'),
      },
    },
    {
      group: 'catalogue',
      item: {
        path: 'modifiers',
        label: 'Modifiers & add-ons',
        roles: ['SystemAdmin', 'SuperAdmin', 'Admin'],
        icon: icon('<path d="M4 21v-7M4 10V3M12 21v-9M12 8V3M20 21v-5M20 12V3M1 14h6M9 8h6M17 16h6"/>'),
      },
    },
    {
      group: 'settings',
      item: {
        path: 'branches',
        label: 'Branches',
        roles: ['SystemAdmin', 'SuperAdmin', 'Admin'],
        icon: icon('<path d="M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0Z"/><circle cx="12" cy="10" r="3"/>'),
      },
    },
    {
      group: 'settings',
      item: {
        path: 'taxes',
        label: 'Taxes',
        roles: ['SystemAdmin', 'SuperAdmin', 'Admin'],
        icon: icon('<path d="M19 5 5 19M6.5 8a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3ZM17.5 19a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3Z"/>'),
      },
    },
    {
      group: 'settings',
      item: {
        path: 'users',
        label: 'Users & roles',
        roles: ['SystemAdmin', 'SuperAdmin', 'Admin'],
        icon: icon('<path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.9"/>'),
      },
    },
  ];

  private visible(group: 'ops' | 'catalogue' | 'settings'): NavItem[] {
    return this.nav
      .filter((n) => n.group === group)
      .map((n) => n.item)
      .filter((i) => !i.roles || this.auth.hasAnyRole(i.roles));
  }

  readonly operations = computed(() => this.visible('ops'));
  readonly catalogue = computed(() => this.visible('catalogue'));
  readonly settings = computed(() => this.visible('settings'));

  readonly initials = computed(() => {
    const name = this.auth.displayName();
    return name.slice(0, 2).toUpperCase();
  });
}

function icon(paths: string): string {
  return `<svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">${paths}</svg>`;
}
