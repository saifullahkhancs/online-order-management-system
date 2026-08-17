import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Order, OrderTypeId } from '../../../core/models/order.models';
import { AuthService } from '../../../core/services/auth.service';
import { CartService } from '../../../core/services/cart.service';
import { OrderService } from '../../../core/services/order.service';
import { ToastService } from '../../../core/services/toast.service';
import { MoneyPipe } from '../../../core/utils/format';
import {
  EmptyStateComponent,
  ImgComponent,
  SpinnerComponent,
} from '../../../shared/components/ui.components';
import { BranchContextService } from '../branch-context.service';

/**
 * Checkout.
 *
 * The backend's `place-order` endpoint does NOT read the cart: the client must
 * send the full order (items, modifiers, add-ons, taxes, totals). The server
 * then re-validates every id against `BranchProduct`, `AddOn`, `Modifier` and
 * `BranchTaxes` and rejects anything that does not belong to the branch, so
 * the payload we build here is a proposal, not a trusted source of truth.
 *
 * Payment is cash-on-delivery: OrderRepository always writes a `Cash` payment
 * with status `Pending`. Card/online is not wired up in the API yet, so the
 * UI presents cash only rather than promising something that will not work.
 */
@Component({
  selector: 'app-checkout-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, MoneyPipe, ImgComponent, EmptyStateComponent, SpinnerComponent],
  template: `
    <section class="mx-auto max-w-5xl px-4 py-8 sm:px-6 lg:px-8">
      <header class="mb-6">
        <a routerLink="/cart" class="mb-3 inline-flex items-center gap-1.5 text-sm font-semibold text-ink-500 hover:text-ink-800">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4"
               stroke-linecap="round" stroke-linejoin="round"><path d="m15 18-6-6 6-6" /></svg>
          Back to cart
        </a>
        <h1 class="text-3xl font-bold tracking-tight text-ink-900">Checkout</h1>
      </header>

      @if (cart.isEmpty()) {
        <div class="card">
          <app-empty-state title="Nothing to check out" message="Your cart is empty.">
            <a routerLink="/menu" class="btn-primary mt-2">Browse the menu</a>
          </app-empty-state>
        </div>
      } @else {
        <form [formGroup]="form" (ngSubmit)="submit()" class="lg:grid lg:grid-cols-[1fr_20rem] lg:items-start lg:gap-6">
          <div class="space-y-5">
            <!-- =================== 1. order type =================== -->
            <fieldset class="card p-5">
              <legend class="mb-3.5 flex items-center gap-2 text-sm font-semibold text-ink-900">
                <span class="grid h-6 w-6 place-items-center rounded-full bg-brand-500 text-xs font-bold text-white">1</span>
                How would you like your order?
              </legend>

              <div class="grid gap-2.5 sm:grid-cols-3">
                @for (t of orderTypes; track t.id) {
                  <label
                    class="flex cursor-pointer flex-col gap-1 rounded-lg border-2 p-3.5 transition"
                    [class]="
                      form.controls.orderTypeId.value === t.id
                        ? 'border-brand-500 bg-brand-50'
                        : 'border-ink-200 hover:border-ink-300 hover:bg-ink-50'
                    "
                  >
                    <input type="radio" class="sr-only" [value]="t.id" formControlName="orderTypeId" />
                    <span class="flex items-center gap-2 text-sm font-semibold text-ink-900">
                      <span [innerHTML]="t.icon" class="text-brand-500"></span>
                      {{ t.label }}
                    </span>
                    <span class="text-xs text-ink-500">{{ t.hint }}</span>
                  </label>
                }
              </div>
            </fieldset>

            <!-- =================== 2. contact =================== -->
            <fieldset class="card p-5">
              <legend class="mb-3.5 flex items-center gap-2 text-sm font-semibold text-ink-900">
                <span class="grid h-6 w-6 place-items-center rounded-full bg-brand-500 text-xs font-bold text-white">2</span>
                Contact details
              </legend>

              <div class="grid gap-4 sm:grid-cols-2">
                <div>
                  <label class="label" for="guestName">Full name <span class="text-danger">*</span></label>
                  <input id="guestName" type="text" class="input" formControlName="guestName"
                         autocomplete="name" placeholder="Ayesha Khan" />
                  @if (invalid('guestName')) { <p class="field-error">Please tell us your name.</p> }
                </div>

                <div>
                  <label class="label" for="guestPhoneNumber">Phone <span class="text-danger">*</span></label>
                  <input id="guestPhoneNumber" type="tel" class="input" formControlName="guestPhoneNumber"
                         autocomplete="tel" placeholder="+92 300 1234567" />
                  @if (invalid('guestPhoneNumber')) {
                    <p class="field-error">We need a phone number to reach you about this order.</p>
                  }
                </div>

                <div class="sm:col-span-2">
                  <label class="label" for="guestEmailAddress">Email <span class="text-ink-400">(optional)</span></label>
                  <input id="guestEmailAddress" type="email" class="input" formControlName="guestEmailAddress"
                         autocomplete="email" placeholder="you@example.com" />
                  @if (invalid('guestEmailAddress')) { <p class="field-error">That email does not look right.</p> }
                </div>
              </div>
            </fieldset>

            <!-- =================== 3. delivery address =================== -->
            @if (isDelivery()) {
              <fieldset class="card p-5">
                <legend class="mb-3.5 flex items-center gap-2 text-sm font-semibold text-ink-900">
                  <span class="grid h-6 w-6 place-items-center rounded-full bg-brand-500 text-xs font-bold text-white">3</span>
                  Delivery address
                </legend>

                <div class="grid gap-4 sm:grid-cols-2">
                  <div class="sm:col-span-2">
                    <label class="label" for="guestAddressLine1">Street address <span class="text-danger">*</span></label>
                    <input id="guestAddressLine1" type="text" class="input" formControlName="guestAddressLine1"
                           autocomplete="address-line1" placeholder="House 12, Street 4" />
                    @if (invalid('guestAddressLine1')) { <p class="field-error">A street address is required for delivery.</p> }
                  </div>

                  <div class="sm:col-span-2">
                    <label class="label" for="guestAddressLine2">Apartment, suite, landmark <span class="text-ink-400">(optional)</span></label>
                    <input id="guestAddressLine2" type="text" class="input" formControlName="guestAddressLine2"
                           autocomplete="address-line2" placeholder="Near the park" />
                  </div>

                  <div>
                    <label class="label" for="guestCity">City <span class="text-danger">*</span></label>
                    <input id="guestCity" type="text" class="input" formControlName="guestCity"
                           autocomplete="address-level2" placeholder="Lahore" />
                    @if (invalid('guestCity')) { <p class="field-error">Which city?</p> }
                  </div>

                  <div>
                    <label class="label" for="guestPostalCode">Postal code <span class="text-ink-400">(optional)</span></label>
                    <input id="guestPostalCode" type="text" class="input" formControlName="guestPostalCode"
                           autocomplete="postal-code" placeholder="54000" />
                  </div>

                  <div class="sm:col-span-2">
                    <label class="label" for="deliveryInstructions">Delivery notes <span class="text-ink-400">(optional)</span></label>
                    <textarea id="deliveryInstructions" rows="2" maxlength="500" class="input resize-none"
                              formControlName="deliveryInstructions"
                              placeholder="Ring the bell twice, gate code 1234…"></textarea>
                  </div>
                </div>
              </fieldset>
            }

            <!-- =================== 4. payment =================== -->
            <fieldset class="card p-5">
              <legend class="mb-3.5 flex items-center gap-2 text-sm font-semibold text-ink-900">
                <span class="grid h-6 w-6 place-items-center rounded-full bg-brand-500 text-xs font-bold text-white">
                  {{ isDelivery() ? 4 : 3 }}
                </span>
                Payment
              </legend>

              <div class="flex items-start gap-3 rounded-lg border-2 border-brand-500 bg-brand-50 p-3.5">
                <svg class="mt-0.5 shrink-0 text-brand-600" width="20" height="20" viewBox="0 0 24 24" fill="none"
                     stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <rect x="2" y="6" width="20" height="12" rx="2" /><circle cx="12" cy="12" r="2.5" />
                </svg>
                <div>
                  <p class="text-sm font-semibold text-ink-900">
                    {{ isDelivery() ? 'Cash on delivery' : 'Pay at the counter' }}
                  </p>
                  <p class="mt-0.5 text-xs leading-relaxed text-ink-600">
                    Your order is recorded with payment status <strong>Pending</strong> and settled when you
                    receive it. Card and online payments are not enabled on this API yet.
                  </p>
                </div>
              </div>
            </fieldset>
          </div>

          <!-- ========================= summary ========================= -->
          <aside class="card mt-5 p-5 lg:sticky lg:top-24 lg:mt-0">
            <h2 class="text-sm font-semibold text-ink-900">Your order</h2>
            @if (branchCtx.selected(); as b) {
              <p class="mt-0.5 text-xs text-ink-500">from {{ b.branchName }}</p>
            }

            <ul class="mt-4 max-h-56 space-y-3 overflow-y-auto pr-1">
              @for (item of cart.items(); track item.cartItemId) {
                <li class="flex gap-2.5">
                  <div class="h-11 w-11 shrink-0 overflow-hidden rounded-md bg-ink-100">
                    <app-img [src]="item.productImageUrl" [alt]="item.productName" />
                  </div>
                  <div class="min-w-0 flex-1">
                    <p class="truncate text-xs font-semibold text-ink-800">
                      {{ item.quantity }}× {{ item.productName }}
                    </p>
                    @if (item.modifiers.length || item.addons.length) {
                      <p class="truncate text-[11px] text-ink-400">{{ optionSummary(item.cartItemId) }}</p>
                    }
                  </div>
                  <span class="shrink-0 text-xs font-semibold tabular-nums text-ink-700">
                    {{ cart.lineTotal(item) | money }}
                  </span>
                </li>
              }
            </ul>

            <dl class="mt-4 space-y-2 border-t border-ink-100 pt-4 text-sm">
              <div class="flex justify-between">
                <dt class="text-ink-500">Subtotal</dt>
                <dd class="font-semibold tabular-nums text-ink-800">{{ cart.subTotal() | money }}</dd>
              </div>
              @for (t of cart.taxes(); track t.taxId) {
                <div class="flex justify-between">
                  <dt class="text-ink-500">{{ t.name }}</dt>
                  <dd class="font-semibold tabular-nums text-ink-800">{{ t.amount | money }}</dd>
                </div>
              }
              <div class="flex justify-between border-t border-ink-100 pt-2.5">
                <dt class="text-base font-bold text-ink-900">Total</dt>
                <dd class="text-base font-bold tabular-nums text-brand-600">{{ cart.grandTotal() | money }}</dd>
              </div>
            </dl>

            <button type="submit" class="btn-primary mt-5 w-full" [disabled]="placing()">
              @if (placing()) { <app-spinner [size]="15" /> }
              {{ placing() ? 'Placing your order…' : 'Place order' }}
            </button>

            <p class="mt-3 text-center text-[11px] leading-relaxed text-ink-400">
              By placing this order you agree to be contacted on the number above about its progress.
            </p>
          </aside>
        </form>
      }
    </section>
  `,
})
export class CheckoutPage implements OnInit {
  readonly cart = inject(CartService);
  readonly branchCtx = inject(BranchContextService);
  private readonly orders = inject(OrderService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly placing = signal(false);

  readonly orderTypes = [
    {
      id: OrderTypeId.Delivery,
      label: 'Delivery',
      hint: 'Brought to your door',
      icon: `<svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M10 17h4V5H2v12h3M20 17h2v-6l-3-4h-5v10h2"/><circle cx="7.5" cy="17.5" r="2.5"/><circle cx="17.5" cy="17.5" r="2.5"/></svg>`,
    },
    {
      id: OrderTypeId.Pickup,
      label: 'Pickup',
      hint: 'Collect from the branch',
      icon: `<svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M6 2 3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4Z"/><path d="M3 6h18M16 10a4 4 0 0 1-8 0"/></svg>`,
    },
    {
      id: OrderTypeId.Takeaway,
      label: 'Takeaway',
      hint: 'Packed and ready to go',
      icon: `<svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 8h16l-1.4 12.2a2 2 0 0 1-2 1.8H7.4a2 2 0 0 1-2-1.8Z"/><path d="M8 8V6a4 4 0 0 1 8 0v2"/></svg>`,
    },
  ];

  readonly form = this.fb.nonNullable.group({
    orderTypeId: [OrderTypeId.Delivery as number, Validators.required],
    guestName: ['', [Validators.required, Validators.maxLength(255)]],
    guestPhoneNumber: ['', [Validators.required, Validators.minLength(7), Validators.maxLength(50)]],
    guestEmailAddress: ['', [Validators.email, Validators.maxLength(255)]],
    guestAddressLine1: ['', Validators.maxLength(500)],
    guestAddressLine2: ['', Validators.maxLength(500)],
    guestCity: ['', Validators.maxLength(100)],
    guestPostalCode: ['', Validators.maxLength(20)],
    guestCountry: ['Pakistan'],
    deliveryInstructions: ['', Validators.maxLength(500)],
  });

  readonly isDelivery = computed(() => this.form.controls.orderTypeId.value === OrderTypeId.Delivery);

  ngOnInit(): void {
    this.branchCtx.load().subscribe();
    this.cart.refresh().subscribe();

    // Prefill from the signed-in user, if any.
    const s = this.auth.session();
    if (s) {
      this.form.patchValue({ guestName: s.username, guestEmailAddress: s.email });
    }

    // Address fields are only required for delivery - toggle the validators.
    this.form.controls.orderTypeId.valueChanges.subscribe((t) => {
      const required = t === OrderTypeId.Delivery;
      for (const name of ['guestAddressLine1', 'guestCity'] as const) {
        const c = this.form.controls[name];
        c.setValidators(
          required
            ? [Validators.required, Validators.maxLength(name === 'guestCity' ? 100 : 500)]
            : [Validators.maxLength(name === 'guestCity' ? 100 : 500)],
        );
        c.updateValueAndValidity({ emitEvent: false });
      }
    });
    this.form.controls.orderTypeId.updateValueAndValidity();
  }

  invalid(name: keyof typeof this.form.controls): boolean {
    const c = this.form.controls[name];
    return c.invalid && (c.touched || c.dirty);
  }

  optionSummary(cartItemId: number): string {
    const item = this.cart.items().find((i) => i.cartItemId === cartItemId);
    if (!item) return '';
    return [
      ...item.modifiers.map((m) => m.modifierName),
      ...item.addons.map((a) => `${a.addOnQuantity}× ${a.addOnName}`),
    ].join(', ');
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.toast.warning('Please complete the highlighted fields.');
      return;
    }

    const branchId = this.branchCtx.selectedId();
    if (!branchId) {
      this.toast.error('Please choose a branch before checking out.');
      void this.router.navigate(['/branches']);
      return;
    }

    const v = this.form.getRawValue();
    const session = this.auth.session();
    const identity = this.cart.identity();
    const taxTotal = this.cart.taxes().reduce((n, t) => n + (t.amount ?? 0), 0);

    // Build the payload the API expects: full items with their options, plus
    // the branch taxes it will re-validate against BranchTaxes.
    const order: Order = {
      branchId,
      headOfficeId: this.branchCtx.selected()?.headOfficeId ?? null,
      customerId: session?.userId ?? null,
      orderTypeId: v.orderTypeId,
      orderDate: new Date().toISOString(),
      tableId: null,

      subtotal: this.cart.subTotal(),
      taxAmount: taxTotal,
      discountAmount: 0,
      deliveryFee: 0,
      totalAmount: this.cart.grandTotal(),

      deliveryAddress: this.isDelivery()
        ? [v.guestAddressLine1, v.guestAddressLine2, v.guestCity, v.guestPostalCode]
            .filter(Boolean)
            .join(', ')
        : null,
      deliveryInstructions: v.deliveryInstructions || null,

      // Guests are matched on this token later, so always send it.
      guestSessionToken: identity.guestSessionToken ?? this.cart.guestToken(),
      guestName: v.guestName,
      guestPhoneNumber: v.guestPhoneNumber,
      guestEmailAddress: v.guestEmailAddress || null,
      guestAddressLine1: this.isDelivery() ? v.guestAddressLine1 : null,
      guestAddressLine2: this.isDelivery() ? v.guestAddressLine2 || null : null,
      guestCity: this.isDelivery() ? v.guestCity : null,
      guestPostalCode: this.isDelivery() ? v.guestPostalCode || null : null,
      guestCountry: this.isDelivery() ? v.guestCountry : null,

      orderItems: this.cart.items().map((i) => ({
        productId: i.productId,
        productName: i.productName,
        quantity: i.quantity,
        unitPrice: i.unitPrice,
        finalPrice: this.cart.lineTotal(i),
        instructions: i.instructions ?? null,
        discountAmount: 0,
        imageUrl: i.productImageUrl,
        itemModifiers: i.modifiers.map((m) => ({
          modifierId: m.modifierId,
          modifierName: m.modifierName,
          modifierPrice: m.modifierPrice,
        })),
        itemAddons: i.addons.map((a) => ({
          addonId: a.addOnId,
          addonName: a.addOnName,
          addonQuantity: a.addOnQuantity,
          addonPrice: a.addOnPrice,
        })),
      })),

      orderTaxes: this.cart.taxes().map((t) => ({
        taxId: t.taxId,
        taxName: t.name,
        taxRate: t.rate,
        taxableAmount: this.cart.subTotal(),
        taxAmount: t.amount,
      })),
    };

    this.placing.set(true);
    this.orders.placeOrder(order).subscribe({
      next: (res) => {
        this.placing.set(false);
        if (!res?.orderId) {
          this.toast.error('The order was not created. Please try again.');
          return;
        }
        this.orders.rememberOrder({
          orderId: res.orderId,
          orderNumber: res.orderNumber ?? String(res.orderId),
          placedAt: new Date().toISOString(),
          total: res.totalAmount ?? this.cart.grandTotal(),
          branchName: this.branchCtx.selected()?.branchName ?? null,
        });
        this.cart.reset();
        this.toast.success('Order placed. You can track it right here.');
        void this.router.navigate(['/orders', res.orderId], { queryParams: { placed: 1 } });
      },
      error: (err: Error) => {
        this.placing.set(false);
        this.toast.error(err.message);
      },
    });
  }
}
