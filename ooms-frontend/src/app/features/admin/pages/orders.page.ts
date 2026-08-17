import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  AvailableStatusOption,
  LiveOrder,
} from '../../../core/models/order.models';
import { AuthService } from '../../../core/services/auth.service';
import { LiveOrderService } from '../../../core/services/live-order.service';
import { OrderService } from '../../../core/services/order.service';
import { ToastService } from '../../../core/services/toast.service';
import {
  AgoPipe,
  HumanisePipe,
  MoneyPipe,
  StatusClassPipe,
  WhenPipe,
  humanise,
} from '../../../core/utils/format';
import {
  EmptyStateComponent,
  ErrorStateComponent,
  ImgComponent,
  LoadingComponent,
  ModalComponent,
  SpinnerComponent,
} from '../../../shared/components/ui.components';

type Tab = 'live' | 'all';

/**
 * Live order board.
 *
 * `GetLiveOrders` returns only non-terminal orders; `GetAllOrders` returns the
 * full history. Status changes go through `available-statuses/{id}` first so
 * the UI only ever offers transitions the backend's
 * `OrderStatusTransitionsRules` table actually permits.
 *
 * Auto-refresh polls every 15 s on the live tab (the SignalR hub exists but the
 * server-side broadcast is commented out in OrderController, so polling is the
 * honest choice today).
 */
