import { Branch, HeadOffice } from './catalog.models';

/**
 * Order contracts - mirrors Restaurant.Application/Dtos/CheckoutDto.cs.
 */

/** Restaurant.Application.Enums.OrderType */
export enum OrderTypeId {
  Delivery = 1,
  Pickup = 2,
  DineIn = 3,
  Takeaway = 4,
}

/** Restaurant.Application.Enums.OrderStatus */
export enum OrderStatusId {
  Pending = 1,
  Confirmed = 2,
  Preparing = 3,
  ReadyForPickup = 4,
  OutForDelivery = 5,
  Delivered = 6,
  Completed = 7,
  Cancelled = 8,
  Rejected = 9,
  Refunded = 10,
}

/** Restaurant.Application.Enums.PaymentStatus */
export enum PaymentStatusId {
  Pending = 1,
  Authorized = 2,
  Paid = 3,
  Failed = 4,
  Refunded = 5,
  Cancelled = 6,
}

export interface OrderItemModifier {
  orderItemModifierId?: number | null;
  orderItemId?: number | null;
  modifierId?: number | null;
  modifierName?: string | null;
  modifierPrice?: number | null;
}

export interface OrderItemAddon {
  orderItemAddonId?: number | null;
  orderItemId?: number | null;
  addonId?: number | null;
  addonName?: string | null;
  addonQuantity?: number | null;
  addonPrice?: number | null;
  addonCategoryId?: number | null;
}

export interface OrderItem {
  orderItemId?: number | null;
  orderId?: number | null;
  productId?: number | null;
  productName?: string | null;
  quantity?: number | null;
  unitPrice?: number | null;
  finalPrice?: number | null;
  instructions?: string | null;
  discountAmount?: number | null;
  imageUrl?: string | null;
  itemModifiers: OrderItemModifier[];
  itemAddons: OrderItemAddon[];
}

export interface OrderTax {
  orderTaxId?: number | null;
  orderId?: number | null;
  taxId?: number | null;
  taxName?: string | null;
  taxRate?: number | null;
  taxableAmount?: number | null;
  taxAmount?: number | null;
}

/** Body of POST /api/Order/place-order and payload of GetOrderDetails. */
export interface Order {
  orderId?: number | null;
  orderNumber?: string | null;
  customerId?: number | null;
  headOfficeId?: number | null;
  branchId: number;
  orderTypeId?: number | null;
  orderType?: string | null;
  orderDate?: string | null;
  tableId?: number | null;

  subtotal?: number | null;
  taxAmount?: number | null;
  discountAmount?: number | null;
  deliveryFee?: number | null;
  totalAmount?: number | null;

  paymentStatus?: string | null;
  orderStatus?: string | null;

  deliveryAddressId?: number | null;
  deliveryAddress?: string | null;
  deliveryInstructions?: string | null;

  guestSessionToken?: string | null;
  guestName?: string | null;
  guestPhoneNumber?: string | null;
  guestEmailAddress?: string | null;
  guestAddressLine1?: string | null;
  guestAddressLine2?: string | null;
  guestAddressLine3?: string | null;
  guestCity?: string | null;
  guestPostalCode?: string | null;
  guestCountry?: string | null;

  createdAt?: string | null;
  updatedAt?: string | null;

  orderItems: OrderItem[];
  orderTaxes: OrderTax[];
}

export interface PlaceOrderResponse {
  orderId?: number | null;
  orderNumber?: string | null;
  totalAmount?: number | null;
  paymentStatus?: string | null;
  orderStatus?: string | null;
  orderType?: string | null;
}

export interface OrderStatusSnapshot {
  orderId?: number | null;
  orderStatus?: string | null;
  lastUpdated?: string | null;
}

export interface CancelOrderRequest {
  guestSessionToken?: string | null;
  cancelReason?: string | null;
}

/* -------------------------------------------------------------------------
   Admin order management
   ------------------------------------------------------------------------- */
export interface LiveOrderModifier {
  modifierName?: string | null;
  modifierPrice?: number | null;
}

export interface LiveOrderAddon {
  addonName?: string | null;
  addonQuantity?: number | null;
  addonPrice?: number | null;
}

export interface LiveOrderItem {
  productName: string;
  quantity: number;
  imageUrl: string;
  price: number;
  modifiers: LiveOrderModifier[];
  addons: LiveOrderAddon[];
}

export interface LiveOrder {
  orderId: number;
  orderNumber: string;
  customerId?: number | null;
  customerName: string;
  customerPhone: string;
  subTotal: number;
  total: number;
  orderType: string;
  orderStatus: string;
  paymentStatus: string;
  paymentMethod: string;
  guestSessionToken?: string | null;
  guestName?: string | null;
  guestPhoneNumber?: string | null;
  guestEmailAddress?: string | null;
  guestAddressLine1?: string | null;
  guestAddressLine2?: string | null;
  guestAddressLine3?: string | null;
  guestCity?: string | null;
  guestPostalCode?: string | null;
  guestCountry?: string | null;
  createdAt: string;
  branch?: Branch | null;
  headOffice?: HeadOffice | null;
  items: LiveOrderItem[];
}

export interface AvailableStatusOption {
  statusId: number;
  statusName: string;
}

export interface UpdateOrderStatusRequest {
  orderId: number;
  newStatusId: number;
  remarks?: string | null;
}
