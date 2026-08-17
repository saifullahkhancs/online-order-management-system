import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Branch, BranchProduct, Product } from '../../../core/models/catalog.models';
import { AdminService } from '../../../core/services/admin.service';
import { ToastService } from '../../../core/services/toast.service';
import { MoneyPipe } from '../../../core/utils/format';
import {
  EmptyStateComponent,
  ErrorStateComponent,
  ImgComponent,
  LoadingComponent,
  SpinnerComponent,
} from '../../../shared/components/ui.components';

interface Row {
  product: Product;
  link: BranchProduct | null;
  /** Branch price being edited; falls back to the catalogue price. */
  price: number;
  active: boolean;
  saving: boolean;
}

/**
 * Branch menu publishing.
 *
 * `BranchProduct` is what actually makes a product orderable at a branch and
 * carries the branch-specific price. `GET /api/Menus?branchId=` only returns
 * products with an active row here, so this page is the bridge between the
 * catalogue and the storefront.
 */
@Component({
  selector: 'app-admin-branch-menu-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    MoneyPipe,
    ImgComponent,
    LoadingComponent,
    ErrorStateComponent,
    EmptyStateComponent,
    SpinnerComponent,
  ],
  template: `
    <div class="p-4 sm:p-6 lg:p-8">
      <header class="mb-5">
        <h1 class="text-2xl font-bold tracking-tight text-ink-900">Branch menu</h1>
        <p class="mt-1 text-sm text-ink-500">
          Choose which catalogue products a branch sells, and at what price.
        </p>
      </header>

      <!-- branch picker -->
      <div class="card mb-5 flex flex-wrap items-end gap-4 p-4">
        <div class="min-w-56">
          <label class="label" for="branchPick">Branch</label>
          <select id="branchPick" class="input" [ngModel]="branchId()" (ngModelChange)="selectBranch(+$event)">
            @for (b of branches(); track b.branchId) {
              <option [value]="b.branchId">{{ b.branchName }}</option>
            }
          </select>
        </div>

        <div class="relative min-w-48 flex-1 sm:max-w-xs">
          <label class="label" for="menuSearch">Search</label>
          <input id="menuSearch" type="search" class="input" placeholder="Filter products…"
                 [ngModel]="query()" (ngModelChange)="query.set($event)" />
        </div>

        <label class="mb-2.5 flex cursor-pointer items-center gap-2 text-xs font-medium text-ink-600">
          <input type="checkbox" class="h-3.5 w-3.5 accent-brand-500"
                 [ngModel]="onlyPublished()" (ngModelChange)="onlyPublished.set($event)" />
          Published only
        </label>

        <div class="mb-2 ml-auto text-xs text-ink-500">
          <strong class="font-semibold text-ink-800">{{ publishedCount() }}</strong> of
          {{ rows().length }} published
        </div>
      </div>

      @if (loading()) {
        <app-loading label="Loading menu…" />
      } @else if (error()) {
        <app-error-state title="Could not load the menu" [message]="error()!" (retry)="load()" />
      } @else if (!branches().length) {
        <div class="card">
          <app-empty-state title="No branches" message="Create a branch before publishing a menu." />
        </div>
      } @else if (!filtered().length) {
        <div class="card">
          <app-empty-state
            title="Nothing to show"
            [message]="query() ? 'No products match your filter.' : 'Add products to the catalogue first.'"
          />
        </div>
      } @else {
        <div class="table-wrap">
          <table class="tbl">
            <thead>
              <tr>
                <th class="w-16">Image</th>
                <th>Product</th>
                <th class="w-28 text-right">Catalogue</th>
                <th class="w-36">Branch price</th>
                <th class="w-28">Published</th>
                <th class="w-24 text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              @for (row of filtered(); track row.product.productId) {
                <tr [class.opacity-60]="!row.link">
                  <td>
                    <div class="h-10 w-10 overflow-hidden rounded-lg bg-ink-100">
                      <app-img [src]="row.product.imageUrl" [alt]="row.product.productName ?? ''" />
                    </div>
                  </td>
                  <td>
                    <p class="font-semibold text-ink-900">{{ row.product.productName }}</p>
                    @if (row.product.isAvailable === false) {
                      <span class="badge mt-0.5 bg-amber-100 text-amber-700">Unavailable in catalogue</span>
                    }
                  </td>
                  <td class="text-right tabular-nums text-ink-500">{{ row.product.price | money }}</td>
                  <td>
                    <input type="number" min="0" step="0.01" class="input py-1.5 text-xs"
                           [ngModel]="row.price" (ngModelChange)="setPrice(row, +$event)"
                           [attr.aria-label]="'Branch price for ' + row.product.productName" />
                  </td>
                  <td>
                    <label class="inline-flex cursor-pointer items-center gap-2">
                      <input type="checkbox" class="h-4 w-4 rounded accent-brand-500"
                             [checked]="row.active && !!row.link" (change)="togglePublish(row)"
                             [disabled]="row.saving" />
                      <span class="text-xs font-medium"
                            [class]="row.link && row.active ? 'text-emerald-700' : 'text-ink-500'">
                        {{ row.link ? (row.active ? 'Live' : 'Paused') : 'Not on menu' }}
                      </span>
                    </label>
                  </td>
                  <td>
                    <div class="flex justify-end">
                      @if (row.saving) {
                        <app-spinner [size]="15" />
                      } @else if (row.link && dirty(row)) {
                        <button type="button" class="btn-primary btn-sm" (click)="savePrice(row)">Save</button>
                      } @else if (row.link) {
                        <span class="text-[11px] text-ink-300">Saved</span>
                      }
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
})
export class AdminBranchMenuPage implements OnInit {
  private readonly admin = inject(AdminService);
  private readonly toast = inject(ToastService);

  readonly branches = signal<Branch[]>([]);
  readonly branchId = signal<number | null>(null);
  readonly rows = signal<Row[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly query = signal('');
  readonly onlyPublished = signal(false);

  private products: Product[] = [];

  readonly publishedCount = computed(() => this.rows().filter((r) => r.link && r.active).length);

  readonly filtered = computed(() => {
    const q = this.query().trim().toLowerCase();
    return this.rows().filter((r) => {
      if (this.onlyPublished() && !r.link) return false;
      if (!q) return true;
      return (r.product.productName ?? '').toLowerCase().includes(q);
    });
  });

  ngOnInit(): void {
    this.loading.set(true);
    this.admin.branches().subscribe({
      next: (list) => {
        this.branches.set(list);
        if (list.length) {
          this.branchId.set(list[0].branchId);
          this.load();
        } else {
          this.loading.set(false);
        }
      },
      error: (err: Error) => {
        this.error.set(err.message);
        this.loading.set(false);
      },
    });
  }

  selectBranch(id: number): void {
    this.branchId.set(id);
    this.load();
  }

  load(): void {
    const branchId = this.branchId();
    if (!branchId) return;

    this.loading.set(true);
    this.error.set(null);

    this.admin.products().subscribe({
      next: (products) => {
        this.products = products;
        this.admin.branchProducts(branchId).subscribe({
          next: (links) => {
            this.rows.set(this.merge(products, links));
            this.loading.set(false);
          },
          error: (err: Error) => {
            this.error.set(err.message);
            this.loading.set(false);
          },
        });
      },
      error: (err: Error) => {
        this.error.set(err.message);
        this.loading.set(false);
      },
    });
  }

  /** Left-join the catalogue against this branch's published rows. */
  private merge(products: Product[], links: BranchProduct[]): Row[] {
    const byProduct = new Map(links.map((l) => [l.productId, l]));
    return products.map((product) => {
      const link = byProduct.get(product.productId) ?? null;
      return {
        product,
        link,
        price: link?.price ?? product.price ?? 0,
        active: link?.isActive ?? false,
        saving: false,
      };
    });
  }

  private patch(target: Row, changes: Partial<Row>): void {
    this.rows.update((rows) =>
      rows.map((r) => (r.product.productId === target.product.productId ? { ...r, ...changes } : r)),
    );
  }

  dirty(row: Row): boolean {
    return !!row.link && (row.link.price ?? 0) !== row.price;
  }

  setPrice(row: Row, price: number): void {
    this.patch(row, { price: Number.isFinite(price) ? price : 0 });
  }

  togglePublish(row: Row): void {
    this.patch(row, { saving: true });

    // Not on the menu yet -> create the link.
    if (!row.link) {
      this.admin
        .createBranchProduct({
          branchId: this.branchId()!,
          productId: row.product.productId,
          price: row.price,
          isActive: true,
        })
        .subscribe({
          next: (created) => {
            this.patch(row, {
              saving: false,
              active: true,
              link: created ?? {
                branchId: this.branchId()!,
                productId: row.product.productId,
                price: row.price,
                isActive: true,
              },
            });
            this.toast.success(`${row.product.productName} added to the menu.`);
          },
          error: (err: Error) => {
            this.patch(row, { saving: false });
            this.toast.error(err.message);
          },
        });
      return;
    }

    // Already linked -> flip isActive rather than deleting, so pricing survives.
    const nextActive = !row.active;
    this.admin
      .updateBranchProduct(row.link.branchProductId!, {
        ...row.link,
        branchProductId: row.link.branchProductId,
        branchId: this.branchId()!,
        productId: row.product.productId,
        price: row.price,
        isActive: nextActive,
      })
      .subscribe({
        next: () => {
          this.patch(row, {
            saving: false,
            active: nextActive,
            link: { ...row.link!, isActive: nextActive, price: row.price },
          });
          this.toast.success(nextActive ? 'Back on the menu.' : 'Paused on this branch.');
        },
        error: (err: Error) => {
          this.patch(row, { saving: false });
          this.toast.error(err.message);
        },
      });
  }

  savePrice(row: Row): void {
    if (!row.link) return;
    this.patch(row, { saving: true });
    this.admin
      .updateBranchProduct(row.link.branchProductId!, {
        ...row.link,
        branchProductId: row.link.branchProductId,
        branchId: this.branchId()!,
        productId: row.product.productId,
        price: row.price,
        isActive: row.active,
      })
      .subscribe({
        next: () => {
          this.patch(row, { saving: false, link: { ...row.link!, price: row.price } });
          this.toast.success('Price updated.');
        },
        error: (err: Error) => {
          this.patch(row, { saving: false });
          this.toast.error(err.message);
        },
      });
  }
}
