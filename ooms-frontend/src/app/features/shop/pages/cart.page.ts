import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CartItem } from '../../../core/models/cart.models';
import { CartService } from '../../../core/services/cart.service';
import { MoneyPipe } from '../../../core/utils/format';
import {
  EmptyStateComponent,
  ImgComponent,
  LoadingComponent,
  SpinnerComponent,
} from '../../../shared/components/ui.components';
import { BranchContextService } from '../branch-context.service';

/**
 * Cart review.
 *
 * Quantity changes go through CartService.setQuantity, which re-posts the whole
 * line (the API's AddItem is an absolute-quantity upsert) and then re-reads the
 * server cart so the tax and grand total always come from the backend.
 */
@Component({
  selector: 'app-cart-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, MoneyPipe, ImgComponent, LoadingComponent, EmptyStateComponent, SpinnerComponent],
  template: `
    <section class="mx-auto max-w-5xl px-4 py-8 sm:px-6 lg:px-8">
      <header class="mb-6 flex items-end justify-between gap-4">
        <div>
          <h1 class="text-3xl font-bold tracking-tight text-ink-900">Your cart</h1>
          @if (branchCtx.selected(); as b) {
            <p class="mt-1.5 text-sm text-ink-500">
              From <span class="font-semibold text-ink-700">{{ b.branchName }}</span>
            </p>
          }
        </div>
        @if (!cart.isEmpty()) {
          <button type="button" class="btn-ghost btn-sm text-danger hover:bg-rose-50"
                  [disabled]="cart.mutating()" (click)="confirmClear.set(true)">
            Clear cart
          </button>
        }
      </header>

      @if (cart.loading() && cart.isEmpty()) {
        <app-loading label="Loading your cart…" />
      } @else if (cart.isEmpty()) {
        <div class="card">
          <app-empty-state
            title="Your cart is empty"
            message="Browse the menu and add a few things — you can check out as a guest, no account needed."
          >
            <a routerLink="/menu" class="btn-primary mt-2">Browse the menu</a>
          </app-empty-state>
        </div>
      } @else {
        <div class="lg:grid lg:grid-cols-[1fr_20rem] lg:items-start lg:gap-6">
          <!-- ======================= line items ======================= -->
          <ul class="card divide-y divide-ink-100 overflow-hidden">
            @for (item of cart.items(); track item.cartItemId) {
              <li class="flex gap-4 p-4">
                <div class="h-20 w-20 shrink-0 overflow-hidden rounded-lg bg-ink-100">
                  <app-img [src]="item.productImageUrl" [alt]="item.productName" />
                </div>

                <div class="min-w-0 flex-1">
                  <div class="flex items-start justify-between gap-3">
                    <h3 class="text-sm font-semibold text-ink-900">{{ item.productName }}</h3>
                    <span class="shrink-0 text-sm font-bold tabular-nums text-ink-900">
                      {{ cart.lineTotal(item) | money }}
                    </span>
                  </div>

                  <p class="mt-0.5 text-xs text-ink-500">{{ item.unitPrice | money }} each</p>

                  @if (item.modifiers.length || item.addons.length) {
                    <ul class="mt-2 space-y-0.5 text-xs text-ink-500">
                      @for (m of item.modifiers; track m.modifierId) {
                        <li class="flex items-center gap-1.5">
                          <span class="h-1 w-1 rounded-full bg-ink-300"></span>
                          {{ m.modifierName }}
                          @if (m.modifierPrice) { <span class="text-ink-400">+{{ m.modifierPrice | money }}</span> }
                        </li>
                      }
                      @for (a of item.addons; track a.addOnId) {
                        <li class="flex items-center gap-1.5">
                          <span class="h-1 w-1 rounded-full bg-brand-300"></span>
                          {{ a.addOnQuantity }}× {{ a.addOnName }}
                          @if (a.addOnPrice) {
                            <span class="text-ink-400">+{{ a.addOnPrice * a.addOnQuantity | money }}</span>
                          }
                        </li>
                      }
                    </ul>
                  }

                  @if (item.instructions) {
                    <p class="mt-2 rounded-md bg-amber-50 px-2 py-1.5 text-xs text-amber-800 ring-1 ring-amber-100">
                      <span class="font-semibold">Note:</span> {{ item.instructions }}
                    </p>
                  }

                  <div class="mt-3 flex items-center gap-3">
                    <div class="flex items-center rounded-lg ring-1 ring-ink-200">
                      <button
                        type="button"
                        class="grid h-8 w-8 place-items-center rounded-l-lg text-ink-600 transition hover:bg-ink-100 disabled:opacity-40"
                        [disabled]="cart.mutating()"
                        (click)="dec(item)"
                        [attr.aria-label]="'Decrease ' + item.productName"
                      >
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.8">
                          <path d="M5 12h14" stroke-linecap="round" />
                        </svg>
                      </button>
                      <span class="w-8 text-center text-sm font-bold tabular-nums">{{ item.quantity }}</span>
                      <button
                        type="button"
                        class="grid h-8 w-8 place-items-center rounded-r-lg text-ink-600 transition hover:bg-ink-100 disabled:opacity-40"
                        [disabled]="cart.mutating()"
                        (click)="inc(item)"
                        [attr.aria-label]="'Increase ' + item.productName"
                      >
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.8">
                          <path d="M12 5v14M5 12h14" stroke-linecap="round" />
                        </svg>
                      </button>
                    </div>

                    <button
                      type="button"
                      class="text-xs font-semibold text-ink-400 transition hover:text-danger disabled:opacity-40"
                      [disabled]="cart.mutating()"
                      (click)="remove(item)"
                    >
                      Remove
                    </button>

                    <a
                      [routerLink]="['/menu']"
                      [queryParams]="{ product: item.productId }"
                      class="text-xs font-semibold text-brand-600 transition hover:text-brand-700"
                    >
                      Edit options
                    </a>
                  </div>
                </div>
              </li>
            }
          </ul>

          <!-- ========================= summary ========================= -->
          <aside class="card mt-5 p-5 lg:sticky lg:top-24 lg:mt-0">
            <h2 class="text-sm font-semibold text-ink-900">Order summary</h2>

            <dl class="mt-4 space-y-2.5 text-sm">
              <div class="flex justify-between">
                <dt class="text-ink-500">Subtotal</dt>
                <dd class="font-semibold tabular-nums text-ink-800">{{ cart.subTotal() | money }}</dd>
              </div>

              @for (t of cart.taxes(); track t.taxId) {
                <div class="flex justify-between">
                  <dt class="text-ink-500">
                    {{ t.name }}
                    @if (t.isPercentage) { <span class="text-ink-400">({{ t.rate }}%)</span> }
                  </dt>
                  <dd class="font-semibold tabular-nums text-ink-800">{{ t.amount | money }}</dd>
                </div>
              }

              <div class="flex justify-between border-t border-ink-100 pt-3">
                <dt class="text-base font-bold text-ink-900">Total</dt>
                <dd class="text-base font-bold tabular-nums text-brand-600">{{ cart.grandTotal() | money }}</dd>
              </div>
            </dl>

            <a routerLink="/checkout" class="btn-primary mt-5 w-full">
              @if (cart.mutating()) { <app-spinner [size]="15" /> }
              Continue to checkout
            </a>

            <a routerLink="/menu" class="btn-ghost mt-2 w-full">Add more items</a>

            <p class="mt-4 flex items-start gap-2 text-[11px] leading-relaxed text-ink-400">
              <svg class="mt-px shrink-0" width="13" height="13" viewBox="0 0 24 24" fill="none"
                   stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                <rect x="3" y="11" width="18" height="11" rx="2" /><path d="M7 11V7a5 5 0 0 1 10 0v4" />
              </svg>
              Totals and taxes are calculated by the server for this branch, so what you see here is
              exactly what you will be charged.
            </p>
          </aside>
        </div>
      }
    </section>

    <!-- clear-cart confirmation -->
    @if (confirmClear()) {
      <div class="fixed inset-0 z-50 grid place-items-center bg-ink-950/50 p-4 backdrop-blur-[2px]"
           (click)="confirmClear.set(false)">
        <div class="animate-fade-up w-full max-w-sm rounded-card bg-white p-5 shadow-pop" (click)="$event.stopPropagation()">
          <h2 class="text-base font-semibold text-ink-900">Clear your cart?</h2>
          <p class="mt-1.5 text-sm text-ink-500">All {{ cart.count() }} item(s) will be removed.</p>
          <div class="mt-5 flex justify-end gap-2">
            <button type="button" class="btn-secondary" (click)="confirmClear.set(false)">Keep them</button>
            <button type="button" class="btn-danger" [disabled]="cart.mutating()" (click)="clear()">
              @if (cart.mutating()) { <app-spinner [size]="14" /> }
              Clear cart
            </button>
          </div>
        </div>
      </div>
    }
  `,
})
export class CartPage implements OnInit {
  readonly cart = inject(CartService);
  readonly branchCtx = inject(BranchContextService);
  readonly confirmClear = signal(false);

  ngOnInit(): void {
    this.branchCtx.load().subscribe();
    this.cart.refresh().subscribe();
  }

  inc(item: CartItem): void {
    this.cart.increment(item).subscribe();
  }

  dec(item: CartItem): void {
    this.cart.decrement(item).subscribe();
  }

  remove(item: CartItem): void {
    this.cart.removeItem(item.cartItemId).subscribe();
  }

  clear(): void {
    this.cart.clear().subscribe(() => this.confirmClear.set(false));
  }
}
