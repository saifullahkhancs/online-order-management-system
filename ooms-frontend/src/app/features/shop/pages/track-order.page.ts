import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Order, OrderStatusId } from '../../../core/models/order.models';
import { AuthService } from '../../../core/services/auth.service';
import { CartService } from '../../../core/services/cart.service';
import { OrderService } from '../../../core/services/order.service';
import { ToastService } from '../../../core/services/toast.service';
import {
  HumanisePipe,
  MoneyPipe,
  StatusClassPipe,
  WhenPipe,
  humanise,
  trackingSteps,
} from '../../../core/utils/format';
import {
  ErrorStateComponent,
  ImgComponent,
  LoadingComponent,
  SpinnerComponent,
} from '../../../shared/components/ui.components';

/**
 * Order tracking.
 *
 * The API has no push channel for customers (SignalR is admin-only), so this
 * page polls `GET /api/Order/order-status/{id}` every 20 s until the order
 * reaches a terminal state, then stops. Polling also pauses while the tab is
 * hidden so we do not hammer the API in a background tab.
 *
 * `orderId` is bound straight from the route via withComponentInputBinding().
 */
@Component({
  selector: 'app-track-order-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    MoneyPipe,
    WhenPipe,
    StatusClassPipe,
    HumanisePipe,
    ImgComponent,
    LoadingComponent,
    ErrorStateComponent,
    SpinnerComponent,
  ],
  template: `
    <section class="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
      @if (justPlaced()) {
        <div class="animate-fade-up mb-6 flex items-start gap-3 rounded-card bg-emerald-50 p-4 ring-1 ring-emerald-200">
          <span class="grid h-9 w-9 shrink-0 place-items-center rounded-full bg-emerald-500 text-white">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3"
                 stroke-linecap="round" stroke-linejoin="round"><path d="m20 6-11 11-5-5" /></svg>
          </span>
          <div>
            <h2 class="text-sm font-bold text-emerald-900">Order placed</h2>
            <p class="mt-0.5 text-sm text-emerald-800">
              The kitchen has your order. This page updates itself as it progresses.
            </p>
          </div>
        </div>
      }

      @if (loading()) {
        <app-loading label="Fetching your order…" />
      } @else if (error()) {
        <app-error-state title="Could not load this order" [message]="error()!" (retry)="load()" />
      } @else if (order(); as o) {
        <!-- ========================== header ========================== -->
        <header class="mb-6 flex flex-wrap items-start justify-between gap-4">
          <div>
            <p class="text-xs font-semibold tracking-wide text-ink-400 uppercase">Order</p>
            <h1 class="text-2xl font-bold tracking-tight text-ink-900">
              #{{ o.orderNumber?.slice(0, 8) ?? o.orderId }}
            </h1>
            <p class="mt-1 text-sm text-ink-500">Placed {{ o.createdAt | when }}</p>
          </div>
          <div class="text-right">
            <span class="badge" [class]="o.orderStatus | statusClass">
              {{ o.orderStatus | humanise }}
            </span>
            @if (polling()) {
              <p class="mt-2 flex items-center justify-end gap-1.5 text-[11px] text-ink-400">
                <app-spinner [size]="10" /> live
              </p>
            }
          </div>
        </header>

        <!-- ========================= progress ========================= -->
        @if (!isTerminal()) {
          <div class="card mb-5 p-5">
            <ol class="flex items-start">
              @for (step of steps(); track step; let i = $index; let last = $last) {
                <li class="flex flex-1 flex-col items-center" [class.flex-none]="last">
                  <div class="flex w-full items-center">
                    <span
                      class="grid h-8 w-8 shrink-0 place-items-center rounded-full text-xs font-bold transition"
                      [class]="
                        i <= currentStepIndex()
                          ? 'bg-brand-500 text-white'
                          : 'bg-ink-100 text-ink-400 ring-1 ring-ink-200'
                      "
                    >
                      @if (i < currentStepIndex()) {
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                             stroke-width="3.2" stroke-linecap="round" stroke-linejoin="round">
                          <path d="m20 6-11 11-5-5" />
                        </svg>
                      } @else {
                        {{ i + 1 }}
                      }
                    </span>
                    @if (!last) {
                      <span class="h-0.5 flex-1 transition"
                            [class]="i < currentStepIndex() ? 'bg-brand-500' : 'bg-ink-200'"></span>
                    }
                  </div>
                  <span class="mt-2 max-w-20 text-center text-[11px] leading-tight font-medium"
                        [class]="i <= currentStepIndex() ? 'text-ink-800' : 'text-ink-400'">
                    {{ label(step) }}
                  </span>
                </li>
              }
            </ol>
          </div>
        }

        <!-- ======================= order detail ======================= -->
        <div class="card divide-y divide-ink-100">
          <!-- type + fulfilment -->
          <div class="grid gap-4 p-5 sm:grid-cols-2">
            <div>
              <p class="text-[11px] font-semibold tracking-wide text-ink-400 uppercase">Order type</p>
              <p class="mt-1 text-sm font-semibold text-ink-800">{{ o.orderType | humanise }}</p>
            </div>
            <div>
              <p class="text-[11px] font-semibold tracking-wide text-ink-400 uppercase">Payment</p>
              <p class="mt-1 text-sm font-semibold text-ink-800">
                {{ o.paymentStatus | humanise }} · Cash
              </p>
            </div>
            @if (o.deliveryAddress) {
              <div class="sm:col-span-2">
                <p class="text-[11px] font-semibold tracking-wide text-ink-400 uppercase">Delivering to</p>
                <p class="mt-1 text-sm text-ink-700">{{ o.deliveryAddress }}</p>
                @if (o.deliveryInstructions) {
                  <p class="mt-1 text-xs text-ink-500">Note: {{ o.deliveryInstructions }}</p>
                }
              </div>
            }
            @if (o.guestName) {
              <div class="sm:col-span-2">
                <p class="text-[11px] font-semibold tracking-wide text-ink-400 uppercase">Contact</p>
                <p class="mt-1 text-sm text-ink-700">{{ o.guestName }} · {{ o.guestPhoneNumber }}</p>
              </div>
            }
          </div>

          <!-- items -->
          <ul class="divide-y divide-ink-100">
            @for (item of o.orderItems; track item.orderItemId) {
              <li class="flex gap-3 p-4">
                <div class="h-14 w-14 shrink-0 overflow-hidden rounded-lg bg-ink-100">
                  <app-img [src]="item.imageUrl" [alt]="item.productName ?? ''" />
                </div>
                <div class="min-w-0 flex-1">
                  <p class="text-sm font-semibold text-ink-900">
                    {{ item.quantity }}× {{ item.productName }}
                  </p>
                  @if (item.itemModifiers.length || item.itemAddons.length) {
                    <p class="mt-0.5 text-xs text-ink-500">
                      {{ options(item.itemModifiers, item.itemAddons) }}
                    </p>
                  }
                  @if (item.instructions) {
                    <p class="mt-1 text-xs text-amber-700">Note: {{ item.instructions }}</p>
                  }
                </div>
                <span class="shrink-0 text-sm font-semibold tabular-nums text-ink-800">
                  {{ item.finalPrice | money }}
                </span>
              </li>
            }
          </ul>

          <!-- totals -->
          <dl class="space-y-2 p-5 text-sm">
            <div class="flex justify-between">
              <dt class="text-ink-500">Subtotal</dt>
              <dd class="font-semibold tabular-nums text-ink-800">{{ o.subtotal | money }}</dd>
            </div>
            @for (t of o.orderTaxes; track t.orderTaxId) {
              <div class="flex justify-between">
                <dt class="text-ink-500">{{ t.taxName }}</dt>
                <dd class="font-semibold tabular-nums text-ink-800">{{ t.taxAmount | money }}</dd>
              </div>
            }
            @if (o.deliveryFee) {
              <div class="flex justify-between">
                <dt class="text-ink-500">Delivery</dt>
                <dd class="font-semibold tabular-nums text-ink-800">{{ o.deliveryFee | money }}</dd>
              </div>
            }
            <div class="flex justify-between border-t border-ink-100 pt-2.5">
              <dt class="text-base font-bold text-ink-900">Total</dt>
              <dd class="text-base font-bold tabular-nums text-brand-600">{{ o.totalAmount | money }}</dd>
            </div>
          </dl>
        </div>

        <!-- ========================== actions ========================== -->
        <div class="mt-5 flex flex-wrap gap-2">
          <a routerLink="/menu" class="btn-secondary">Order something else</a>
          <a routerLink="/orders" class="btn-ghost">All my orders</a>
          @if (canCancel()) {
            <button type="button" class="btn-ghost ml-auto text-danger hover:bg-rose-50"
                    [disabled]="cancelling()" (click)="cancel()">
              @if (cancelling()) { <app-spinner [size]="14" /> }
              Cancel order
            </button>
          }
        </div>
      }
    </section>
  `,
})
export class TrackOrderPage implements OnInit, OnDestroy {
  /** Bound from the `:orderId` route param. */
  readonly orderId = input.required<string>();

