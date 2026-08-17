import { Injectable } from '@angular/core';

/**
 * localStorage wrapper that never throws.
 *
 * Private-mode Safari and some embedded webviews make localStorage throw on
 * write; every call here degrades to an in-memory map instead of taking the
 * app down.
 */
@Injectable({ providedIn: 'root' })
export class StorageService {
  private readonly memory = new Map<string, string>();
  private readonly available = detectLocalStorage();

  get<T>(key: string, fallback: T): T {
    try {
      const raw = this.available ? localStorage.getItem(key) : (this.memory.get(key) ?? null);
      return raw === null ? fallback : (JSON.parse(raw) as T);
    } catch {
      return fallback;
    }
  }

  set(key: string, value: unknown): void {
    const raw = JSON.stringify(value);
    try {
      if (this.available) localStorage.setItem(key, raw);
      else this.memory.set(key, raw);
    } catch {
      this.memory.set(key, raw);
    }
  }

  remove(key: string): void {
    try {
      if (this.available) localStorage.removeItem(key);
    } catch {
      /* ignore */
    }
    this.memory.delete(key);
  }
}

function detectLocalStorage(): boolean {
  try {
    const probe = '__ooms_probe__';
    localStorage.setItem(probe, '1');
    localStorage.removeItem(probe);
    return true;
  } catch {
    return false;
  }
}

/** Every localStorage key the app owns, in one place. */
export const StorageKeys = {
  session: 'ooms.session',
  guestToken: 'ooms.guestToken',
  branchId: 'ooms.branchId',
  recentOrders: 'ooms.recentOrders',
} as const;
