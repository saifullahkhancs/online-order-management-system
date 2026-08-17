import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AddOnGroup, ModifierGroup, ProductDetails } from '../../../core/models/catalog.models';
import { CartService } from '../../../core/services/cart.service';
import { MenuService } from '../../../core/services/menu.service';
import { ToastService } from '../../../core/services/toast.service';
import { MoneyPipe } from '../../../core/utils/format';
import {
  ImgComponent,
  LoadingComponent,
  SpinnerComponent,
} from '../../../shared/components/ui.components';

/**
 * Product configuration sheet.
 *
 * Rules enforced here (they mirror the columns the admin sets):
 *  - ModifierCategory.isRequired -> exactly one modifier must be chosen from
 *    that group. Modifier groups are single-select (radio) because a modifier
 *    changes the item (size, spice level).
 *  - AddOnCategory.minSelect / maxSelect -> add-ons are multi-select with a
 *    quantity, bounded by those two numbers.
 *
 * The running price is a preview only. The server recomputes it from
 * `Product.Price` + modifier/add-on prices when the line hits the cart.
 */
@Component({
  selector: 'app-product-sheet',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, MoneyPipe, ImgComponent, LoadingComponent, SpinnerComponent],
  template: `
    <div
      class="fixed inset-0 z-50 flex items-end justify-center bg-ink-950/55 backdrop-blur-[2px] sm:items-center sm:p-6"
      (click)="close.emit()"
      role="dialog"
      aria-modal="true"
      [attr.aria-label]="details()?.product?.productName ?? 'Product'"
    >
      <div
        class="animate-fade-up flex max-h-[94vh] w-full max-w-lg flex-col overflow-hidden rounded-t-2xl
               bg-white shadow-pop sm:rounded-card"
        (click)="$event.stopPropagation()"
      >
        @if (loading()) {
          <app-loading label="Loading options…" />
        } @else if (error()) {
          <div class="p-8 text-center">
            <p class="text-sm text-ink-600">{{ error() }}</p>
            <button type="button" class="btn-secondary mt-4" (click)="close.emit()">Close</button>
          </div>
        } @else if (details(); as d) {
          <!-- ------------------------- header image ------------------------- -->
          <div class="relative shrink-0">
            <div class="aspect-[16/9] w-full overflow-hidden bg-ink-100">
              <app-img [src]="d.product.imageUrl" [alt]="d.product.productName ?? ''" />
            </div>
            <button
              type="button"
              class="absolute top-3 right-3 grid h-9 w-9 place-items-center rounded-full bg-white/90
                     text-ink-700 shadow-sm backdrop-blur transition hover:bg-white"
              (click)="close.emit()"
              aria-label="Close"
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4">
                <path d="M18 6 6 18M6 6l12 12" stroke-linecap="round" />
              </svg>
            </button>
          </div>

          <!-- ---------------------------- body ---------------------------- -->
          <div class="min-h-0 flex-1 overflow-y-auto">
            <div class="border-b border-ink-100 px-5 py-4">
              <h2 class="text-xl font-bold tracking-tight text-ink-900">{{ d.product.productName }}</h2>
              @if (d.product.description) {
                <p class="mt-1.5 text-sm leading-relaxed text-ink-500">{{ d.product.description }}</p>
              }
              <p class="mt-2.5 text-lg font-bold text-brand-600">{{ d.product.price | money }}</p>
            </div>

            <!-- ------------------------ modifiers ------------------------ -->
            @for (group of d.modifierGroups; track groupKey(group)) {
              <fieldset class="border-b border-ink-100 px-5 py-4">
                <legend class="mb-3 flex w-full items-center justify-between gap-3">
                  <span class="text-sm font-semibold text-ink-900">
                    {{ group.category.categoryName ?? group.category.name }}
                  </span>
                  <span
                    class="badge"
                    [class]="
                      group.category.isRequired
                        ? 'bg-brand-100 text-brand-700 ring-1 ring-brand-200'
                        : 'bg-ink-100 text-ink-600'
                    "
                  >
                    {{ group.category.isRequired ? 'Required · pick 1' : 'Optional' }}
                  </span>
                </legend>

                <div class="space-y-1.5">
                  @for (m of group.modifiers; track m.id) {
                    <label
                      class="flex cursor-pointer items-center gap-3 rounded-lg px-3 py-2.5 transition
                             hover:bg-ink-50 has-checked:bg-brand-50 has-checked:ring-1 has-checked:ring-brand-200"
                    >
                      <input
                        type="radio"
                        class="h-4 w-4 shrink-0 accent-brand-500"
                        [name]="'mod-' + groupKey(group)"
                        [value]="m.id"
                        [checked]="selectedModifiers()[groupKey(group)] === m.id"
                        (change)="pickModifier(groupKey(group), m.id, !!group.category.isRequired)"
                      />
                      <span class="flex-1 text-sm font-medium text-ink-800">{{ m.name }}</span>
                      @if (m.defaultPrice) {
                        <span class="text-sm font-semibold text-ink-600">+{{ m.defaultPrice | money }}</span>
                      }
                    </label>
                  }
                </div>

                @if (group.category.isRequired && !selectedModifiers()[groupKey(group)] && submitted()) {
                  <p class="field-error">Please choose an option.</p>
                }
              </fieldset>
            }

            <!-- ------------------------- add-ons ------------------------- -->
            @for (group of d.addOnGroups; track addOnKey(group)) {
              <fieldset class="border-b border-ink-100 px-5 py-4">
                <legend class="mb-3 flex w-full items-center justify-between gap-3">
                  <span class="text-sm font-semibold text-ink-900">
                    {{ group.category.categoryName ?? group.category.name }}
                  </span>
                  <span class="badge bg-ink-100 text-ink-600">{{ selectionHint(group) }}</span>
                </legend>

                <div class="space-y-1.5">
                  @for (a of group.addOns; track a.id) {
                    <div
                      class="flex items-center gap-3 rounded-lg px-3 py-2.5 transition"
                      [class]="addOnQty()[a.id] ? 'bg-brand-50 ring-1 ring-brand-200' : 'hover:bg-ink-50'"
                    >
                      <label class="flex flex-1 cursor-pointer items-center gap-3">
                        <input
                          type="checkbox"
                          class="h-4 w-4 shrink-0 rounded accent-brand-500"
                          [checked]="!!addOnQty()[a.id]"
                          [disabled]="!addOnQty()[a.id] && atMax(group)"
                          (change)="toggleAddOn(group, a.id)"
                        />
                        <span class="flex-1 text-sm font-medium text-ink-800">{{ a.name }}</span>
                      </label>

                      @if (a.addOnUnitPrice) {
                        <span class="text-sm font-semibold text-ink-600">+{{ a.addOnUnitPrice | money }}</span>
                      }

                      @if (addOnQty()[a.id]) {
                        <span class="flex shrink-0 items-center gap-1 rounded-lg bg-white ring-1 ring-ink-200">
                          <button type="button" class="px-2 py-1 text-ink-600 hover:text-ink-900"
                                  (click)="bumpAddOn(a.id, -1)" aria-label="Decrease">−</button>
                          <span class="min-w-4 text-center text-xs font-bold tabular-nums">{{ addOnQty()[a.id] }}</span>
                          <button type="button" class="px-2 py-1 text-ink-600 hover:text-ink-900"
                                  (click)="bumpAddOn(a.id, 1)" aria-label="Increase">+</button>
                        </span>
                      }
                    </div>
                  }
                </div>

                @if (belowMin(group) && submitted()) {
                  <p class="field-error">Choose at least {{ group.category.minSelect }}.</p>
                }
              </fieldset>
            }

            <!-- --------------------- special instructions --------------------- -->
            <div class="px-5 py-4">
              <label class="label" [attr.for]="'instructions'">Special instructions</label>
              <textarea
                id="instructions"
                rows="2"
                maxlength="500"
                class="input resize-none"
                placeholder="No onions, extra napkins…"
                [ngModel]="instructions()"
                (ngModelChange)="instructions.set($event)"
              ></textarea>
              <p class="mt-1 text-right text-[11px] text-ink-400">{{ instructions().length }}/500</p>
            </div>
          </div>

          <!-- --------------------------- footer --------------------------- -->
          <div class="shrink-0 border-t border-ink-100 bg-white px-5 py-3.5">
            <div class="flex items-center gap-3">
              <div class="flex items-center rounded-lg ring-1 ring-ink-200">
                <button
                  type="button"
                  class="grid h-10 w-10 place-items-center rounded-l-lg text-ink-600 transition hover:bg-ink-100
                         disabled:opacity-40"
                  [disabled]="quantity() <= 1"
                  (click)="quantity.set(quantity() - 1)"
                  aria-label="Decrease quantity"
                >
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.6">
                    <path d="M5 12h14" stroke-linecap="round" />
                  </svg>
                </button>
                <span class="w-9 text-center text-sm font-bold tabular-nums">{{ quantity() }}</span>
                <button
                  type="button"
                  class="grid h-10 w-10 place-items-center rounded-r-lg text-ink-600 transition hover:bg-ink-100"
                  (click)="quantity.set(quantity() + 1)"
                  aria-label="Increase quantity"
                >
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.6">
                    <path d="M12 5v14M5 12h14" stroke-linecap="round" />
                  </svg>
                </button>
              </div>

              <button
                type="button"
                class="btn-primary flex-1 justify-between"
                [disabled]="cart.mutating()"
                (click)="addToCart()"
              >
                <span class="flex items-center gap-2">
                  @if (cart.mutating()) { <app-spinner [size]="15" /> }
                  {{ existingLine() ? 'Update cart' : 'Add to cart' }}
                </span>
                <span class="font-bold tabular-nums">{{ runningTotal() | money }}</span>
              </button>
            </div>

            @if (existingLine()) {
              <p class="mt-2 text-center text-[11px] leading-relaxed text-ink-400">
                This item is already in your cart — adding it again replaces the existing line.
              </p>
            }
          </div>
        }
      </div>
    </div>
  `,
})
export class ProductSheetComponent {
  /** Product to configure. */
  readonly productId = input.required<number>();
  /** Branch the line belongs to. */
  readonly branchId = input.required<number>();
  readonly close = output<void>();
  readonly added = output<void>();

