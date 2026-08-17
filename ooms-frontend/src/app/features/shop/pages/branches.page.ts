import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Branch } from '../../../core/models/catalog.models';
import { CartService } from '../../../core/services/cart.service';
import { ToastService } from '../../../core/services/toast.service';
import { formatTimeOnly } from '../../../core/utils/format';
import {
  EmptyStateComponent,
  ErrorStateComponent,
  LoadingComponent,
} from '../../../shared/components/ui.components';
import { BranchContextService } from '../branch-context.service';

/**
 * Branch picker.
 *
 * Switching branches while a cart exists is a real hazard: prices and the
 * product list are branch-scoped, and `place-order` validates every line
 * against `BranchProduct`. So we warn and clear the cart on change.
 */
@Component({
  selector: 'app-branches-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, LoadingComponent, ErrorStateComponent, EmptyStateComponent],
  template: `
    <section class="mx-auto max-w-4xl px-4 py-10 sm:px-6 lg:px-8">
      <header class="mb-7">
        <h1 class="text-3xl font-bold tracking-tight text-ink-900">Choose a branch</h1>
        <p class="mt-1.5 text-sm text-ink-500">
          Menus, prices and taxes are set per branch, so pick where you would like to order from.
        </p>
      </header>

      @if (branchCtx.branches().length > 3) {
        <div class="relative mb-5">
          <svg class="pointer-events-none absolute top-1/2 left-3.5 -translate-y-1/2 text-ink-400"
               width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2">
            <circle cx="11" cy="11" r="7" /><path d="m20 20-3.5-3.5" stroke-linecap="round" />
          </svg>
          <input
            type="search"
            class="input pl-10"
            placeholder="Search by branch name or city…"
            [ngModel]="query()"
            (ngModelChange)="query.set($event)"
            aria-label="Search branches"
          />
        </div>
      }

      @if (branchCtx.loading()) {
        <app-loading label="Finding branches near you…" />
      } @else if (branchCtx.error()) {
        <app-error-state
          title="Could not load branches"
          [message]="branchCtx.error()!"
          (retry)="reload()"
        />
      } @else if (!filtered().length) {
        <app-empty-state
          title="No branches found"
          [message]="
            query()
              ? 'Nothing matched your search. Try a different name or city.'
              : 'This restaurant has no active branches yet. An administrator can add one from the admin console.'
          "
        />
      } @else {
        <ul class="grid gap-3.5">
          @for (b of filtered(); track b.branchId) {
            <li>
              <button
                type="button"
                class="group flex w-full items-start gap-4 rounded-card border-2 bg-white p-4 text-left
                       shadow-card transition-all hover:-translate-y-0.5 hover:shadow-pop"
                [class]="
                  b.branchId === branchCtx.selectedId()
                    ? 'border-brand-500 ring-2 ring-brand-100'
                    : 'border-transparent ring-1 ring-ink-900/5'
                "
                (click)="choose(b)"
              >
                <span
                  class="mt-0.5 grid h-11 w-11 shrink-0 place-items-center rounded-xl transition"
                  [class]="
                    b.branchId === branchCtx.selectedId()
                      ? 'bg-brand-500 text-white'
                      : 'bg-ink-100 text-ink-500 group-hover:bg-brand-50 group-hover:text-brand-600'
                  "
                >
                  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                       stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0Z" /><circle cx="12" cy="10" r="3" />
                  </svg>
                </span>

                <span class="min-w-0 flex-1">
                  <span class="flex flex-wrap items-center gap-2">
                    <span class="text-base font-semibold text-ink-900">{{ b.branchName }}</span>
                    @if (openState(b) !== null) {
                      <span class="badge"
                            [class]="openState(b)
                              ? 'bg-emerald-100 text-emerald-800 ring-1 ring-emerald-200'
                              : 'bg-rose-100 text-rose-700 ring-1 ring-rose-200'">
                        {{ openState(b) ? 'Open now' : 'Closed' }}
                      </span>
                    }
                    @if (b.branchId === branchCtx.selectedId()) {
                      <span class="badge bg-brand-100 text-brand-700 ring-1 ring-brand-200">Selected</span>
                    }
                  </span>

                  @if (addressLine(b); as addr) {
                    <span class="mt-1 block text-sm text-ink-500">{{ addr }}</span>
                  }

                  <span class="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs text-ink-500">
                    @if (b.phoneNumber) {
                      <span class="flex items-center gap-1.5">
                        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                             stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                          <path d="M22 16.9v3a2 2 0 0 1-2.2 2 19.8 19.8 0 0 1-8.6-3.1 19.5 19.5 0 0 1-6-6A19.8 19.8 0 0 1 2 4.2 2 2 0 0 1 4 2h3a2 2 0 0 1 2 1.7c.1 1 .4 1.9.7 2.8a2 2 0 0 1-.5 2.1L8.1 9.9a16 16 0 0 0 6 6l1.3-1.1a2 2 0 0 1 2.1-.5c.9.3 1.8.6 2.8.7a2 2 0 0 1 1.7 2Z" />
                        </svg>
                        {{ b.phoneNumber }}
                      </span>
                    }
                    @if (todaysHours(b); as hrs) {
                      <span class="flex items-center gap-1.5">
                        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                             stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                          <circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" />
                        </svg>
                        {{ hrs }}
                      </span>
                    }
                  </span>
                </span>

                <svg class="mt-3 shrink-0 text-ink-300 transition group-hover:translate-x-0.5 group-hover:text-brand-500"
                     width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                     stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round">
                  <path d="m9 6 6 6-6 6" />
                </svg>
              </button>
            </li>
          }
        </ul>
      }
    </section>
  `,
})
export class BranchesPage implements OnInit {
  readonly branchCtx = inject(BranchContextService);
  private readonly cart = inject(CartService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  readonly query = signal('');

  readonly filtered = computed(() => {
    const q = this.query().trim().toLowerCase();
    const list = this.branchCtx.branches();
    if (!q) return list;
    return list.filter((b) =>
      [b.branchName, b.branchAddress?.city, b.branchAddress?.addressLine1]
        .filter(Boolean)
        .some((v) => v!.toLowerCase().includes(q)),
    );
  });

  ngOnInit(): void {
    this.branchCtx.load().subscribe();
  }

  reload(): void {
    this.branchCtx.load(true).subscribe();
  }

  openState(b: Branch): boolean | null {
    return this.branchCtx.isOpenNow(b);
  }

  addressLine(b: Branch): string {
    const a = b.branchAddress;
    if (!a) return '';
    return [a.addressLine1, a.addressLine2, a.city, a.postalCode].filter(Boolean).join(', ');
  }

  todaysHours(b: Branch): string | null {
    const day = new Date().toLocaleDateString('en-US', { weekday: 'long' }).toLowerCase();
    const t = b.branchTimings?.find((x) => (x.branchTimingName ?? '').trim().toLowerCase() === day);
    if (!t) return null;
    if (t.isClosed) return 'Closed today';
    if (!t.openTime || !t.closeTime) return null;
    return `${formatTimeOnly(t.openTime)} – ${formatTimeOnly(t.closeTime)}`;
  }

  choose(b: Branch): void {
    const previous = this.branchCtx.selectedId();
    const switching = previous !== null && previous !== b.branchId;
    const hasItems = this.cart.count() > 0;

    if (switching && hasItems) {
      const ok = confirm(
        `Your cart has ${this.cart.count()} item(s) from another branch.\n\n` +
          'Prices and availability differ per branch, so switching will empty your cart. Continue?',
      );
      if (!ok) return;

      this.cart.clear().subscribe({
        next: () => {
          this.branchCtx.select(b.branchId);
          this.toast.info(`Now ordering from ${b.branchName}. Your cart was cleared.`);
          void this.router.navigate(['/menu']);
        },
        error: () => this.toast.error('Could not clear the cart. Please try again.'),
      });
      return;
    }

    this.branchCtx.select(b.branchId);
    this.toast.success(`Ordering from ${b.branchName}.`);
    void this.router.navigate(['/menu']);
  }
}
