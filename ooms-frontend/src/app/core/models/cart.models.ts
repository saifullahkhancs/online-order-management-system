import { Tax } from './catalog.models';

/**
 * Cart contracts - mirrors Restaurant.Application/Dtos/CartDto.cs.
 * The backend owns cart pricing: the client posts intents and re-reads the
 * server cart, so totals can never drift.
 */

export interface CartItemModifier {
  modifierId: number;
  modifierName: string;
  modifierPrice: number;
}

export interface CartItemAddon {
  addOnId: number;
  addOnName: string;
  addOnPrice: number;
  addOnQuantity: number;
}

export interface CartItem {
  cartItemId: number;
  productId: number;
  productName: string;
  productDescription: string;
  productImageUrl: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  instructions?: string | null;
  modifiers: CartItemModifier[];
  addons: CartItemAddon[];
}

export interface Cart {
  cartId: number;
  userId?: number | null;
  guestSessionToken?: string | null;
  branchId: number;
  createdAt: string;
  items: CartItem[];
  subTotal: number;
  taxes: Tax[];
  grandTotal: number;
}

/* -------------------------------------------------------------------------
   Request payloads
   ------------------------------------------------------------------------- */
export interface AddCartItemModifierRequest {
  modifierId: number;
  quantity: number;
}

export interface AddCartItemAddonRequest {
  addOnId: number;
  quantity: number;
}

export interface AddCartItemRequest {
  userId?: number | null;
  guestSessionToken?: string | null;
  branchId: number;
  productId: number;
  quantity: number;
  instructions?: string | null;
  modifiers: AddCartItemModifierRequest[];
  addons: AddCartItemAddonRequest[];
}