  private readonly menu = inject(MenuService);
  readonly cart = inject(CartService);
  private readonly toast = inject(ToastService);

  readonly details = signal<ProductDetails | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly submitted = signal(false);

  readonly quantity = signal(1);
  readonly instructions = signal('');
  /** groupKey -> modifierId */
  readonly selectedModifiers = signal<Record<string, number>>({});
  /** addOnId -> quantity */
  readonly addOnQty = signal<Record<number, number>>({});

  /** The cart line for this product, if any (the API keys lines by productId). */
  readonly existingLine = computed(() =>
    this.cart.items().find((i) => i.productId === this.productId()),
  );

  readonly runningTotal = computed(() => {
    const d = this.details();
    if (!d) return 0;
    const base = d.product.price ?? 0;

    let mods = 0;
    for (const g of d.modifierGroups) {
      const chosen = this.selectedModifiers()[this.groupKey(g)];
      const m = g.modifiers.find((x) => x.id === chosen);
      if (m) mods += m.defaultPrice ?? 0;
    }

    let adds = 0;
    for (const g of d.addOnGroups) {
      for (const a of g.addOns) {
        const q = this.addOnQty()[a.id] ?? 0;
        if (q) adds += (a.addOnUnitPrice ?? 0) * q;
      }
    }

    // Matches CartRepository: modifiers are per-line, base price is per-unit.
    return base * this.quantity() + mods + adds;
  });

