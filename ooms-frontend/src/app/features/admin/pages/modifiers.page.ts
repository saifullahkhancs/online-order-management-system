import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  AddOn,
  AddOnCategory,
  Modifier,
  ModifierCategory,
} from '../../../core/models/catalog.models';
import { AdminService } from '../../../core/services/admin.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { MoneyPipe } from '../../../core/utils/format';
import {
  ConfirmComponent,
  EmptyStateComponent,
  ErrorStateComponent,
  LoadingComponent,
  ModalComponent,
  SpinnerComponent,
} from '../../../shared/components/ui.components';

type Tab = 'modifiers' | 'addons';
type DeleteKind = 'modifierCategory' | 'modifier' | 'addOnCategory' | 'addOn';

/**
 * Modifiers and add-ons.
 *
 * Modifiers *change* an item (size, doneness) and are grouped by a category
 * that may be required. Add-ons are *extras* with their own quantity, grouped
 * by a category with min/max selection rules.
 *
 * Both sets of endpoints are head-office scoped: the GET-all routes require a
 * `headOfficeId` query parameter, taken from the signed-in user's JWT.
 */
@Component({
  selector: 'app-admin-modifiers-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    MoneyPipe,
    LoadingComponent,
    ErrorStateComponent,
    EmptyStateComponent,
    ModalComponent,
    ConfirmComponent,
    SpinnerComponent,
  ],
  template: `
    <div class="p-4 sm:p-6 lg:p-8">
      <header class="mb-5 flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold tracking-tight text-ink-900">Modifiers &amp; add-ons</h1>
          <p class="mt-1 text-sm text-ink-500">
            Options customers pick on the product sheet before adding to cart.
          </p>
        </div>
        <div class="flex gap-2">
          <button type="button" class="btn-secondary" (click)="openCategoryForm()">
            New group
          </button>
          <button type="button" class="btn-primary" [disabled]="!currentCategories().length"
                  (click)="openItemForm()">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.6"
                 stroke-linecap="round"><path d="M12 5v14M5 12h14" /></svg>
            {{ tab() === 'modifiers' ? 'New modifier' : 'New add-on' }}
          </button>
        </div>
      </header>

      <div class="mb-5 flex rounded-lg bg-white p-1 ring-1 ring-ink-200 sm:w-fit">
        @for (t of tabs; track t.id) {
          <button type="button" class="flex-1 rounded-md px-4 py-1.5 text-xs font-semibold transition sm:flex-none"
                  [class]="tab() === t.id ? 'bg-ink-900 text-white' : 'text-ink-600 hover:text-ink-900'"
                  (click)="tab.set(t.id)">
            {{ t.label }}
          </button>
        }
      </div>

      @if (!headOfficeId()) {
        <div class="card">
          <app-empty-state
            title="No head office on your account"
            message="Modifier and add-on endpoints are scoped by head office. Sign in with a user that belongs to one."
          />
        </div>
      } @else if (loading()) {
        <app-loading label="Loading options…" />
      } @else if (error()) {
        <app-error-state title="Could not load options" [message]="error()!" (retry)="load()" />
      } @else if (!currentCategories().length) {
        <div class="card">
          <app-empty-state
            [title]="tab() === 'modifiers' ? 'No modifier groups' : 'No add-on groups'"
            [message]="
              tab() === 'modifiers'
                ? 'Create a group such as “Size” or “Cooking preference”, then add its options.'
                : 'Create a group such as “Extras” or “Sauces”, then add its options.'
            "
          >
            <button type="button" class="btn-primary mt-2" (click)="openCategoryForm()">New group</button>
          </app-empty-state>
        </div>
      } @else {
        <div class="grid gap-4 lg:grid-cols-2">
          @for (group of grouped(); track group.categoryId) {
            <section class="card overflow-hidden">
              <header class="flex items-start justify-between gap-3 border-b border-ink-100 p-4">
                <div class="min-w-0">
                  <h2 class="truncate font-semibold text-ink-900">{{ group.name }}</h2>
                  <p class="mt-0.5 text-xs text-ink-500">{{ group.rule }}</p>
                </div>
                <div class="flex shrink-0 gap-1">
                  <button type="button" class="rounded-md p-1.5 text-ink-400 transition hover:bg-ink-100 hover:text-ink-800"
                          (click)="openCategoryForm(group.categoryId)" aria-label="Edit group" title="Edit group">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                         stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                      <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                      <path d="M18.5 2.5a2.1 2.1 0 0 1 3 3L12 15l-4 1 1-4Z" />
                    </svg>
                  </button>
                  <button type="button" class="rounded-md p-1.5 text-ink-400 transition hover:bg-rose-50 hover:text-danger"
                          (click)="askDelete(tab() === 'modifiers' ? 'modifierCategory' : 'addOnCategory',
                                              group.categoryId, group.name)"
                          aria-label="Delete group" title="Delete group">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                         stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                      <path d="M3 6h18M8 6V4a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1v2M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                    </svg>
                  </button>
                </div>
              </header>

              <ul class="divide-y divide-ink-50">
                @for (opt of group.options; track opt.id) {
                  <li class="flex items-center gap-3 px-4 py-2.5">
                    <span class="min-w-0 flex-1 truncate text-sm text-ink-800">{{ opt.name }}</span>
                    <span class="shrink-0 text-sm font-semibold tabular-nums text-ink-600">
                      {{ opt.price ? (opt.price | money) : 'Free' }}
                    </span>
                    <button type="button" class="shrink-0 rounded-md p-1 text-ink-300 transition hover:bg-ink-100 hover:text-ink-700"
                            (click)="openItemForm(opt.id)" aria-label="Edit option">
                      <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                           stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                        <path d="M18.5 2.5a2.1 2.1 0 0 1 3 3L12 15l-4 1 1-4Z" />
                      </svg>
                    </button>
                    <button type="button" class="shrink-0 rounded-md p-1 text-ink-300 transition hover:bg-rose-50 hover:text-danger"
                            (click)="askDelete(tab() === 'modifiers' ? 'modifier' : 'addOn', opt.id, opt.name)"
                            aria-label="Delete option">
                      <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                           stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M18 6 6 18M6 6l12 12" />
                      </svg>
                    </button>
                  </li>
                } @empty {
                  <li class="px-4 py-6 text-center text-xs text-ink-400">
                    No options in this group yet.
                  </li>
                }
              </ul>

              <footer class="border-t border-ink-100 bg-ink-50/60 px-4 py-2.5">
                <button type="button" class="text-xs font-semibold text-brand-600 hover:text-brand-700"
                        (click)="openItemForm(undefined, group.categoryId)">
                  + Add option
                </button>
              </footer>
            </section>
          }
        </div>
      }
    </div>

    <!-- ========================= group form ========================= -->
    @if (categoryFormOpen()) {
      <app-modal
        [heading]="editingCategoryId() ? 'Edit group' : (tab() === 'modifiers' ? 'New modifier group' : 'New add-on group')"
        maxWidth="28rem"
        (closed)="categoryFormOpen.set(false)"
      >
        <form [formGroup]="categoryForm" class="space-y-4">
          <div>
            <label class="label" for="groupName">Group name <span class="text-danger">*</span></label>
            <input id="groupName" type="text" class="input" formControlName="name"
                   [placeholder]="tab() === 'modifiers' ? 'Size' : 'Extras'" />
            @if (categoryForm.controls.name.invalid && categoryForm.controls.name.touched) {
              <p class="field-error">A group name is required.</p>
            }
          </div>

          @if (tab() === 'modifiers') {
            <label class="flex cursor-pointer items-center gap-2.5 text-sm font-medium text-ink-700">
              <input type="checkbox" class="h-4 w-4 rounded accent-brand-500" formControlName="isRequired" />
              Customers must choose one
            </label>
          } @else {
            <div class="grid gap-4 sm:grid-cols-2">
              <div>
                <label class="label" for="minSelect">Minimum selections</label>
                <input id="minSelect" type="number" min="0" step="1" class="input" formControlName="minSelect" />
              </div>
              <div>
                <label class="label" for="maxSelect">Maximum selections</label>
                <input id="maxSelect" type="number" min="0" step="1" class="input" formControlName="maxSelect" />
                <p class="mt-1 text-[11px] text-ink-400">0 means unlimited.</p>
              </div>
            </div>
          }

          <label class="flex cursor-pointer items-center gap-2.5 text-sm font-medium text-ink-700">
            <input type="checkbox" class="h-4 w-4 rounded accent-brand-500" formControlName="isActive" />
            Active
          </label>
        </form>

        <div modalFooter class="flex justify-end gap-2">
          <button type="button" class="btn-secondary" (click)="categoryFormOpen.set(false)">Cancel</button>
          <button type="button" class="btn-primary" [disabled]="saving()" (click)="saveCategory()">
            @if (saving()) { <app-spinner [size]="14" /> }
            Save group
          </button>
        </div>
      </app-modal>
    }

    <!-- ========================= option form ========================= -->
    @if (itemFormOpen()) {
      <app-modal
        [heading]="editingItemId() ? 'Edit option' : (tab() === 'modifiers' ? 'New modifier' : 'New add-on')"
        maxWidth="28rem"
        (closed)="itemFormOpen.set(false)"
      >
        <form [formGroup]="itemForm" class="space-y-4">
          <div>
            <label class="label" for="optionGroup">Group <span class="text-danger">*</span></label>
            <select id="optionGroup" class="input" formControlName="categoryId">
              @for (c of currentCategories(); track categoryIdOf(c)) {
                <option [value]="categoryIdOf(c)">{{ categoryNameOf(c) }}</option>
              }
            </select>
          </div>
          <div>
            <label class="label" for="optionName">Name <span class="text-danger">*</span></label>
            <input id="optionName" type="text" class="input" formControlName="name"
                   [placeholder]="tab() === 'modifiers' ? 'Large' : 'Extra cheese'" />
            @if (itemForm.controls.name.invalid && itemForm.controls.name.touched) {
              <p class="field-error">A name is required.</p>
            }
          </div>
          <div>
            <label class="label" for="optionPrice">Price</label>
            <input id="optionPrice" type="number" min="0" step="0.01" class="input" formControlName="price" />
            <p class="mt-1 text-[11px] text-ink-400">Leave at 0 for a free option.</p>
          </div>
          <label class="flex cursor-pointer items-center gap-2.5 text-sm font-medium text-ink-700">
            <input type="checkbox" class="h-4 w-4 rounded accent-brand-500" formControlName="isActive" />
            Active
          </label>
        </form>

        <div modalFooter class="flex justify-end gap-2">
          <button type="button" class="btn-secondary" (click)="itemFormOpen.set(false)">Cancel</button>
          <button type="button" class="btn-primary" [disabled]="saving()" (click)="saveItem()">
            @if (saving()) { <app-spinner [size]="14" /> }
            Save option
          </button>
        </div>
      </app-modal>
    }

    @if (deleteTarget(); as target) {
      <app-confirm
        heading="Delete this item?"
        [message]="'“' + target.label + '” will be removed. Products using it lose the option.'"
        [busy]="deleting()"
        (confirmed)="confirmDelete()"
        (cancelled)="deleteTarget.set(null)"
      />
    }
  `,
})
export class AdminModifiersPage implements OnInit {
  private readonly admin = inject(AdminService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  readonly tabs: { id: Tab; label: string }[] = [
    { id: 'modifiers', label: 'Modifiers' },
    { id: 'addons', label: 'Add-ons' },
  ];

  readonly tab = signal<Tab>('modifiers');
  readonly modifierCategories = signal<ModifierCategory[]>([]);
  readonly modifiers = signal<Modifier[]>([]);
  readonly addOnCategories = signal<AddOnCategory[]>([]);
  readonly addOns = signal<AddOn[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly categoryFormOpen = signal(false);
  readonly editingCategoryId = signal<number | null>(null);
  readonly itemFormOpen = signal(false);
  readonly editingItemId = signal<number | null>(null);
  readonly saving = signal(false);

  readonly deleteTarget = signal<{ kind: DeleteKind; id: number; label: string } | null>(null);
  readonly deleting = signal(false);

  readonly headOfficeId = computed(() => this.auth.headOfficeId());

  readonly categoryForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    isRequired: [false],
    minSelect: [0],
    maxSelect: [0],
    isActive: [true],
  });

