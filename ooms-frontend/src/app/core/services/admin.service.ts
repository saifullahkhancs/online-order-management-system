import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from './api.service';
import {
  AddOn,
  AddOnCategory,
  Branch,
  BranchProduct,
  Category,
  HeadOffice,
  Modifier,
  ModifierCategory,
  Product,
  Tax,
} from '../models/catalog.models';
import { AppUser, Role } from '../models/auth.models';

/**
 * Admin CRUD facade - one method per backend endpoint, grouped by controller.
 *
 * Notes on the API's quirks, encoded here so components stay clean:
 *  - ProductsController and CategoryController bind `[FromForm]`, so their
 *    create/update take FormData and may carry an `imageFile` for Cloudinary.
 *  - AddOn/Modifier/ProductAddOn/ProductModifier list endpoints are scoped by
 *    `headOfficeId` (a required query param).
 *  - BranchProduct list is scoped by `branchId`.
 *  - TaxController's update is `PUT /api/Tax` with the id inside the body.
 *  - RoleController returns bare payloads instead of the ApiResponse envelope;
 *    ApiService.unwrap handles both.
 */
@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly api = inject(ApiService);

  /* ===================================================================== */
  /* Head offices - /api/HeadOffices                                       */
  /* ===================================================================== */
  headOffices(): Observable<HeadOffice[]> {
    return this.api.get<HeadOffice[]>('/api/HeadOffices').pipe(map((r) => r ?? []));
  }
  headOffice(id: number) {
    return this.api.get<HeadOffice>(`/api/HeadOffices/${id}`);
  }
  createHeadOffice(body: HeadOffice) {
    return this.api.post<HeadOffice>('/api/HeadOffices', body);
  }
  updateHeadOffice(id: number, body: HeadOffice) {
    return this.api.put<HeadOffice>(`/api/HeadOffices/${id}`, body);
  }
  deleteHeadOffice(id: number) {
    return this.api.delete<unknown>(`/api/HeadOffices/${id}`);
  }

  /* ===================================================================== */
  /* Branches - /api/Branches                                              */
  /* ===================================================================== */
  branches(): Observable<Branch[]> {
    return this.api.get<Branch[]>('/api/Branches').pipe(map((r) => r ?? []));
  }
  branch(id: number) {
    return this.api.get<Branch>(`/api/Branches/${id}`);
  }
  createBranch(body: Partial<Branch>) {
    return this.api.post<Branch>('/api/Branches', body);
  }
  updateBranch(id: number, body: Partial<Branch>) {
    return this.api.put<Branch>(`/api/Branches/${id}`, body);
  }
  deleteBranch(id: number) {
    return this.api.delete<unknown>(`/api/Branches/${id}`);
  }

  /* ===================================================================== */
  /* Categories - /api/Category  (multipart: supports imageFile)           */
  /* ===================================================================== */
  categories(): Observable<Category[]> {
    return this.api.get<Category[]>('/api/Category').pipe(map((r) => r ?? []));
  }
  category(id: number) {
    return this.api.get<Category>(`/api/Category/${id}`);
  }
  createCategory(body: Partial<Category>, imageFile?: File | null) {
    return this.api.postForm<Category>('/api/Category', toFormData(body, imageFile));
  }
  updateCategory(id: number, body: Partial<Category>, imageFile?: File | null) {
    return this.api.putForm<Category>(`/api/Category/${id}`, toFormData(body, imageFile));
  }
  deleteCategory(id: number) {
    return this.api.delete<unknown>(`/api/Category/${id}`);
  }

  /* ===================================================================== */
  /* Products - /api/Products  (multipart: supports imageFile)             */
  /* ===================================================================== */
  products(): Observable<Product[]> {
    return this.api.get<Product[]>('/api/Products').pipe(map((r) => r ?? []));
  }
  product(id: number) {
    return this.api.get<Product>(`/api/Products/${id}`);
  }
  createProduct(body: Partial<Product>, imageFile?: File | null) {
    return this.api.postForm<Product>('/api/Products', toFormData(body, imageFile));
  }
  updateProduct(id: number, body: Partial<Product>, imageFile?: File | null) {
    // The controller checks `id != entity.ProductId`, so the id must be in the body too.
    return this.api.putForm<Product>(
      `/api/Products/${id}`,
      toFormData({ ...body, productId: id }, imageFile),
    );
  }
  deleteProduct(id: number) {
    return this.api.delete<unknown>(`/api/Products/${id}`);
  }

  /* ===================================================================== */
  /* Branch product list (menu publishing) - /api/BranchProduct            */
  /* ===================================================================== */
  branchProducts(branchId: number): Observable<BranchProduct[]> {
    return this.api
      .get<BranchProduct[]>('/api/BranchProduct', { branchId })
      .pipe(map((r) => r ?? []));
  }
  createBranchProduct(body: Partial<BranchProduct>) {
    return this.api.post<BranchProduct>('/api/BranchProduct', body);
  }
  createBranchProducts(body: Partial<BranchProduct>[]) {
    return this.api.post<BranchProduct[]>('/api/BranchProduct/AddMultiple', body);
  }
  updateBranchProduct(id: number, body: Partial<BranchProduct>) {
    return this.api.put<BranchProduct>(`/api/BranchProduct/${id}`, body);
  }
  deleteBranchProduct(id: number) {
    return this.api.delete<unknown>(`/api/BranchProduct/${id}`);
  }

  /* ===================================================================== */
  /* Modifiers - /api/ModifierCategory and /api/Modifier                   */
  /* ===================================================================== */
  modifierCategories(headOfficeId: number): Observable<ModifierCategory[]> {
    return this.api
      .get<ModifierCategory[]>('/api/ModifierCategory', { headOfficeId })
      .pipe(map((r) => r ?? []));
  }
  createModifierCategory(body: Partial<ModifierCategory>) {
    return this.api.post<ModifierCategory>('/api/ModifierCategory', body);
  }
  updateModifierCategory(id: number, body: Partial<ModifierCategory>) {
    return this.api.put<ModifierCategory>(`/api/ModifierCategory/${id}`, body);
  }
  deleteModifierCategory(id: number) {
    return this.api.delete<unknown>(`/api/ModifierCategory/${id}`);
  }

  modifiers(headOfficeId: number): Observable<Modifier[]> {
    return this.api.get<Modifier[]>('/api/Modifier', { headOfficeId }).pipe(map((r) => r ?? []));
  }
  createModifier(body: Partial<Modifier>) {
    return this.api.post<Modifier>('/api/Modifier', body);
  }
  updateModifier(id: number, body: Partial<Modifier>) {
    return this.api.put<Modifier>(`/api/Modifier/${id}`, body);
  }
  deleteModifier(id: number) {
    return this.api.delete<unknown>(`/api/Modifier/${id}`);
  }

  /* ===================================================================== */
  /* Add-ons - /api/AddOnCategory and /api/AddOn                           */
  /* ===================================================================== */
  addOnCategories(headOfficeId: number): Observable<AddOnCategory[]> {
    return this.api
      .get<AddOnCategory[]>('/api/AddOnCategory', { headOfficeId })
      .pipe(map((r) => r ?? []));
  }
  createAddOnCategory(body: Partial<AddOnCategory>) {
    return this.api.post<AddOnCategory>('/api/AddOnCategory', body);
  }
  updateAddOnCategory(id: number, body: Partial<AddOnCategory>) {
    return this.api.put<AddOnCategory>(`/api/AddOnCategory/${id}`, body);
  }
  deleteAddOnCategory(id: number) {
    return this.api.delete<unknown>(`/api/AddOnCategory/${id}`);
  }

  addOns(headOfficeId: number): Observable<AddOn[]> {
    return this.api.get<AddOn[]>('/api/AddOn', { headOfficeId }).pipe(map((r) => r ?? []));
  }
  createAddOn(body: Partial<AddOn>) {
    return this.api.post<AddOn>('/api/AddOn', body);
  }
  updateAddOn(id: number, body: Partial<AddOn>) {
    return this.api.put<AddOn>(`/api/AddOn/${id}`, body);
  }
  deleteAddOn(id: number) {
    return this.api.delete<unknown>(`/api/AddOn/${id}`);
  }

  /* ===================================================================== */
  /* Product <-> modifier / add-on links                                   */
  /* ===================================================================== */
  productModifiers(productId: number) {
    return this.api
      .get<{ id: number; productId: number; modifierId: number }[]>(
        `/api/ProductModifier/product/${productId}`,
      )
      .pipe(map((r) => r ?? []));
  }
  linkProductModifiers(body: { productId: number; modifierId: number; headOfficeId: number }[]) {
    return this.api.post<unknown>('/api/ProductModifier/AddMultiple', body);
  }
  unlinkProductModifier(id: number) {
    return this.api.delete<unknown>(`/api/ProductModifier/${id}`);
  }

  productAddOns(productId: number) {
    return this.api
      .get<{ id: number; productId: number; addOnId: number }[]>(
        `/api/ProductAddOn/product/${productId}`,
      )
      .pipe(map((r) => r ?? []));
  }
  linkProductAddOns(body: { productId: number; addOnId: number; headOfficeId: number }[]) {
    return this.api.post<unknown>('/api/ProductAddOn/AddMultiple', body);
  }
  unlinkProductAddOn(id: number) {
    return this.api.delete<unknown>(`/api/ProductAddOn/${id}`);
  }

  /* ===================================================================== */
  /* Taxes - /api/Tax                                                      */
  /* ===================================================================== */
  taxes(): Observable<Tax[]> {
    return this.api.get<Tax[]>('/api/Tax').pipe(map((r) => r ?? []));
  }
  createTax(body: Partial<Tax> & { branchIds?: number[] }) {
    return this.api.post<Tax>('/api/Tax', body);
  }
  /** Update is a body-only PUT: the id travels inside the payload. */
  updateTax(body: Partial<Tax> & { taxId: number; branchIds?: number[] }) {
    return this.api.put<Tax>('/api/Tax', body);
  }
  deleteTax(id: number) {
    return this.api.delete<unknown>(`/api/Tax/${id}`);
  }

  /* ===================================================================== */
  /* Users, roles, assignments                                             */
  /* ===================================================================== */
  users(): Observable<AppUser[]> {
    return this.api.get<AppUser[]>('/api/Users').pipe(map((r) => r ?? []));
  }
  user(id: number) {
    return this.api.get<AppUser>(`/api/Users/${id}`);
  }
  createUser(body: Partial<AppUser>) {
    return this.api.post<AppUser>('/api/Users', body);
  }
  updateUser(id: number, body: Partial<AppUser>) {
    return this.api.put<AppUser>(`/api/Users/${id}`, body);
  }
  deleteUser(id: number) {
    return this.api.delete<unknown>(`/api/Users/${id}`);
  }

  roles(): Observable<Role[]> {
    return this.api.get<Role[]>('/api/Role').pipe(map((r) => r ?? []));
  }
  createRole(body: Partial<Role>) {
    return this.api.post<Role>('/api/Role', body);
  }
  updateRole(id: number, body: Partial<Role>) {
    return this.api.put<Role>(`/api/Role/${id}`, { ...body, roleId: id });
  }
  deleteRole(id: number) {
    return this.api.delete<unknown>(`/api/Role/${id}`);
  }

  assignUserRoles(userId: number, roleIds: number[]) {
    return this.api.post<unknown>('/api/UserRoles/AssignUserRoles', { userId, roleIds });
  }
  userRoles(userId: number) {
    return this.api.get<unknown>(`/api/UserRoles/${userId}/roles`);
  }
}

/* -------------------------------------------------------------------------- */

/**
 * Serialises a plain object into FormData for the `[FromForm]` controllers.
 * Keys are PascalCased because ASP.NET model binding is case-insensitive but
 * nested/complex names are not - camelCase works fine for simple properties.
 * Nulls and undefined are skipped so the server keeps its existing values.
 */
function toFormData(body: Record<string, unknown>, imageFile?: File | null): FormData {
  const fd = new FormData();
  for (const [key, value] of Object.entries(body)) {
    if (value === null || value === undefined) continue;
    if (value instanceof File) {
      fd.append(key, value);
    } else if (typeof value === 'boolean') {
      fd.append(key, value ? 'true' : 'false');
    } else if (typeof value === 'object') {
      fd.append(key, JSON.stringify(value));
    } else {
      fd.append(key, String(value));
    }
  }
  if (imageFile) fd.append('imageFile', imageFile);
  return fd;
}