  private readonly orders = inject(OrderService);
  private readonly auth = inject(AuthService);
  private readonly cart = inject(CartService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);

  readonly order = signal<Order | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly polling = signal(false);
  readonly cancelling = signal(false);
  readonly justPlaced = signal(false);

  private timer: ReturnType<typeof setInterval> | null = null;
  private readonly onVisibility = () => (document.hidden ? this.stopPolling() : this.startPolling());

  readonly steps = computed(() => trackingSteps(this.order()?.orderTypeId));

  readonly currentStepIndex = computed(() => {
    const status = (this.order()?.orderStatus ?? '').replace(/\s+/g, '');
    const id = OrderStatusId[status as keyof typeof OrderStatusId];
    const idx = this.steps().indexOf(id);
    return idx >= 0 ? idx : 0;
  });

  readonly isTerminal = computed(() => {
    const s = (this.order()?.orderStatus ?? '').replace(/\s+/g, '');
    return ['Delivered', 'Completed', 'Cancelled', 'Rejected', 'Refunded'].includes(s);
  });

  /** The API only allows cancelling while Pending or Confirmed. */
  readonly canCancel = computed(() => {
    const s = (this.order()?.orderStatus ?? '').replace(/\s+/g, '');
    return s === 'Pending' || s === 'Confirmed';
  });