  readonly itemForm = this.fb.nonNullable.group({
    categoryId: [0, Validators.required],
    name: ['', [Validators.required, Validators.maxLength(100)]],
    price: [0, Validators.min(0)],
    isActive: [true],
  });

  readonly currentCategories = computed<(ModifierCategory | AddOnCategory)[]>(() =>
    this.tab() === 'modifiers' ? this.modifierCategories() : this.addOnCategories(),
  );

  /** Categories with their options nested, plus a human rule summary. */
  readonly grouped = computed(() => {
    if (this.tab() === 'modifiers') {
      return this.modifierCategories().map((c) => {
        const id = this.categoryIdOf(c);
        return {
          categoryId: id,
          name: this.categoryNameOf(c),
          rule: c.isRequired ? 'Required · choose one' : 'Optional',
          options: this.modifiers()
            .filter((m) => m.categoryId === id)
            .map((m) => ({ id: m.id, name: m.name ?? '', price: m.defaultPrice ?? 0 })),
        };
      });
    }
    return this.addOnCategories().map((c) => {
      const id = this.categoryIdOf(c);
      const min = c.minSelect ?? 0;
      const max = c.maxSelect ?? 0;
      return {
        categoryId: id,
        name: this.categoryNameOf(c),
        rule: max ? `Choose ${min}–${max}` : min ? `Choose at least ${min}` : 'Optional · any number',
        options: this.addOns()
          .filter((a) => a.addOnCategoryId === id)
          .map((a) => ({ id: a.id, name: a.name ?? '', price: a.addOnUnitPrice ?? 0 })),
      };
    });
  });