  constructor() {
    effect(() => {
      const id = this.productId();
      this.loading.set(true);
      this.error.set(null);
      this.menu.productDetails(id).subscribe({
        next: (d) => {
          this.details.set(d);
          this.loading.set(false);
          this.prefill(d);
        },
        error: (err: Error) => {
          this.error.set(err.message);
          this.loading.set(false);
        },
      });
    });
  }

  /** Seeds required single-select groups and reuses the existing cart line. */
  private prefill(d: ProductDetails): void {
    const line = this.existingLine();
    const mods: Record<string, number> = {};

    for (const g of d.modifierGroups) {
      const key = this.groupKey(g);
      const already = line?.modifiers.find((m) => g.modifiers.some((x) => x.id === m.modifierId));
      if (already) mods[key] = already.modifierId;
      else if (g.category.isRequired && g.modifiers.length) mods[key] = g.modifiers[0].id;
    }
    this.selectedModifiers.set(mods);

    const adds: Record<number, number> = {};
    for (const a of line?.addons ?? []) adds[a.addOnId] = a.addOnQuantity || 1;
    this.addOnQty.set(adds);

    if (line) {
      this.quantity.set(line.quantity || 1);
      this.instructions.set(line.instructions ?? '');
    }
  }

  groupKey(g: ModifierGroup): string {
    return String(g.category.categoryId ?? g.category.id ?? g.category.name ?? 'g');
  }