@Component({
  selector: 'app-admin-orders-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    MoneyPipe,
    WhenPipe,
    AgoPipe,
    StatusClassPipe,
    HumanisePipe,
    ImgComponent,
    LoadingComponent,
    ErrorStateComponent,
    EmptyStateComponent,
    ModalComponent,
    SpinnerComponent,
  ],
  template: `
    <div class="p-4 sm:p-6 lg:p-8">
      <!-- ========================== header ========================== -->
      <header class="mb-5 flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold tracking-tight text-ink-900">Orders</h1>
          <p class="mt-1 text-sm text-ink-500">
            {{ tab() === 'live' ? 'Orders currently in progress across your branches.' : 'Every order on record.' }}
          </p>
        </div>

        <div class="flex items-center gap-2">
          <span
            class="flex items-center gap-1.5 rounded-lg px-2.5 py-2 text-xs font-semibold"
            [class]="
              live.state() === 'connected'
                ? 'bg-emerald-50 text-emerald-700'
                : live.state() === 'reconnecting' || live.state() === 'connecting'
                  ? 'bg-amber-50 text-amber-700'
                  : 'bg-ink-100 text-ink-500'
            "
            [title]="
              live.state() === 'connected'
                ? 'Connected to the order hub - new orders arrive instantly.'
                : 'Order hub offline - falling back to polling every 15s.'
            "
          >
            <span class="h-1.5 w-1.5 rounded-full"
                  [class]="live.state() === 'connected' ? 'bg-emerald-500' : 'bg-ink-400'"></span>
            {{ live.state() === 'connected' ? 'Live' : 'Polling' }}
          </span>

          <label class="flex cursor-pointer items-center gap-2 rounded-lg bg-white px-3 py-2 text-xs
                        font-medium text-ink-600 ring-1 ring-ink-200">
            <input type="checkbox" class="h-3.5 w-3.5 accent-brand-500"
                   [ngModel]="autoRefresh()" (ngModelChange)="toggleAutoRefresh($event)" />
            Auto-refresh
            @if (autoRefresh() && tab() === 'live') {
              <span class="flex h-1.5 w-1.5 rounded-full bg-emerald-500"></span>
            }
          </label>

          <button type="button" class="btn-secondary btn-sm" [disabled]="loading()" (click)="load()">
            @if (loading()) { <app-spinner [size]="13" /> } @else {
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2"
                   stroke-linecap="round" stroke-linejoin="round">
                <path d="M21 12a9 9 0 1 1-3-6.7L21 8M21 3v5h-5" />
              </svg>
            }
            Refresh
          </button>
        </div>
      </header>

      <!-- ========================== stats ========================== -->
      <div class="mb-5 grid grid-cols-2 gap-3 sm:grid-cols-4">
        @for (s of stats(); track s.label) {
          <div class="card p-4">
            <p class="text-[11px] font-semibold tracking-wide text-ink-400 uppercase">{{ s.label }}</p>
            <p class="mt-1 text-2xl font-bold tabular-nums" [class]="s.tone">{{ s.value }}</p>
          </div>
        }
      </div>

      <!-- ===================== tabs + filters ===================== -->
      <div class="mb-4 flex flex-wrap items-center gap-3">
        <div class="flex rounded-lg bg-white p-1 ring-1 ring-ink-200">
          @for (t of tabs; track t.id) {
            <button
              type="button"
              class="rounded-md px-3.5 py-1.5 text-xs font-semibold transition"
              [class]="tab() === t.id ? 'bg-ink-900 text-white' : 'text-ink-600 hover:text-ink-900'"
              (click)="switchTab(t.id)"
            >
              {{ t.label }}
            </button>
          }
        </div>

        <div class="relative min-w-48 flex-1 sm:max-w-xs">
          <svg class="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-ink-400"
               width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2">
            <circle cx="11" cy="11" r="7" /><path d="m20 20-3.5-3.5" stroke-linecap="round" />
          </svg>
          <input type="search" class="input py-2 pl-9 text-xs" placeholder="Order number, customer, phone…"
                 [ngModel]="query()" (ngModelChange)="query.set($event)" aria-label="Search orders" />
        </div>

        <select class="input w-auto py-2 text-xs" [ngModel]="statusFilter()"
                (ngModelChange)="statusFilter.set($event)" aria-label="Filter by status">
          <option value="">All statuses</option>
          @for (s of knownStatuses(); track s) {
            <option [value]="s">{{ s | humanise }}</option>
          }
        </select>
      </div>

      <!-- ========================== list ========================== -->
      @if (loading() && !orders().length) {
        <app-loading label="Loading orders…" />
      } @else if (error()) {
        <app-error-state title="Could not load orders" [message]="error()!" (retry)="load()" />
      } @else if (!filtered().length) {
        <div class="card">
          <app-empty-state
            [title]="tab() === 'live' ? 'No live orders' : 'No orders found'"
            [message]="
              query() || statusFilter()
                ? 'Nothing matches the current filters.'
                : tab() === 'live'
                  ? 'New orders will appear here the moment a customer checks out.'
                  : 'No orders have been placed yet.'
            "
          />
        </div>
      } @else {
        <ul class="grid gap-3.5 xl:grid-cols-2">
          @for (o of filtered(); track o.orderId) {
            <li class="card overflow-hidden">
              <!-- card header -->
              <div class="flex flex-wrap items-start justify-between gap-3 border-b border-ink-100 p-4">
                <div class="min-w-0">
                  <div class="flex flex-wrap items-center gap-2">
                    <span class="text-sm font-bold text-ink-900">#{{ o.orderNumber.slice(0, 8) }}</span>
                    <span class="badge" [class]="o.orderStatus | statusClass">{{ o.orderStatus | humanise }}</span>
                    <span class="badge bg-ink-100 text-ink-600">{{ o.orderType | humanise }}</span>
                  </div>
                  <p class="mt-1 text-xs text-ink-500">
                    {{ o.createdAt | ago }} · {{ o.createdAt | when }}
                    @if (o.branch?.branchName) { · {{ o.branch?.branchName }} }
                  </p>
                </div>
                <span class="shrink-0 text-lg font-bold tabular-nums text-ink-900">{{ o.total | money }}</span>
              </div>

              <!-- customer -->
              <div class="flex flex-wrap gap-x-5 gap-y-1 border-b border-ink-100 bg-ink-50/50 px-4 py-2.5 text-xs">
                <span class="flex items-center gap-1.5 font-medium text-ink-700">
                  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2"
                       stroke-linecap="round" stroke-linejoin="round">
                    <path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2" /><circle cx="12" cy="7" r="4" />
                  </svg>
                  {{ customerName(o) }}
                </span>
                @if (customerPhone(o); as phone) {
                  <a [href]="'tel:' + phone" class="flex items-center gap-1.5 text-ink-600 hover:text-brand-600">
                    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2"
                         stroke-linecap="round" stroke-linejoin="round">
                      <path d="M22 16.9v3a2 2 0 0 1-2.2 2 19.8 19.8 0 0 1-8.6-3.1 19.5 19.5 0 0 1-6-6A19.8 19.8 0 0 1 2 4.2 2 2 0 0 1 4 2h3a2 2 0 0 1 2 1.7c.1 1 .4 1.9.7 2.8a2 2 0 0 1-.5 2.1L8.1 9.9a16 16 0 0 0 6 6l1.3-1.1a2 2 0 0 1 2.1-.5c.9.3 1.8.6 2.8.7a2 2 0 0 1 1.7 2Z" />
                    </svg>
                    {{ phone }}
                  </a>
                }
                <span class="text-ink-500">{{ o.paymentMethod || 'Cash' }} · {{ o.paymentStatus | humanise }}</span>
              </div>

              @if (deliveryAddress(o); as addr) {
                <p class="border-b border-ink-100 px-4 py-2 text-xs text-ink-600">
                  <span class="font-semibold text-ink-700">Deliver to:</span> {{ addr }}
                </p>
              }

              <!-- items -->
              <ul class="divide-y divide-ink-50">
                @for (item of o.items; track $index) {
                  <li class="flex items-center gap-3 px-4 py-2.5">
                    <div class="h-9 w-9 shrink-0 overflow-hidden rounded-md bg-ink-100">
                      <app-img [src]="item.imageUrl" [alt]="item.productName" />
                    </div>
                    <div class="min-w-0 flex-1">
                      <p class="truncate text-xs font-semibold text-ink-800">
                        <span class="text-brand-600">{{ item.quantity }}×</span> {{ item.productName }}
                      </p>
                      @if (item.modifiers.length || item.addons.length) {
                        <p class="truncate text-[11px] text-ink-500">{{ itemOptions(item) }}</p>
                      }
                    </div>
                    <span class="shrink-0 text-xs font-semibold tabular-nums text-ink-600">
                      {{ item.price | money }}
                    </span>
                  </li>
                }
              </ul>

              <!-- actions -->
              <div class="flex items-center gap-2 border-t border-ink-100 bg-ink-50/60 px-4 py-3">
                <button type="button" class="btn-primary btn-sm" (click)="openStatus(o)">
                  Update status
                </button>
                @if (o.guestSessionToken) {
                  <span class="badge bg-slate-100 text-slate-600">Guest</span>
                }
                <span class="ml-auto text-[11px] text-ink-400">Subtotal {{ o.subTotal | money }}</span>
              </div>
            </li>
          }
        </ul>
      }
    </div>

    <!-- ==================== status update modal ==================== -->
    @if (statusTarget(); as target) {
      <app-modal
        heading="Update order status"
        [subheading]="'Order #' + target.orderNumber.slice(0, 8) + ' · currently ' + humanise(target.orderStatus)"
        maxWidth="26rem"
        (closed)="closeStatus()"
      >
        @if (loadingStatuses()) {
          <app-loading label="Checking allowed transitions…" />
        } @else if (!availableStatuses().length) {
          <app-empty-state
            title="No transitions available"
            message="This order is in a terminal state, or no transition rules are configured for its order type. Seed dbo.OrderStatusTransitionsRules to enable status changes."
          />
        } @else {
          <div class="space-y-1.5">
            @for (s of availableStatuses(); track s.statusId) {
              <label
                class="flex cursor-pointer items-center gap-3 rounded-lg px-3 py-2.5 transition
                       hover:bg-ink-50 has-checked:bg-brand-50 has-checked:ring-1 has-checked:ring-brand-200"
              >
                <input type="radio" name="newStatus" class="h-4 w-4 accent-brand-500"
                       [value]="s.statusId" [checked]="newStatusId() === s.statusId"
                       (change)="newStatusId.set(s.statusId)" />
                <span class="text-sm font-medium text-ink-800">{{ s.statusName | humanise }}</span>
              </label>
            }
          </div>

          <div class="mt-4">
            <label class="label" for="remarks">Remarks <span class="text-ink-400">(optional)</span></label>
            <textarea id="remarks" rows="2" class="input resize-none"
                      placeholder="Rider assigned, customer called…"
                      [ngModel]="remarks()" (ngModelChange)="remarks.set($event)"></textarea>
          </div>
        }

        <div modalFooter class="flex justify-end gap-2">
          <button type="button" class="btn-secondary" (click)="closeStatus()">Cancel</button>
          <button type="button" class="btn-primary"
                  [disabled]="!newStatusId() || savingStatus()" (click)="saveStatus()">
            @if (savingStatus()) { <app-spinner [size]="14" /> }
            Save status
          </button>
        </div>
      </app-modal>
    }
  `,
})
export class AdminOrdersPage implements OnInit, OnDestroy {
  private readonly orderService = inject(OrderService);
  private readonly toast = inject(ToastService);
  private readonly auth = inject(AuthService);
  readonly live = inject(LiveOrderService);