  ngOnInit(): void {
    this.load();
  }

  /** The API is inconsistent: some projections use `categoryId`, others `id`. */
  categoryIdOf(c: ModifierCategory | AddOnCategory): number {
    return c.categoryId ?? c.id ?? 0;
  }

  categoryNameOf(c: ModifierCategory | AddOnCategory): string {
    return c.categoryName ?? c.name ?? 'Untitled group';
  }

  load(): void {
    const hoId = this.headOfficeId();
    if (!hoId) return;

    this.loading.set(true);
    this.error.set(null);
    let pending = 4;
    const done = () => {
      if (--pending === 0) this.loading.set(false);
    };
    const fail = (err: Error) => {
      this.error.set(err.message);
      this.loading.set(false);
    };

    this.admin.modifierCategories(hoId).subscribe({
      next: (l) => { this.modifierCategories.set(l); done(); }, error: fail,
    });
    this.admin.modifiers(hoId).subscribe({
      next: (l) => { this.modifiers.set(l); done(); }, error: fail,
    });
    this.admin.addOnCategories(hoId).subscribe({
      next: (l) => { this.addOnCategories.set(l); done(); }, error: fail,
    });
    this.admin.addOns(hoId).subscribe({
      next: (l) => { this.addOns.set(l); done(); }, error: fail,
    });
  }

