/**
 * Auth contracts - mirrors AuthService.LoginAsync's anonymous response object
 * and Restaurant.Application/Dtos/UserDto.cs.
 */

export interface LoginRequest {
  email: string;
  password: string;
}

export interface UserBranch {
  branchId: number;
  branchName?: string | null;
}

export interface UserRole {
  roleId: number;
  roleName?: string | null;
}

/** `data` payload of POST /api/Auth/login. */
export interface LoginResponse {
  userId: number;
  username: string;
  email: string;
  headOfficeId?: number | null;
  headOfficeName?: string | null;
  branches?: UserBranch[] | null;
  roles?: UserRole[] | null;
  token: string;
}

/** What we persist in localStorage and expose as a signal. */
export interface AuthSession {
  userId: number;
  username: string;
  email: string;
  headOfficeId: number | null;
  headOfficeName: string | null;
  branches: UserBranch[];
  roles: string[];
  token: string;
  /** Unix ms - taken from the JWT `exp` claim. */
  expiresAt: number;
}

export interface AppUser {
  userId: number;
  username: string;
  email: string;
  phoneNumber?: string | null;
  password?: string | null;
  isActive?: boolean | null;
  isDeleted?: boolean | null;
  headOfficeId?: number | null;
  headOfficeName?: string | null;
  branches: UserBranch[];
  roles: UserRole[];
}

export interface Role {
  roleId: number;
  roleName?: string | null;
  roleDescription?: string | null;
  isActive?: boolean | null;
}

export interface ResetPasswordRequest {
  userId: number;
  email?: string | null;
  phoneNumber?: string | null;
  newPassword: string;
}

/** Roles that unlock the admin console. */
export const ADMIN_ROLES = ['SystemAdmin', 'SuperAdmin', 'Admin', 'OrderTaker'] as const;

/** Roles allowed to manage the catalogue (products, categories, ...). */
export const CATALOG_ROLES = ['SystemAdmin', 'SuperAdmin', 'Admin'] as const;