  readonly tabs: { id: Tab; label: string }[] = [
    { id: 'live', label: 'Live' },
    { id: 'all', label: 'All orders' },
  ];

  readonly tab = signal<Tab>('live');
  readonly orders = signal<LiveOrder[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly query = signal('');
  readonly statusFilter = signal('');
  readonly autoRefresh = signal(true);

  readonly statusTarget = signal<LiveOrder | null>(null);
  readonly availableStatuses = signal<AvailableStatusOption[]>([]);
  readonly loadingStatuses = signal(false);
  readonly newStatusId = signal<number | null>(null);
  readonly remarks = signal('');
  readonly savingStatus = signal(false);

  private timer: ReturnType<typeof setInterval> | null = null;
  protected readonly humanise = humanise;

  readonly knownStatuses = computed(() =>
    [...new Set(this.orders().map((o) => o.orderStatus).filter(Boolean))].sort(),
  );

  readonly filtered = computed(() => {
    const q = this.query().trim().toLowerCase();
    const status = this.statusFilter();
    return this.orders().filter((o) => {
      if (status && o.orderStatus !== status) return false;
      if (!q) return true;
      return [o.orderNumber, this.customerName(o), this.customerPhone(o), o.orderType]
        .filter(Boolean)
        .some((v) => v!.toLowerCase().includes(q));
    });
  });

  readonly stats = computed(() => {
    const list = this.orders();
    const count = (names: string[]) => list.filter((o) => names.includes(o.orderStatus)).length;
    return [
      { label: 'Showing', value: list.length, tone: 'text-ink-900' },
      { label: 'Pending', value: count(['Pending']), tone: 'text-amber-600' },
      { label: 'In kitchen', value: count(['Confirmed', 'Preparing']), tone: 'text-blue-600' },
      {
        label: 'Revenue',
        value: new Intl.NumberFormat('en', { notation: 'compact' }).format(
          list.reduce((n, o) => n + (o.total || 0), 0),
        ),
        tone: 'text-emerald-600',
      },
    ];
  });

  constructor() {
    // A push from the hub refreshes the board immediately. `revision` changes
    // on every ReceiveOrder event; the first (zero) run is skipped.
    let seen = 0;
    effect(() => {
      const rev = this.live.revision();
      if (rev === seen) return;
      seen = rev;
      const order = this.live.lastOrder();
      this.toast.info(
        order?.orderNumber
          ? `New order #${order.orderNumber.slice(0, 8)} came in.`
          : 'A new order came in.',
      );
      this.load(true);
    });
  }

  ngOnInit(): void {
    this.load();
    this.startTimer();
    // Join the hub groups for the branches this account can see.
    void this.live.start(this.auth.branches().map((b) => b.branchId));
  }

  ngOnDestroy(): void {
    this.stopTimer();
  }

  switchTab(tab: Tab): void {
    this.tab.set(tab);
    this.load();
    // Only the live board is worth polling.
    if (tab === 'live') this.startTimer();
    else this.stopTimer();
  }

  toggleAutoRefresh(on: boolean): void {
    this.autoRefresh.set(on);
    if (on) this.startTimer();
    else this.stopTimer();
  }

  private startTimer(): void {
    this.stopTimer();
    if (!this.autoRefresh() || this.tab() !== 'live') return;
    this.timer = setInterval(() => {
      if (!document.hidden && !this.statusTarget()) this.load(true);
    }, 15_000);
  }

  private stopTimer(): void {
    if (this.timer) clearInterval(this.timer);
    this.timer = null;
  }

  load(silent = false): void {
    if (!silent) this.loading.set(true);
    this.error.set(null);
    const source = this.tab() === 'live' ? this.orderService.liveOrders() : this.orderService.allOrders();
    source.subscribe({
      next: (list) => {
        this.orders.set(list);
        this.loading.set(false);
      },
      error: (err: Error) => {
        if (!silent) this.error.set(err.message);
        this.loading.set(false);
      },
    });
  }

  /* --------------------------- presentation --------------------------- */

  customerName(o: LiveOrder): string {
    return o.customerName?.trim() || o.guestName?.trim() || 'Guest customer';
  }

  customerPhone(o: LiveOrder): string {
    return o.customerPhone?.trim() || o.guestPhoneNumber?.trim() || '';
  }

  deliveryAddress(o: LiveOrder): string {
    return [o.guestAddressLine1, o.guestAddressLine2, o.guestCity, o.guestPostalCode]
      .filter(Boolean)
      .join(', ');
  }

  itemOptions(item: LiveOrder['items'][number]): string {
    return [
      ...item.modifiers.map((m) => m.modifierName),
      ...item.addons.map((a) => `${a.addonQuantity}× ${a.addonName}`),
    ]
      .filter(Boolean)
      .join(', ');
  }

  /* ----------------------------- status ----------------------------- */

  openStatus(o: LiveOrder): void {
    this.statusTarget.set(o);
    this.newStatusId.set(null);
    this.remarks.set('');
    this.loadingStatuses.set(true);
    this.availableStatuses.set([]);

    this.orderService.availableStatuses(o.orderId).subscribe({
      next: (list) => {
        this.availableStatuses.set(list);
        this.loadingStatuses.set(false);
        if (list.length === 1) this.newStatusId.set(list[0].statusId);
      },
      error: (err: Error) => {
        this.loadingStatuses.set(false);
        this.toast.error(err.message);
      },
    });
  }

  closeStatus(): void {
    this.statusTarget.set(null);
  }

  saveStatus(): void {
    const order = this.statusTarget();
    const statusId = this.newStatusId();
    if (!order || !statusId) return;

    this.savingStatus.set(true);
    this.orderService
      .updateStatus({ orderId: order.orderId, newStatusId: statusId, remarks: this.remarks() || null })
      .subscribe({
        next: () => {
          this.savingStatus.set(false);
          const name = this.availableStatuses().find((s) => s.statusId === statusId)?.statusName ?? '';
          this.toast.success(`Order #${order.orderNumber.slice(0, 8)} is now ${humanise(name)}.`);
          this.closeStatus();
          this.load(true);
        },
        error: (err: Error) => {
          this.savingStatus.set(false);
          this.toast.error(err.message);
        },
      });
  }
}