  /* ---------------------------- categories ---------------------------- */

  openCategoryForm(categoryId?: number): void {
    this.editingCategoryId.set(categoryId ?? null);
    const existing = categoryId
      ? this.currentCategories().find((c) => this.categoryIdOf(c) === categoryId)
      : undefined;

    this.categoryForm.reset({
      name: existing ? this.categoryNameOf(existing) : '',
      isRequired: (existing as ModifierCategory | undefined)?.isRequired ?? false,
      minSelect: (existing as AddOnCategory | undefined)?.minSelect ?? 0,
      maxSelect: (existing as AddOnCategory | undefined)?.maxSelect ?? 0,
      isActive: existing?.isActive ?? true,
    });
    this.categoryFormOpen.set(true);
  }

  saveCategory(): void {
    if (this.categoryForm.invalid) {
      this.categoryForm.markAllAsTouched();
      return;
    }
    const v = this.categoryForm.getRawValue();
    const id = this.editingCategoryId();
    const hoId = this.headOfficeId()!;
    this.saving.set(true);

    const req: Observable<unknown> =
      this.tab() === 'modifiers'
        ? (() => {
            const body: Partial<ModifierCategory> = {
              categoryName: v.name,
              name: v.name,
              isRequired: v.isRequired,
              isActive: v.isActive,
              headOfficeId: hoId,
            };
            return id
              ? this.admin.updateModifierCategory(id, { ...body, categoryId: id, id })
              : this.admin.createModifierCategory(body);
          })()
        : (() => {
            const body: Partial<AddOnCategory> = {
              categoryName: v.name,
              name: v.name,
              minSelect: v.minSelect,
              maxSelect: v.maxSelect,
              isActive: v.isActive,
              headOfficeId: hoId,
            };
            return id
              ? this.admin.updateAddOnCategory(id, { ...body, categoryId: id, id })
              : this.admin.createAddOnCategory(body);
          })();

    req.subscribe({
      next: () => {
        this.saving.set(false);
        this.categoryFormOpen.set(false);
        this.toast.success('Group saved.');
        this.load();
      },
      error: (err: Error) => {
        this.saving.set(false);
        this.toast.error(err.message);
      },
    });
  }

