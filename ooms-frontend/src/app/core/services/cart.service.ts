import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError, finalize, tap } from 'rxjs/operators';
import { ApiService } from './api.service';
import { AuthService } from './auth.service';
import { StorageKeys, StorageService } from './storage.service';
import { ToastService } from './toast.service';
import { AddCartItemRequest, Cart, CartItem } from '../models/cart.models';

/**
 * Server-authoritative cart store.
 *
 * IMPORTANT backend behaviour this service is written around
 * (Restaurant.Infrastructure/Repositories/CartRepository.cs):
 *
 *  1. `POST /api/Cart/AddItem` is an UPSERT keyed on (cart, productId), and
 *     `Quantity` is ABSOLUTE, not a delta. Sending qty 3 for a product already
 *     in the cart sets it to 3. So "increment" = read current qty, post qty+1.
 *  2. Because the key is productId only, one product can appear once per cart.
 *     Re-adding the same product with different modifiers REPLACES the previous
 *     modifier/add-on selection.
 *  3. `GET /api/Cart` returns 404 when the visitor has no cart yet - that is a
 *     normal empty state, not an error.
 *  4. Subtotal, taxes and grand total are computed server-side, so after every
 *     mutation we re-read the cart instead of doing local arithmetic.
 *
 * Guests are identified by a GUID kept in localStorage; signed-in users by
 * their userId. Exactly one of the two is sent on every call.
 */
@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly storage = inject(StorageService);
  private readonly toast = inject(ToastService);

  private readonly _cart = signal<Cart | null>(null);
  private readonly _loading = signal(false);
  private readonly _mutating = signal(false);

  readonly cart = this._cart.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly mutating = this._mutating.asReadonly();

  readonly items = computed<CartItem[]>(() => this._cart()?.items ?? []);
  readonly isEmpty = computed(() => this.items().length === 0);
  readonly count = computed(() => this.items().reduce((n, i) => n + (i.quantity || 0), 0));
  readonly subTotal = computed(() => this._cart()?.subTotal ?? 0);
  readonly taxes = computed(() => this._cart()?.taxes ?? []);
  readonly grandTotal = computed(() => this._cart()?.grandTotal ?? 0);
  readonly branchId = computed(() => this._cart()?.branchId ?? null);

  /* ---------------------------------------------------------------------- */
  /* Identity                                                               */
  /* ---------------------------------------------------------------------- */

  /** Stable guest GUID, generated on first use. */
  guestToken(): string {
    let token = this.storage.get<string | null>(StorageKeys.guestToken, null);
    if (!token) {
      token = uuid();
      this.storage.set(StorageKeys.guestToken, token);
    }
    return token;
  }

  /** userId for signed-in users, otherwise null. */
  private userId(): number | null {
    return this.auth.session()?.userId ?? null;
  }

  /** Query/body identity: send userId when signed in, guest token otherwise. */
  identity(): { userId: number | null; guestSessionToken: string | null } {
    const uid = this.userId();
    return uid !== null
      ? { userId: uid, guestSessionToken: null }
      : { userId: null, guestSessionToken: this.guestToken() };
  }

  /* ---------------------------------------------------------------------- */
  /* Reads                                                                   */
  /* ---------------------------------------------------------------------- */

  /** Re-reads the server cart. A 404 is treated as "empty cart". */
  refresh(): Observable<Cart | null> {
    const { userId, guestSessionToken } = this.identity();
    this._loading.set(true);
    return this.api
      .get<Cart>('/api/Cart', {
        userId: userId ?? undefined,
        GuestSessionToken: guestSessionToken ?? undefined,
      })
      .pipe(
        tap((cart) => this._cart.set(cart ?? null)),
        catchError((err: Error & { status?: number }) => {
          if (err.status === 404) {
            this._cart.set(null);
            return of(null);
          }
          this.toast.error(err.message);
          return of(null);
        }),
        finalize(() => this._loading.set(false)),
      );
  }

  /* ---------------------------------------------------------------------- */
  /* Mutations - each re-reads the cart so totals stay authoritative         */
  /* ---------------------------------------------------------------------- */

  /**
   * Adds a product (or replaces its configuration).
   * @param quantity ABSOLUTE quantity to store, not a delta.
   */
  addItem(payload: Omit<AddCartItemRequest, 'userId' | 'guestSessionToken'>): Observable<Cart | null> {
    const body: AddCartItemRequest = { ...payload, ...this.identity() };
    this._mutating.set(true);
    return this.api.post<unknown>('/api/Cart/AddItem', body).pipe(
      catchError((err: Error) => {
        this.toast.error(err.message);
        throw err;
      }),
      finalize(() => this._mutating.set(false)),
      switchToRefresh(() => this.refresh()),
    );
  }

  /** Sets an existing line to an exact quantity (0 or less removes it). */
  setQuantity(item: CartItem, quantity: number): Observable<Cart | null> {
    if (quantity <= 0) return this.removeItem(item.cartItemId);
    return this.addItem({
      branchId: this._cart()?.branchId ?? 0,
      productId: item.productId,
      quantity,
      instructions: item.instructions ?? null,
      modifiers: item.modifiers.map((m) => ({ modifierId: m.modifierId, quantity: 1 })),
      addons: item.addons.map((a) => ({ addOnId: a.addOnId, quantity: a.addOnQuantity || 1 })),
    });
  }

  increment(item: CartItem) {
    return this.setQuantity(item, (item.quantity || 0) + 1);
  }

  decrement(item: CartItem) {
    return this.setQuantity(item, (item.quantity || 0) - 1);
  }

  removeItem(cartItemId: number): Observable<Cart | null> {
    this._mutating.set(true);
    return this.api.delete<unknown>(`/api/Cart/RemoveItem/${cartItemId}`).pipe(
      catchError((err: Error) => {
        this.toast.error(err.message);
        throw err;
      }),
      finalize(() => this._mutating.set(false)),
      switchToRefresh(() => this.refresh()),
    );
  }

  clear(): Observable<Cart | null> {
    const cartId = this._cart()?.cartId;
    if (!cartId) {
      this._cart.set(null);
      return of(null);
    }
    this._mutating.set(true);
    return this.api.post<unknown>(`/api/Cart/Clear/${cartId}`).pipe(
      catchError((err: Error) => {
        this.toast.error(err.message);
        throw err;
      }),
      finalize(() => this._mutating.set(false)),
      switchToRefresh(() => this.refresh()),
    );
  }

  /** Wipes local state after a successful checkout. */
  reset(): void {
    this._cart.set(null);
  }

  /** Per-line total including modifiers and add-ons - matches the API's maths. */
  lineTotal(item: CartItem): number {
    const base = (item.unitPrice || 0) * (item.quantity || 0);
    const mods = item.modifiers.reduce((n, m) => n + (m.modifierPrice || 0), 0);
    const adds = item.addons.reduce((n, a) => n + (a.addOnPrice || 0) * (a.addOnQuantity || 0), 0);
    return base + mods + adds;
  }
}

/* -------------------------------------------------------------------------- */

import { switchMap } from 'rxjs/operators';

/** `mergeMap`-style helper: discard the mutation result, emit the fresh cart. */
function switchToRefresh(refresh: () => Observable<Cart | null>) {
  return switchMap(() => refresh());
}

/** RFC-4122 v4 GUID; uses crypto.randomUUID when available. */
function uuid(): string {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
    return crypto.randomUUID();
  }
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === 'x' ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}