  addOnKey(g: AddOnGroup): string {
    return String(g.category.categoryId ?? g.category.id ?? g.category.name ?? 'a');
  }

  pickModifier(key: string, modifierId: number, required: boolean): void {
    this.selectedModifiers.update((cur) => {
      const next = { ...cur };
      // Optional groups can be toggled off by re-picking the same option.
      if (!required && next[key] === modifierId) delete next[key];
      else next[key] = modifierId;
      return next;
    });
  }

  /* --------------------------- add-on helpers --------------------------- */

  private chosenCount(g: AddOnGroup): number {
    return g.addOns.filter((a) => (this.addOnQty()[a.id] ?? 0) > 0).length;
  }

  atMax(g: AddOnGroup): boolean {
    const max = g.category.maxSelect ?? 0;
    return max > 0 && this.chosenCount(g) >= max;
  }

  belowMin(g: AddOnGroup): boolean {
    const min = g.category.minSelect ?? 0;
    return min > 0 && this.chosenCount(g) < min;
  }

  selectionHint(g: AddOnGroup): string {
    const min = g.category.minSelect ?? 0;
    const max = g.category.maxSelect ?? 0;
    if (min > 0 && max > 0) return min === max ? `Pick ${min}` : `Pick ${min}–${max}`;
    if (max > 0) return `Up to ${max}`;
    return 'Optional';
  }

  toggleAddOn(g: AddOnGroup, addOnId: number): void {
    this.addOnQty.update((cur) => {
      const next = { ...cur };
      if (next[addOnId]) delete next[addOnId];
      else if (!this.atMax(g)) next[addOnId] = 1;
      return next;
    });
  }

  bumpAddOn(addOnId: number, delta: number): void {
    this.addOnQty.update((cur) => {
      const next = { ...cur };
      const q = (next[addOnId] ?? 0) + delta;
      if (q <= 0) delete next[addOnId];
      else next[addOnId] = Math.min(q, 20);
      return next;
    });
  }

  /* ------------------------------- submit ------------------------------- */

  addToCart(): void {
    this.submitted.set(true);
    const d = this.details();
    if (!d) return;

    // Required modifier groups must have a pick.
    const missing = d.modifierGroups.find(
      (g) => g.category.isRequired && !this.selectedModifiers()[this.groupKey(g)],
    );
    if (missing) {
      this.toast.warning(
        `Please choose an option for "${missing.category.categoryName ?? missing.category.name}".`,
      );
      return;
    }

    // Add-on groups must meet their minimum.
    const short = d.addOnGroups.find((g) => this.belowMin(g));
    if (short) {
      this.toast.warning(
        `Choose at least ${short.category.minSelect} from "${short.category.categoryName ?? short.category.name}".`,
      );
      return;
    }

    this.cart
      .addItem({
        branchId: this.branchId(),
        productId: this.productId(),
        quantity: this.quantity(),
        instructions: this.instructions().trim() || null,
        modifiers: Object.values(this.selectedModifiers()).map((modifierId) => ({
          modifierId,
          quantity: 1,
        })),
        addons: Object.entries(this.addOnQty()).map(([addOnId, quantity]) => ({
          addOnId: Number(addOnId),
          quantity,
        })),
      })
      .subscribe({
        next: () => {
          this.toast.success(`${d.product.productName} added to your cart.`);
          this.added.emit();
          this.close.emit();
        },
        error: () => {
          /* CartService already surfaced the message */
        },
      });
  }
}
