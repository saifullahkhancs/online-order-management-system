import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { OrderService, RecentOrderRef } from '../../../core/services/order.service';
import { ToastService } from '../../../core/services/toast.service';
import { MoneyPipe, WhenPipe } from '../../../core/utils/format';
import { EmptyStateComponent } from '../../../shared/components/ui.components';

/**
 * "My orders".
 *
 * There is no `GET /api/Order/mine` endpoint, and guests have no account, so
 * this page lists the order references we stored locally at checkout and also
 * offers a lookup box for an order id from another device.
 */
@Component({
  selector: 'app-my-orders-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, FormsModule, MoneyPipe, WhenPipe, EmptyStateComponent],
  template: `
    <section class="mx-auto max-w-3xl px-4 py-8 sm:px-6 lg:px-8">
      <header class="mb-6">
        <h1 class="text-3xl font-bold tracking-tight text-ink-900">My orders</h1>
        <p class="mt-1.5 text-sm text-ink-500">
          Orders placed from this device. Tracking works without an account.
        </p>
      </header>

      <!-- ======================= lookup ======================= -->
      <form class="card mb-6 flex flex-wrap items-end gap-3 p-4" (ngSubmit)="lookup()">
        <div class="min-w-48 flex-1">
          <label class="label" for="lookupId">Track an order by number</label>
          <input
            id="lookupId"
            type="number"
            min="1"
            class="input"
            placeholder="e.g. 1042"
            [ngModel]="lookupId()"
            (ngModelChange)="lookupId.set($event)"
            name="lookupId"
          />
        </div>
        <button type="submit" class="btn-secondary">Track</button>
      </form>

      <!-- ======================= list ======================= -->
      @if (!orders().length) {
        <div class="card">
          <app-empty-state
            title="No orders yet"
            message="Once you place an order it will appear here so you can track it."
          >
            <a routerLink="/menu" class="btn-primary mt-2">Browse the menu</a>
          </app-empty-state>
        </div>
      } @else {
        <ul class="space-y-3">
          @for (o of orders(); track o.orderId) {
            <li>
              <a
                [routerLink]="['/orders', o.orderId]"
                class="group card flex items-center gap-4 p-4 transition-all hover:-translate-y-0.5 hover:shadow-pop"
              >
                <span class="grid h-11 w-11 shrink-0 place-items-center rounded-xl bg-brand-50 text-brand-600">
                  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                       stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M9 3h6l1 3H8ZM5 6h14l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2Z" />
                    <path d="m9 12 2 2 4-4" />
                  </svg>
                </span>

                <span class="min-w-0 flex-1">
                  <span class="block text-sm font-semibold text-ink-900">
                    Order #{{ o.orderNumber.slice(0, 8) }}
                  </span>
                  <span class="mt-0.5 block text-xs text-ink-500">
                    {{ o.placedAt | when }}
                    @if (o.branchName) { · {{ o.branchName }} }
                  </span>
                </span>

                <span class="shrink-0 text-sm font-bold tabular-nums text-ink-900">{{ o.total | money }}</span>

                <svg class="shrink-0 text-ink-300 transition group-hover:translate-x-0.5 group-hover:text-brand-500"
                     width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                     stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round">
                  <path d="m9 6 6 6-6 6" />
                </svg>
              </a>
            </li>
          }
        </ul>
      }
    </section>
  `,
})
export class MyOrdersPage implements OnInit {
  private readonly orderService = inject(OrderService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  readonly orders = signal<RecentOrderRef[]>([]);
  readonly lookupId = signal<number | null>(null);

  ngOnInit(): void {
    this.orders.set(this.orderService.recentOrders());
  }

  lookup(): void {
    const id = this.lookupId();
    if (!id || id < 1) {
      this.toast.warning('Enter the order number you want to track.');
      return;
    }
    void this.router.navigate(['/orders', id]);
  }
}
