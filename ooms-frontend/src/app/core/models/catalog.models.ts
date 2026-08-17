/**
 * Catalogue-side contracts.
 * Mirrors Restaurant.Domain/Entities/*.cs and the DTOs the Menus controller
 * projects into.
 */

export interface Address {
  addressId?: number;
  addressLine1: string;
  addressLine2?: string | null;
  addressLine3?: string | null;
  city: string;
  state?: string | null;
  country: string;
  postalCode?: string | null;
  isDefault?: boolean;
}

export interface HeadOffice {
  headOfficeId?: number;
  name?: string | null;
  description?: string | null;
  phoneNumber?: string | null;
  email?: string | null;
  website?: string | null;
  businessCategory?: string | null;
  headOfficeAddress?: Address | null;
  isActive?: boolean | null;
  isDeleted?: boolean | null;
  createdAt?: string | null;
  updatedAt?: string | null;
}

export interface BranchTiming {
  branchTimingId?: number;
  branchTimingName?: string | null;
  /** "HH:mm:ss" as serialised by System.Text.Json for TimeOnly. */
  openTime?: string | null;
  closeTime?: string | null;
  isClosed?: boolean | null;
  sortOrder?: number | null;
  isActive?: boolean;
}

export interface Branch {
  branchId: number;
  headOfficeId: number;
  headOfficeName?: string | null;
  branchName?: string | null;
  phoneNumber?: string | null;
  email?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  addressId?: number | null;
  isActive: boolean;
  isDeleted: boolean;
  branchAddress?: Address | null;
  headOffice?: HeadOffice | null;
  branchTimings?: BranchTiming[] | null;
  createdAt?: string | null;
  updatedAt?: string | null;
}

export interface Category {
  categoryId: number;
  categoryName?: string | null;
  description?: string | null;
  imageUrl?: string | null;
  headOfficeId?: number | null;
  isActive?: boolean | null;
  isDeleted?: boolean | null;
  products?: Product[] | null;
}

export interface Product {
  productId: number;
  productName?: string | null;
  description?: string | null;
  price?: number | null;
  imageUrl?: string | null;
  isAvailable?: boolean | null;
  isDeleted?: boolean | null;
  headOfficeId?: number | null;
  categories?: Category[] | null;
}

/** One entry of GET /api/Menus?branchId=N */
export interface MenuGroup {
  category: Category;
}

export interface BranchProduct {
  branchProductId?: number;
  branchId: number;
  productId: number;
  price?: number | null;
  isActive?: boolean | null;
  /** Convenience fields the admin grid fills in locally. */
  productName?: string | null;
  imageUrl?: string | null;
}

/* -------------------------------------------------------------------------
   Modifiers (single/multi choice that CHANGE the item, e.g. size)
   ------------------------------------------------------------------------- */
export interface ModifierCategory {
  categoryId?: number;
  /** Admin CRUD endpoints use `id`; the menu projection uses `categoryId`. */
  id?: number;
  categoryName?: string | null;
  name?: string | null;
  isRequired?: boolean | null;
  price?: number | null;
  headOfficeId?: number | null;
  isActive?: boolean | null;
}

export interface Modifier {
  id: number;
  name?: string | null;
  categoryId?: number | null;
  defaultPrice?: number | null;
  headOfficeId?: number | null;
  isActive?: boolean | null;
  isDeleted?: boolean | null;
}

export interface ModifierGroup {
  category: ModifierCategory;
  modifiers: Modifier[];
}

/* -------------------------------------------------------------------------
   Add-ons (extras ADDED to the item, e.g. extra cheese)
   ------------------------------------------------------------------------- */
export interface AddOnCategory {
  categoryId?: number;
  id?: number;
  categoryName?: string | null;
  name?: string | null;
  minSelect?: number | null;
  maxSelect?: number | null;
  sortOrder?: number | null;
  headOfficeId?: number | null;
  isActive?: boolean | null;
}

export interface AddOn {
  id: number;
  name?: string | null;
  addOnCategoryId?: number | null;
  addOnUnitPrice?: number | null;
  headOfficeId?: number | null;
  isActive?: boolean | null;
  isDeleted?: boolean | null;
}

export interface AddOnGroup {
  category: AddOnCategory;
  addOns: AddOn[];
}

/** Payload of GET /api/Menus/GetProductDetails?productId=N */
export interface ProductDetails {
  product: Product;
  modifierGroups: ModifierGroup[];
  addOnGroups: AddOnGroup[];
}

export interface Tax {
  taxId: number;
  code?: string | null;
  name?: string | null;
  rate?: number | null;
  isPercentage?: boolean | null;
  isCompound?: boolean | null;
  priority?: number | null;
  isActive?: boolean | null;
  paymentTypeId?: number | null;
  paymentType?: string | null;
  headOfficeId?: number | null;
  amount?: number | null;
}
