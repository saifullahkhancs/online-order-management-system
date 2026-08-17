import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from './api.service';
import { StorageKeys, StorageService } from './storage.service';
import {
  AvailableStatusOption,
  CancelOrderRequest,
  LiveOrder,
  Order,
  OrderStatusSnapshot,
  PlaceOrderResponse,
  UpdateOrderStatusRequest,
} from '../models/order.models';

/** Minimal record kept locally so guests can find their orders again. */
export interface RecentOrderRef {
  orderId: number;
  orderNumber: string;
  placedAt: string;
  total: number;
  branchName?: string | null;
}

/**
 * Orders - both the customer flow and the admin console.
 *
 * Customer (anonymous):
 *   POST /api/Order/place-order
 *   GET  /api/Order/order-status/{orderId}
 *   GET  /api/Order/GetOrderDetails/{orderId}?customerId=&guestToken=
 *   POST /api/Order/cancel/{orderId}
 *
 * Admin (`SystemAdmin,SuperAdmin,Admin,OrderTaker`):
 *   GET  /api/Order/GetLiveOrders
 *   GET  /api/Order/GetAllOrders
 *   GET  /api/Order/available-statuses/{orderId}
 *   GET  /api/Order/GetAllOrderStatuses
 *   PUT  /api/Order/update-status
 */
@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly api = inject(ApiService);
  private readonly storage = inject(StorageService);

  /* ------------------------------ customer ------------------------------ */

  placeOrder(order: Order): Observable<PlaceOrderResponse> {
    return this.api.post<PlaceOrderResponse>('/api/Order/place-order', order);
  }

  orderStatus(orderId: number): Observable<OrderStatusSnapshot> {
    return this.api.get<OrderStatusSnapshot>(`/api/Order/order-status/${orderId}`);
  }

  /**
   * Full order. The API requires `customerId` as a query param even for
   * guests - pass 0 in that case and identify with `guestToken`.
   */
  orderDetails(
    orderId: number,
    opts: { customerId?: number | null; guestToken?: string | null } = {},
  ): Observable<Order> {
    return this.api.get<Order>(`/api/Order/GetOrderDetails/${orderId}`, {
      customerId: opts.customerId ?? 0,
      guestToken: opts.guestToken ?? undefined,
    });
  }

  cancelOrder(orderId: number, body: CancelOrderRequest): Observable<unknown> {
    return this.api.post<unknown>(`/api/Order/cancel/${orderId}`, body);
  }

  /* -------------------------------- admin ------------------------------- */

  liveOrders(): Observable<LiveOrder[]> {
    return this.api.get<LiveOrder[]>('/api/Order/GetLiveOrders').pipe(map((r) => r ?? []));
  }

  allOrders(): Observable<LiveOrder[]> {
    return this.api.get<LiveOrder[]>('/api/Order/GetAllOrders').pipe(map((r) => r ?? []));
  }

  availableStatuses(orderId: number): Observable<AvailableStatusOption[]> {
    return this.api
      .get<AvailableStatusOption[]>(`/api/Order/available-statuses/${orderId}`)
      .pipe(map((r) => r ?? []));
  }

  allStatuses(): Observable<AvailableStatusOption[]> {
    return this.api
      .get<AvailableStatusOption[]>('/api/Order/GetAllOrderStatuses')
      .pipe(map((r) => r ?? []));
  }

  updateStatus(body: UpdateOrderStatusRequest): Observable<unknown> {
    return this.api.put<unknown>('/api/Order/update-status', body);
  }

  /* --------------------- local "my orders" bookmarks -------------------- */

  recentOrders(): RecentOrderRef[] {
    return this.storage.get<RecentOrderRef[]>(StorageKeys.recentOrders, []);
  }

  rememberOrder(ref: RecentOrderRef): void {
    const list = [ref, ...this.recentOrders().filter((o) => o.orderId !== ref.orderId)].slice(0, 15);
    this.storage.set(StorageKeys.recentOrders, list);
  }
}
