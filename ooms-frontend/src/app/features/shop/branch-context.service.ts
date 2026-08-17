import { Injectable, computed, inject, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { Observable, of } from 'rxjs';
import { catchError, filter, finalize, take, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { Branch } from '../../core/models/catalog.models';
import { MenuService } from '../../core/services/menu.service';
import { StorageKeys, StorageService } from '../../core/services/storage.service';

/**
 * Which branch is the customer ordering from?
 *
 * Everything downstream (menu, cart, checkout) is branch-scoped, so this is
 * resolved once, remembered in localStorage and exposed as signals.
 */
@Injectable({ providedIn: 'root' })
export class BranchContextService {
  private readonly menu = inject(MenuService);
  private readonly storage = inject(StorageService);

  private readonly _branches = signal<Branch[]>([]);
  private readonly _selectedId = signal<number | null>(
    this.storage.get<number | null>(StorageKeys.branchId, null),
  );
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private loaded = false;

  readonly branches = this._branches.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly selectedId = this._selectedId.asReadonly();

  readonly selected = computed<Branch | null>(() => {
    const id = this._selectedId();
    return id === null ? null : (this._branches().find((b) => b.branchId === id) ?? null);
  });

  readonly hasSelection = computed(() => this.selected() !== null);

  /** Loads the branch list once per session. */
  load(force = false): Observable<Branch[]> {
    if (this.loaded && !force) return of(this._branches());
    this._loading.set(true);
    this._error.set(null);
    return this.menu.branchesForHeadOffice(environment.defaultHeadOfficeId).pipe(
      tap((list) => {
        this._branches.set(list);
        this.loaded = true;
        // Drop a stale saved id, and auto-pick when there is only one branch.
        const current = this._selectedId();
        if (current !== null && !list.some((b) => b.branchId === current)) {
          this.select(null);
        }
        if (this._selectedId() === null && list.length === 1) {
          this.select(list[0].branchId);
        }
      }),
      catchError((err: Error) => {
        this._error.set(err.message);
        return of([]);
      }),
      finalize(() => this._loading.set(false)),
    );
  }

  select(branchId: number | null): void {
    this._selectedId.set(branchId);
    if (branchId === null) this.storage.remove(StorageKeys.branchId);
    else this.storage.set(StorageKeys.branchId, branchId);
  }

  /** Emits once a branch is chosen - handy for resolvers. */
  whenSelected(): Observable<Branch> {
    return toObservable(this.selected).pipe(
      filter((b): b is Branch => b !== null),
      take(1),
    );
  }

  /** "Open now" badge, based on today's BranchTiming row. */
  isOpenNow(branch: Branch | null = this.selected()): boolean | null {
    const timings = branch?.branchTimings;
    if (!timings?.length) return null;

    const dayName = new Date().toLocaleDateString('en-US', { weekday: 'long' });
    const today = timings.find(
      (t) => (t.branchTimingName ?? '').trim().toLowerCase() === dayName.toLowerCase(),
    );
    if (!today || today.isClosed) return false;
    if (!today.openTime || !today.closeTime) return null;

    const now = new Date();
    const minutesNow = now.getHours() * 60 + now.getMinutes();
    const open = toMinutes(today.openTime);
    const close = toMinutes(today.closeTime);
    if (open === null || close === null) return null;

    // Handle windows that cross midnight, e.g. 11:00 -> 00:00
    return close <= open
      ? minutesNow >= open || minutesNow < close
      : minutesNow >= open && minutesNow < close;
  }
}

function toMinutes(hhmmss: string): number | null {
  const [h, m] = hhmmss.split(':').map(Number);
  if (Number.isNaN(h) || Number.isNaN(m)) return null;
  return h * 60 + m;
}