  /* ------------------------------ options ------------------------------ */

  openItemForm(itemId?: number, presetCategoryId?: number): void {
    this.editingItemId.set(itemId ?? null);
    const fallbackCategory = presetCategoryId ?? this.categoryIdOf(this.currentCategories()[0]);

    if (this.tab() === 'modifiers') {
      const m = itemId ? this.modifiers().find((x) => x.id === itemId) : undefined;
      this.itemForm.reset({
        categoryId: m?.categoryId ?? fallbackCategory,
        name: m?.name ?? '',
        price: m?.defaultPrice ?? 0,
        isActive: m?.isActive ?? true,
      });
    } else {
      const a = itemId ? this.addOns().find((x) => x.id === itemId) : undefined;
      this.itemForm.reset({
        categoryId: a?.addOnCategoryId ?? fallbackCategory,
        name: a?.name ?? '',
        price: a?.addOnUnitPrice ?? 0,
        isActive: a?.isActive ?? true,
      });
    }
    this.itemFormOpen.set(true);
  }

  saveItem(): void {
    if (this.itemForm.invalid) {
      this.itemForm.markAllAsTouched();
      return;
    }
    const v = this.itemForm.getRawValue();
    const id = this.editingItemId();
    const hoId = this.headOfficeId()!;
    this.saving.set(true);

    const req: Observable<unknown> =
      this.tab() === 'modifiers'
        ? (() => {
            const body: Partial<Modifier> = {
              name: v.name,
              categoryId: Number(v.categoryId),
              defaultPrice: v.price,
              isActive: v.isActive,
              isDeleted: false,
              headOfficeId: hoId,
            };
            return id ? this.admin.updateModifier(id, { ...body, id }) : this.admin.createModifier(body);
          })()
        : (() => {
            const body: Partial<AddOn> = {
              name: v.name,
              addOnCategoryId: Number(v.categoryId),
              addOnUnitPrice: v.price,
              isActive: v.isActive,
              isDeleted: false,
              headOfficeId: hoId,
            };
            return id ? this.admin.updateAddOn(id, { ...body, id }) : this.admin.createAddOn(body);
          })();

    req.subscribe({
      next: () => {
        this.saving.set(false);
        this.itemFormOpen.set(false);
        this.toast.success('Option saved.');
        this.load();
      },
      error: (err: Error) => {
        this.saving.set(false);
        this.toast.error(err.message);
      },
    });
  }

  /* ------------------------------ delete ------------------------------ */

  askDelete(kind: DeleteKind, id: number, label: string): void {
    this.deleteTarget.set({ kind, id, label });
  }

  confirmDelete(): void {
    const target = this.deleteTarget();
    if (!target) return;
    this.deleting.set(true);

    const req = {
      modifierCategory: () => this.admin.deleteModifierCategory(target.id),
      modifier: () => this.admin.deleteModifier(target.id),
      addOnCategory: () => this.admin.deleteAddOnCategory(target.id),
      addOn: () => this.admin.deleteAddOn(target.id),
    }[target.kind]();

    req.subscribe({
      next: () => {
        this.deleting.set(false);
        this.deleteTarget.set(null);
        this.toast.success('Deleted.');
        this.load();
      },
      error: (err: Error) => {
        this.deleting.set(false);
        this.toast.error(err.message);
      },
    });
  }
}
