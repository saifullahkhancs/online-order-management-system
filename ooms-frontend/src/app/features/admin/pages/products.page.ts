import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Product } from '../../../core/models/catalog.models';
import { AdminService } from '../../../core/services/admin.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { MoneyPipe } from '../../../core/utils/format';
import {
  ConfirmComponent,
  EmptyStateComponent,
  ErrorStateComponent,
  ImgComponent,
  LoadingComponent,
  ModalComponent,
  SpinnerComponent,
} from '../../../shared/components/ui.components';

/**
 * Product catalogue.
 *
 * `POST/PUT /api/Products` are `[FromForm]` and accept an optional `imageFile`
 * which the controller uploads to Cloudinary, so this page posts FormData
 * rather than JSON. Leaving the file empty keeps the existing image.
 */
@Component({
  selector: 'app-admin-products-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    MoneyPipe,
    ImgComponent,
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
          <h1 class="text-2xl font-bold tracking-tight text-ink-900">Products</h1>
          <p class="mt-1 text-sm text-ink-500">
            The master catalogue. Publish products to a branch from
            <strong class="font-semibold text-ink-700">Branch menu</strong>.
          </p>
        </div>
        <button type="button" class="btn-primary" (click)="openForm()">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.6"
               stroke-linecap="round"><path d="M12 5v14M5 12h14" /></svg>
          New product
        </button>
      </header>

      <div class="mb-4 flex flex-wrap items-center gap-3">
        <div class="relative min-w-48 flex-1 sm:max-w-xs">
          <svg class="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-ink-400"
               width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2">
            <circle cx="11" cy="11" r="7" /><path d="m20 20-3.5-3.5" stroke-linecap="round" />
          </svg>
          <input type="search" class="input py-2 pl-9 text-xs" placeholder="Search products…"
                 [ngModel]="query()" (ngModelChange)="query.set($event)" aria-label="Search products" />
        </div>
        <span class="text-xs text-ink-500">{{ filtered().length }} of {{ products().length }}</span>
      </div>

      @if (loading()) {
        <app-loading label="Loading products…" />
      } @else if (error()) {
        <app-error-state title="Could not load products" [message]="error()!" (retry)="load()" />
      } @else if (!filtered().length) {
        <div class="card">
          <app-empty-state
            title="No products"
            [message]="query() ? 'Nothing matches your search.' : 'Add your first product to get started.'"
          >
            @if (!query()) {
              <button type="button" class="btn-primary mt-2" (click)="openForm()">New product</button>
            }
          </app-empty-state>
        </div>
      } @else {
        <div class="table-wrap">
          <table class="tbl">
            <thead>
              <tr>
                <th class="w-16">Image</th>
                <th>Product</th>
                <th class="w-28 text-right">Price</th>
                <th class="w-28">Status</th>
                <th class="w-24 text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              @for (p of filtered(); track p.productId) {
                <tr>
                  <td>
                    <div class="h-10 w-10 overflow-hidden rounded-lg bg-ink-100">
                      <app-img [src]="p.imageUrl" [alt]="p.productName ?? ''" />
                    </div>
                  </td>
                  <td>
                    <p class="font-semibold text-ink-900">{{ p.productName }}</p>
                    @if (p.description) {
                      <p class="mt-0.5 line-clamp-1 max-w-md text-xs text-ink-500">{{ p.description }}</p>
                    }
                  </td>
                  <td class="text-right font-semibold tabular-nums">{{ p.price | money }}</td>
                  <td>
                    <span class="badge"
                          [class]="p.isAvailable === false
                            ? 'bg-ink-100 text-ink-600'
                            : 'bg-emerald-100 text-emerald-700'">
                      {{ p.isAvailable === false ? 'Unavailable' : 'Available' }}
                    </span>
                  </td>
                  <td>
                    <div class="flex justify-end gap-1">
                      <button type="button" class="rounded-md p-1.5 text-ink-400 transition hover:bg-ink-100 hover:text-ink-800"
                              (click)="openForm(p)" aria-label="Edit" title="Edit">
                        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                             stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                          <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                          <path d="M18.5 2.5a2.1 2.1 0 0 1 3 3L12 15l-4 1 1-4Z" />
                        </svg>
                      </button>
                      <button type="button" class="rounded-md p-1.5 text-ink-400 transition hover:bg-rose-50 hover:text-danger"
                              (click)="deleteTarget.set(p)" aria-label="Delete" title="Delete">
                        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                             stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                          <path d="M3 6h18M8 6V4a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1v2M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                        </svg>
                      </button>
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>

    <!-- ========================== form modal ========================== -->
    @if (formOpen()) {
      <app-modal
        [heading]="editing() ? 'Edit product' : 'New product'"
        subheading="Products belong to a head office and are published per branch."
        maxWidth="32rem"
        (closed)="closeForm()"
      >
        <form [formGroup]="form" class="space-y-4">
          <div>
            <label class="label" for="productName">Name <span class="text-danger">*</span></label>
            <input id="productName" type="text" class="input" formControlName="productName"
                   placeholder="Classic Beef Burger" />
            @if (invalid('productName')) { <p class="field-error">A product name is required.</p> }
          </div>

          <div>
            <label class="label" for="description">Description</label>
            <textarea id="description" rows="2" maxlength="500" class="input resize-none"
                      formControlName="description"
                      placeholder="180g beef patty, cheddar, lettuce, house sauce"></textarea>
          </div>

          <div class="grid gap-4 sm:grid-cols-2">
            <div>
              <label class="label" for="price">Price <span class="text-danger">*</span></label>
              <input id="price" type="number" min="0" step="0.01" class="input" formControlName="price" />
              @if (invalid('price')) { <p class="field-error">Enter a price of 0 or more.</p> }
            </div>
            <div class="flex items-end pb-2.5">
              <label class="flex cursor-pointer items-center gap-2.5 text-sm font-medium text-ink-700">
                <input type="checkbox" class="h-4 w-4 rounded accent-brand-500" formControlName="isAvailable" />
                Available for ordering
              </label>
            </div>
          </div>

          <!-- image -->
          <div>
            <label class="label">Image</label>
            <div class="flex items-center gap-4">
              <div class="h-20 w-20 shrink-0 overflow-hidden rounded-lg bg-ink-100 ring-1 ring-ink-200">
                <app-img [src]="previewUrl() ?? editing()?.imageUrl" alt="Product image preview" />
              </div>
              <div class="min-w-0 flex-1">
                <input type="file" accept="image/*" class="block w-full text-xs text-ink-600
                         file:mr-3 file:rounded-lg file:border-0 file:bg-ink-100 file:px-3 file:py-2
                         file:text-xs file:font-semibold file:text-ink-700 hover:file:bg-ink-200"
                       (change)="pickImage($event)" />
                <p class="mt-1.5 text-[11px] leading-relaxed text-ink-400">
                  Uploaded to Cloudinary by the API. Leave empty to keep the current image.
                </p>
                @if (imageFile()) {
                  <button type="button" class="mt-1 text-[11px] font-semibold text-danger" (click)="clearImage()">
                    Remove selected file
                  </button>
                }
              </div>
            </div>
          </div>
        </form>

        <div modalFooter class="flex justify-end gap-2">
          <button type="button" class="btn-secondary" (click)="closeForm()">Cancel</button>
          <button type="button" class="btn-primary" [disabled]="saving()" (click)="save()">
            @if (saving()) { <app-spinner [size]="14" /> }
            {{ editing() ? 'Save changes' : 'Create product' }}
          </button>
        </div>
      </app-modal>
    }

    <!-- ========================= delete confirm ========================= -->
    @if (deleteTarget(); as target) {
      <app-confirm
        heading="Delete this product?"
        [message]="'“' + target.productName + '” will be removed from the catalogue and every branch menu.'"
        [busy]="deleting()"
        (confirmed)="confirmDelete()"
        (cancelled)="deleteTarget.set(null)"
      />
    }
  `,
})
export class AdminProductsPage implements OnInit {
  private readonly admin = inject(AdminService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  readonly products = signal<Product[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly query = signal('');

  readonly formOpen = signal(false);
  readonly editing = signal<Product | null>(null);
  readonly saving = signal(false);
  readonly imageFile = signal<File | null>(null);
  readonly previewUrl = signal<string | null>(null);

  readonly deleteTarget = signal<Product | null>(null);
  readonly deleting = signal(false);

  readonly form = this.fb.nonNullable.group({
    productName: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', Validators.maxLength(500)],
    price: [0, [Validators.required, Validators.min(0)]],
    isAvailable: [true],
  });

  readonly filtered = computed(() => {
    const q = this.query().trim().toLowerCase();
    if (!q) return this.products();
    return this.products().filter((p) =>
      [p.productName, p.description].filter(Boolean).some((v) => v!.toLowerCase().includes(q)),
    );
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.admin.products().subscribe({
      next: (list) => {
        this.products.set(list);
        this.loading.set(false);
      },
      error: (err: Error) => {
        this.error.set(err.message);
        this.loading.set(false);
      },
    });
  }

  invalid(name: keyof typeof this.form.controls): boolean {
    const c = this.form.controls[name];
    return c.invalid && (c.touched || c.dirty);
  }

  openForm(p?: Product): void {
    this.editing.set(p ?? null);
    this.clearImage();
    this.form.reset({
      productName: p?.productName ?? '',
      description: p?.description ?? '',
      price: p?.price ?? 0,
      isAvailable: p?.isAvailable ?? true,
    });
    this.formOpen.set(true);
  }

  closeForm(): void {
    this.formOpen.set(false);
    this.editing.set(null);
    this.clearImage();
  }

  pickImage(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0] ?? null;
    this.imageFile.set(file);
    this.previewUrl.set(file ? URL.createObjectURL(file) : null);
  }

  clearImage(): void {
    const url = this.previewUrl();
    if (url) URL.revokeObjectURL(url);
    this.imageFile.set(null);
    this.previewUrl.set(null);
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const existing = this.editing();

    // headOfficeId comes from the JWT: products are scoped to the signed-in
    // user's head office.
    const payload: Partial<Product> = {
      productName: v.productName,
      description: v.description || null,
      price: v.price,
      isAvailable: v.isAvailable,
      isDeleted: false,
      headOfficeId: existing?.headOfficeId ?? this.auth.headOfficeId() ?? undefined,
    };

    this.saving.set(true);
    const req = existing
      ? this.admin.updateProduct(existing.productId, payload, this.imageFile())
      : this.admin.createProduct(payload, this.imageFile());

    req.subscribe({
      next: () => {
        this.saving.set(false);
        this.toast.success(existing ? 'Product updated.' : 'Product created.');
        this.closeForm();
        this.load();
      },
      error: (err: Error) => {
        this.saving.set(false);
        this.toast.error(err.message);
      },
    });
  }

  confirmDelete(): void {
    const target = this.deleteTarget();
    if (!target) return;
    this.deleting.set(true);
    this.admin.deleteProduct(target.productId).subscribe({
      next: () => {
        this.deleting.set(false);
        this.deleteTarget.set(null);
        this.toast.success('Product deleted.');
        this.load();
      },
      error: (err: Error) => {
        this.deleting.set(false);
        this.toast.error(err.message);
      },
    });
  }
}
