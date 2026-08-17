import { Injectable, inject } from '@angular/core';
import { Observable, map, shareReplay } from 'rxjs';
import { ApiService } from './api.service';
import { Branch, Category, MenuGroup, ProductDetails } from '../models/catalog.models';

/**
 * Public storefront catalogue.
 *
 * Endpoints (MenusController):
 *   GET /api/Menus?branchId=N                    -> [{ category: { ..., products: [] } }]
 *   GET /api/Menus/GetBranchByRestId/{restId}    -> BranchDto[]
 *   GET /api/Menus/GetProductDetails?productId=N -> { product, modifierGroups, addOnGroups }
 *
 * All three are anonymous, which is what makes guest ordering possible.
 */
@Injectable({ providedIn: 'root' })
export class MenuService {
  private readonly api = inject(ApiService);
  private readonly branchCache = new Map<number, Observable<Branch[]>>();

  /** Branches belonging to a head office (restaurant). Cached per head office. */
  branchesForHeadOffice(headOfficeId: number): Observable<Branch[]> {
    let cached = this.branchCache.get(headOfficeId);
    if (!cached) {
      cached = this.api
        .get<Branch[]>(`/api/Menus/GetBranchByRestId/${headOfficeId}`)
        .pipe(
          map((list) => list ?? []),
          shareReplay({ bufferSize: 1, refCount: false }),
        );
      this.branchCache.set(headOfficeId, cached);
    }
    return cached;
  }

  /**
   * Menu for one branch, flattened from the API's `[{ category }]` wrapper into
   * a plain `Category[]` with `products` populated.
   */
  menu(branchId: number): Observable<Category[]> {
    return this.api
      .get<MenuGroup[]>('/api/Menus', { branchId })
      .pipe(
        map((groups) =>
          (groups ?? [])
            .map((g) => g.category)
            .filter((c): c is Category => !!c)
            .map((c) => ({ ...c, products: c.products ?? [] })),
        ),
      );
  }

  /** Product with its modifier groups and add-on groups, for the detail sheet. */
  productDetails(productId: number): Observable<ProductDetails> {
    return this.api.get<ProductDetails>('/api/Menus/GetProductDetails', { productId });
  }

  /** Drops the branch cache - call after an admin edits branches. */
  invalidate(): void {
    this.branchCache.clear();
  }
}
