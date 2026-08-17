import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Category, Product } from '../../../core/models/catalog.models';
import { CartService } from '../../../core/services/cart.service';
import { MenuService } from '../../../core/services/menu.service';
import { MoneyPipe } from '../../../core/utils/format';
import {
  EmptyStateComponent,
  ErrorStateComponent,
  ImgComponent,
  LoadingComponent,
} from '../../../shared/components/ui.components';
import { BranchContextService } from '../branch-context.service';
import { ProductSheetComponent } from '../components/product-sheet.component';

/**
 * The menu.
 *
 * Layout: sticky category rail on the left (desktop) / chip row (mobile),
 * product grid on the right, and a floating "view cart" bar on mobile.
 * Clicking a product opens <app-product-sheet> to configure and add it.
 *
 * Deep links supported:
 *   /menu?category=3   scrolls to a category
 *   /menu?product=12   opens that product's sheet straight away
 */
@Component({
  selector: 'app-menu-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    RouterLink,
    MoneyPipe,
    ImgComponent,
    LoadingComponent,
    ErrorStateComponent,
    EmptyStateComponent,
    ProductSheetComponent,
  ],
  template: `
    <section class="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
      <!-- =========================== header =========================== -->
      <header class="mb-6 flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 class="text-3xl font-bold tracking-tight text-ink-900">Menu</h1>
          <p class="mt-1.5 text-sm text-ink-500">
            @if (branchCtx.selected(); as b) {
              Ordering from <span class="font-semibold text-ink-700">{{ b.branchName }}</span>
              <a routerLink="/branches" class="ml-2 font-semibold text-brand-600 hover:text-brand-700">change</a>
            } @else {
              Choose a branch to see the menu.
            }
          </p>
        </div>

        <div class="relative w-full sm:w-72">
          <svg class="pointer-events-none absolute top-1/2 left-3.5 -translate-y-1/2 text-ink-400"
               width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2">
            <circle cx="11" cy="11" r="7" /><path d="m20 20-3.5-3.5" stroke-linecap="round" />
          </svg>
          <input
            type="search"
            class="input pl-10"
            placeholder="Search the menu…"
            [ngModel]="query()"
            (ngModelChange)="query.set($event)"
            aria-label="Search the menu"
          />
        </div>
      </header>

      @if (!branchCtx.hasSelection() && !branchCtx.loading()) {
        <app-empty-state
          title="No branch selected"
          message="Pick the branch you would like to order from and the menu will load."
        >
          <a routerLink="/branches" class="btn-primary mt-2">Choose a branch</a>
        </app-empty-state>
      } @else if (loading()) {
        <app-loading label="Loading the menu…" />
      } @else if (error()) {
        <app-error-state title="Could not load the menu" [message]="error()!" (retry)="reload()" />
      } @else if (!categories().length) {
        <app-empty-state
          title="This branch has no items yet"
          message="An administrator needs to publish products to this branch from the admin console."
        />
      } @else {
        <div class="lg:flex lg:gap-8">
          <!-- ===================== category rail ===================== -->
          <aside class="lg:w-52 lg:shrink-0">
            <!-- mobile: horizontal chips -->
            <div class="sticky top-16 z-20 -mx-4 mb-5 overflow-x-auto bg-ink-50/95 px-4 py-2.5 backdrop-blur lg:hidden">
              <div class="flex gap-2">
                @for (c of categories(); track c.categoryId) {
                  <button
                    type="button"
                    class="shrink-0 rounded-pill px-3.5 py-1.5 text-xs font-semibold whitespace-nowrap transition"
                    [class]="
                      activeCategory() === c.categoryId
                        ? 'bg-ink-900 text-white'
                        : 'bg-white text-ink-600 ring-1 ring-ink-200 hover:bg-ink-100'
                    "
                    (click)="scrollTo(c.categoryId)"
                  >
                    {{ c.categoryName }}
                  </button>
                }
              </div>
            </div>

            <!-- desktop: vertical list -->
            <nav class="sticky top-24 hidden lg:block" aria-label="Menu categories">
              <p class="mb-2 px-3 text-[11px] font-semibold tracking-wide text-ink-400 uppercase">
                Categories
              </p>
              <ul class="space-y-0.5">
                @for (c of categories(); track c.categoryId) {
                  <li>
                    <button
                      type="button"
                      class="flex w-full items-center justify-between gap-2 rounded-lg px-3 py-2 text-left
                             text-sm font-medium transition"
                      [class]="
                        activeCategory() === c.categoryId
                          ? 'bg-brand-50 text-brand-700 ring-1 ring-brand-200'
                          : 'text-ink-600 hover:bg-ink-100 hover:text-ink-900'
                      "
                      (click)="scrollTo(c.categoryId)"
                    >
                      <span class="truncate">{{ c.categoryName }}</span>
                      <span class="shrink-0 text-[11px] text-ink-400">{{ c.products?.length ?? 0 }}</span>
                    </button>
                  </li>
                }
              </ul>
            </nav>
          </aside>

          <!-- ======================= product grid ======================= -->
          <div class="min-w-0 flex-1">
            @if (query() && !visible().length) {
              <app-empty-state
                title="No matches"
                [message]="'Nothing on this menu matches “' + query() + '”.'"
              />
            }

            @for (c of visible(); track c.categoryId) {
              <section [id]="'cat-' + c.categoryId" class="mb-10 scroll-mt-28">
                <header class="mb-4">
                  <h2 class="text-xl font-bold tracking-tight text-ink-900">{{ c.categoryName }}</h2>
                  @if (c.description) {
                    <p class="mt-0.5 text-sm text-ink-500">{{ c.description }}</p>
                  }
                </header>

                <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
                  @for (p of c.products ?? []; track p.productId) {
                    <article
                      class="group card flex overflow-hidden transition-all hover:-translate-y-0.5 hover:shadow-pop"
                      [class.opacity-60]="p.isAvailable === false"
                    >
                      <button
                        type="button"
                        class="flex flex-1 items-stretch gap-0 text-left"
                        [disabled]="p.isAvailable === false"
                        (click)="open(p)"
                      >
                        <div class="min-w-0 flex-1 p-4">
                          <h3 class="text-sm font-semibold text-ink-900">{{ p.productName }}</h3>
                          @if (p.description) {
                            <p class="mt-1 line-clamp-2 text-xs leading-relaxed text-ink-500">
                              {{ p.description }}
                            </p>
                          }
                          <div class="mt-2.5 flex items-center gap-2">
                            <span class="text-sm font-bold text-brand-600">{{ p.price | money }}</span>
                            @if (inCartQty(p.productId); as q) {
                              <span class="badge bg-emerald-100 text-emerald-700">{{ q }} in cart</span>
                            }
                            @if (p.isAvailable === false) {
                              <span class="badge bg-ink-100 text-ink-500">Unavailable</span>
                            }
                          </div>
                        </div>

                        <div class="relative w-28 shrink-0 self-stretch overflow-hidden bg-ink-100">
                          <app-img
                            [src]="p.imageUrl"
                            [alt]="p.productName ?? ''"
                            cssClass="h-full w-full object-cover transition duration-300 group-hover:scale-105"
                          />
                          @if (p.isAvailable !== false) {
                            <span
                              class="absolute right-2 bottom-2 grid h-7 w-7 place-items-center rounded-full
                                     bg-white text-brand-600 shadow-md transition group-hover:bg-brand-500
                                     group-hover:text-white"
                              aria-hidden="true"
                            >
                              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                                   stroke-width="3" stroke-linecap="round">
                                <path d="M12 5v14M5 12h14" />
                              </svg>
                            </span>
                          }
                        </div>
                      </button>
                    </article>
                  }
                </div>
              </section>
            }
          </div>
        </div>
      }
    </section>

    <!-- ==================== floating cart bar (mobile) ==================== -->
    @if (cart.count() > 0) {
      <div class="fixed inset-x-0 bottom-16 z-30 px-4 md:hidden">
        <a
          routerLink="/cart"
          class="animate-fade-up flex items-center justify-between gap-3 rounded-card bg-ink-900 px-4 py-3
                 text-white shadow-pop"
        >
          <span class="flex items-center gap-2.5 text-sm font-semibold">
            <span class="grid h-6 min-w-6 place-items-center rounded-full bg-brand-500 px-1.5 text-xs font-bold">
              {{ cart.count() }}
            </span>
            View cart
          </span>
          <span class="text-sm font-bold tabular-nums">{{ cart.grandTotal() | money }}</span>
        </a>
      </div>
    }

    <!-- ========================= product sheet ========================= -->
    @if (sheetProductId(); as pid) {
      <app-product-sheet
        [productId]="pid"
        [branchId]="branchCtx.selectedId()!"
        (close)="closeSheet()"
      />
    }
  `,
})
export class MenuPage implements OnInit {
  readonly branchCtx = inject(BranchContextService);
  readonly cart = inject(CartService);
  private readonly menuService = inject(MenuService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly categories = signal<Category[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly query = signal('');
  readonly activeCategory = signal<number | null>(null);
  readonly sheetProductId = signal<number | null>(null);

  /** Categories filtered by the search box; empty categories are dropped. */
  readonly visible = computed<Category[]>(() => {
    const q = this.query().trim().toLowerCase();
    if (!q) return this.categories();
    return this.categories()
      .map((c) => ({
        ...c,
        products: (c.products ?? []).filter((p) =>
          [p.productName, p.description].filter(Boolean).some((v) => v!.toLowerCase().includes(q)),
        ),
      }))
      .filter((c) => (c.products?.length ?? 0) > 0);
  });

  ngOnInit(): void {
    this.branchCtx.load().subscribe(() => {
      const id = this.branchCtx.selectedId();
      if (id !== null) this.load(id);
    });

    // Deep links
    const qp = this.route.snapshot.queryParamMap;
    const product = Number(qp.get('product'));
    if (product) this.sheetProductId.set(product);
  }

  reload(): void {
    const id = this.branchCtx.selectedId();
    if (id !== null) this.load(id);
  }

  private load(branchId: number): void {
    this.loading.set(true);
    this.error.set(null);
    this.menuService.menu(branchId).subscribe({
      next: (cats) => {
        this.categories.set(cats);
        this.loading.set(false);
        const wanted = Number(this.route.snapshot.queryParamMap.get('category'));
        const first = cats[0]?.categoryId ?? null;
        this.activeCategory.set(wanted || first);
        if (wanted) setTimeout(() => this.scrollTo(wanted), 60);
      },
      error: (err: Error) => {
        this.error.set(err.message);
        this.loading.set(false);
      },
    });
  }

  scrollTo(categoryId: number): void {
    this.activeCategory.set(categoryId);
    document.getElementById(`cat-${categoryId}`)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  inCartQty(productId: number): number {
    return this.cart.items().find((i) => i.productId === productId)?.quantity ?? 0;
  }

  open(p: Product): void {
    if (p.isAvailable === false) return;
    this.sheetProductId.set(p.productId);
  }

  closeSheet(): void {
    this.sheetProductId.set(null);
    // Drop ?product= so a refresh does not reopen the sheet.
    if (this.route.snapshot.queryParamMap.has('product')) {
      void this.router.navigate([], {
        relativeTo: this.route,
        queryParams: { product: null },
        queryParamsHandling: 'merge',
        replaceUrl: true,
      });
    }
  }
}
