import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { Category } from '../../../core/models/catalog.models';
import { MenuService } from '../../../core/services/menu.service';
import { ImgComponent, SpinnerComponent } from '../../../shared/components/ui.components';
import { MoneyPipe } from '../../../core/utils/format';
import { BranchContextService } from '../branch-context.service';

/**
 * Landing page: hero, branch status, category shortcuts and a few popular items.
 * Everything is derived from the selected branch's menu.
 */
@Component({
  selector: 'app-home-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, ImgComponent, SpinnerComponent, MoneyPipe],
  template: `
    <!-- ============================= hero ============================= -->
    <section class="relative overflow-hidden bg-ink-900">
      <div
        class="absolute inset-0 opacity-25"
        style="background-image:radial-gradient(circle at 20% 20%, #f97316 0, transparent 45%),
                                radial-gradient(circle at 80% 0%, #2563eb 0, transparent 40%)"
        aria-hidden="true"
      ></div>

      <div class="relative mx-auto max-w-7xl px-4 py-16 sm:px-6 sm:py-24 lg:px-8">
        <div class="max-w-2xl">
          <span
            class="badge bg-white/10 text-brand-200 ring-1 ring-white/15 backdrop-blur"
          >
            <span class="h-1.5 w-1.5 rounded-full bg-brand-400"></span>
            Order online · no account needed
          </span>

          <h1 class="mt-5 text-4xl leading-[1.1] font-extrabold tracking-tight text-white sm:text-6xl">
            Great food,<br />
            <span class="text-brand-400">delivered fast.</span>
          </h1>

          <p class="mt-5 max-w-lg text-base leading-relaxed text-ink-300 sm:text-lg">
            Browse the live menu for your nearest branch, build your order with modifiers
            and extras, and track it from the kitchen to your door.
          </p>

          <div class="mt-8 flex flex-wrap items-center gap-3">
            <a routerLink="/menu" class="btn-primary btn-lg">
              Browse the menu
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                   stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round">
                <path d="M5 12h14M13 6l6 6-6 6" />
              </svg>
            </a>
            <a routerLink="/branches" class="btn-lg btn bg-white/10 text-white ring-1 ring-white/20 hover:bg-white/15">
              Change branch
            </a>
          </div>

          <!-- branch status strip -->
          @if (branchCtx.selected(); as branch) {
            <div class="mt-8 inline-flex flex-wrap items-center gap-x-5 gap-y-2 rounded-card bg-white/5
                        px-4 py-3 text-sm text-ink-300 ring-1 ring-white/10 backdrop-blur">
              <span class="flex items-center gap-2 font-semibold text-white">
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                     stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0Z" /><circle cx="12" cy="10" r="3" />
                </svg>
                {{ branch.branchName }}
              </span>
              @if (branch.branchAddress?.city) {
                <span>{{ branch.branchAddress?.city }}</span>
              }
              @if (openNow() !== null) {
                <span
                  class="badge"
                  [class]="openNow()
                    ? 'bg-emerald-400/15 text-emerald-300 ring-1 ring-emerald-400/25'
                    : 'bg-rose-400/15 text-rose-300 ring-1 ring-rose-400/25'"
                >
                  <span class="h-1.5 w-1.5 rounded-full"
                        [class]="openNow() ? 'bg-emerald-400' : 'bg-rose-400'"></span>
                  {{ openNow() ? 'Open now' : 'Closed' }}
                </span>
              }
            </div>
          } @else if (!branchCtx.loading()) {
            <div class="mt-8 flex items-center gap-3 rounded-card bg-amber-400/10 px-4 py-3 text-sm
                        text-amber-200 ring-1 ring-amber-400/25">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2">
                <path d="M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z"
                      stroke-linejoin="round" /><path d="M12 9v4M12 17h.01" stroke-linecap="round" />
              </svg>
              Pick a branch to see its menu and prices.
              <a routerLink="/branches" class="font-semibold text-white underline underline-offset-2">Choose</a>
            </div>
          }
        </div>
      </div>
    </section>

    <!-- ========================== categories ========================== -->
    @if (branchCtx.hasSelection()) {
      <section class="mx-auto max-w-7xl px-4 py-12 sm:px-6 lg:px-8">
        @if (loading()) {
          <div class="flex items-center gap-3 text-ink-500">
            <app-spinner /> <span class="text-sm font-medium">Loading the menu…</span>
          </div>
        } @else if (categories().length) {
          <header class="mb-6 flex items-end justify-between gap-4">
            <div>
              <h2 class="text-2xl font-bold tracking-tight text-ink-900">Browse by category</h2>
              <p class="mt-1 text-sm text-ink-500">
                {{ categories().length }} categories · {{ totalProducts() }} items available today
              </p>
            </div>
            <a routerLink="/menu" class="hidden shrink-0 text-sm font-semibold text-brand-600 hover:text-brand-700 sm:block">
              See full menu →
            </a>
          </header>

          <div class="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
            @for (cat of categories(); track cat.categoryId) {
              <a
                [routerLink]="['/menu']"
                [queryParams]="{ category: cat.categoryId }"
                class="group card overflow-hidden transition-all hover:-translate-y-0.5 hover:shadow-pop"
              >
                <div class="aspect-[4/3] overflow-hidden bg-ink-100">
                  <app-img
                    [src]="cat.imageUrl"
                    [alt]="cat.categoryName ?? ''"
                    cssClass="h-full w-full object-cover transition duration-300 group-hover:scale-105"
                  />
                </div>
                <div class="p-3.5">
                  <h3 class="truncate text-sm font-semibold text-ink-900">{{ cat.categoryName }}</h3>
                  <p class="mt-0.5 text-xs text-ink-500">{{ cat.products?.length ?? 0 }} items</p>
                </div>
              </a>
            }
          </div>

          <!-- ======================= popular items ======================= -->
          @if (popular().length) {
            <header class="mt-14 mb-6">
              <h2 class="text-2xl font-bold tracking-tight text-ink-900">Popular right now</h2>
              <p class="mt-1 text-sm text-ink-500">A quick taste of what this branch is serving.</p>
            </header>

            <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              @for (p of popular(); track p.productId) {
                <a
                  [routerLink]="['/menu']"
                  [queryParams]="{ product: p.productId }"
                  class="group card flex items-center gap-4 overflow-hidden p-3 transition hover:shadow-pop"
                >
                  <div class="h-20 w-20 shrink-0 overflow-hidden rounded-lg bg-ink-100">
                    <app-img [src]="p.imageUrl" [alt]="p.productName ?? ''"
                             cssClass="h-full w-full object-cover transition duration-300 group-hover:scale-105" />
                  </div>
                  <div class="min-w-0 flex-1">
                    <h3 class="truncate text-sm font-semibold text-ink-900">{{ p.productName }}</h3>
                    <p class="mt-0.5 line-clamp-2 text-xs leading-relaxed text-ink-500">{{ p.description }}</p>
                    <p class="mt-1.5 text-sm font-bold text-brand-600">{{ p.price | money }}</p>
                  </div>
                </a>
              }
            </div>
          }
        }
      </section>
    }
  `,
})
export class HomePage implements OnInit {
  readonly branchCtx = inject(BranchContextService);
  private readonly menuService = inject(MenuService);
  private readonly router = inject(Router);

  readonly categories = signal<Category[]>([]);
  readonly loading = signal(false);

  readonly openNow = computed(() => this.branchCtx.isOpenNow());
  readonly totalProducts = computed(() =>
    this.categories().reduce((n, c) => n + (c.products?.length ?? 0), 0),
  );
  /** First product of each category, capped at six, as a "popular" teaser. */
  readonly popular = computed(() =>
    this.categories()
      .flatMap((c) => (c.products ?? []).slice(0, 2))
      .slice(0, 6),
  );

  ngOnInit(): void {
    this.branchCtx.load().subscribe(() => {
      const id = this.branchCtx.selectedId();
      if (id === null) {
        // More than one branch and nothing chosen yet -> let the user pick.
        if (this.branchCtx.branches().length > 1) void this.router.navigate(['/branches']);
        return;
      }
      this.loadMenu(id);
    });
  }

  private loadMenu(branchId: number): void {
    this.loading.set(true);
    this.menuService.menu(branchId).subscribe({
      next: (cats) => {
        this.categories.set(cats);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