  ngOnInit(): void {
    this.justPlaced.set(this.route.snapshot.queryParamMap.get('placed') === '1');
    this.load();
    document.addEventListener('visibilitychange', this.onVisibility);
  }

  ngOnDestroy(): void {
    this.stopPolling();
    document.removeEventListener('visibilitychange', this.onVisibility);
  }

  load(): void {
    const id = Number(this.orderId());
    if (!id) {
      this.error.set('That order reference is not valid.');
      this.loading.set(false);
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    this.orders
      .orderDetails(id, {
        customerId: this.auth.session()?.userId ?? 0,
        guestToken: this.auth.isAuthenticated() ? null : this.cart.guestToken(),
      })
      .subscribe({
        next: (o) => {
          this.order.set(o);
          this.loading.set(false);
          if (!this.isTerminal()) this.startPolling();
        },
        error: (err: Error) => {
          this.error.set(err.message);
          this.loading.set(false);
        },
      });
  }

  private startPolling(): void {
    if (this.timer || this.isTerminal() || document.hidden) return;
    this.polling.set(true);
    this.timer = setInterval(() => this.pollStatus(), 20_000);
  }

  private stopPolling(): void {
    if (this.timer) clearInterval(this.timer);
    this.timer = null;
    this.polling.set(false);
  }

  /** Cheap poll: only the status, not the whole order. */
  private pollStatus(): void {
    const id = Number(this.orderId());
    this.orders.orderStatus(id).subscribe({
      next: (snap) => {
        const current = this.order();
        if (!current || !snap?.orderStatus) return;
        if (snap.orderStatus !== current.orderStatus) {
          this.order.set({ ...current, orderStatus: snap.orderStatus });
          this.toast.info(`Your order is now ${humanise(snap.orderStatus)}.`);
          if (this.isTerminal()) this.stopPolling();
        }
      },
      error: () => this.stopPolling(),
    });
  }

  label(step: OrderStatusId): string {
    return humanise(OrderStatusId[step]);
  }

  options(
    mods: { modifierName?: string | null }[],
    adds: { addonName?: string | null; addonQuantity?: number | null }[],
  ): string {
    return [
      ...mods.map((m) => m.modifierName),
      ...adds.map((a) => `${a.addonQuantity}× ${a.addonName}`),
    ]
      .filter(Boolean)
      .join(', ');
  }

  cancel(): void {
    if (!confirm('Cancel this order? This cannot be undone.')) return;
    const id = Number(this.orderId());
    this.cancelling.set(true);
    this.orders
      .cancelOrder(id, {
        guestSessionToken: this.auth.isAuthenticated() ? null : this.cart.guestToken(),
        cancelReason: 'Cancelled by customer',
      })
      .subscribe({
        next: () => {
          this.cancelling.set(false);
          this.toast.success('Your order was cancelled.');
          this.stopPolling();
          this.load();
        },
        error: (err: Error) => {
          this.cancelling.set(false);
          this.toast.error(err.message);
        },
      });
  }
}
