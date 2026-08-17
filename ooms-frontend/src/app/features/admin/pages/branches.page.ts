import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Branch } from '../../../core/models/catalog.models';
import { AdminService } from '../../../core/services/admin.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import {
  ConfirmComponent,
  EmptyStateComponent,
  ErrorStateComponent,
  LoadingComponent,
  ModalComponent,
  SpinnerComponent,
} from '../../../shared/components/ui.components';

/**
 * Branches. The API takes a nested `branchAddress` object, so the form is a
 * nested FormGroup that maps 1:1 onto the entity.
 */
@Component({
  selector: 'app-admin-branches-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    LoadingComponent,
    ErrorStateComponent,
    EmptyStateComponent,
    ModalComponent,
    ConfirmComponent,
    SpinnerComponent,
  ],
  template: `
    <div class="p-4 sm:p-6 lg:p-8">
      <header class="mb-5 flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold tracking-tight text-ink-900">Branches</h1>
          <p class="mt-1 text-sm text-ink-500">
            Every branch has its own menu, prices and tax set.
          </p>
        </div>
        <button type="button" class="btn-primary" (click)="openForm()">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.6"
               stroke-linecap="round"><path d="M12 5v14M5 12h14" /></svg>
          New branch
        </button>
      </header>

      @if (loading()) {
        <app-loading label="Loading branches…" />
      } @else if (error()) {
        <app-error-state title="Could not load branches" [message]="error()!" (retry)="load()" />
      } @else if (!branches().length) {
        <div class="card">
          <app-empty-state title="No branches" message="Add your first location to start taking orders.">
            <button type="button" class="btn-primary mt-2" (click)="openForm()">New branch</button>
          </app-empty-state>
        </div>
      } @else {
        <ul class="grid gap-3.5 sm:grid-cols-2 xl:grid-cols-3">
          @for (b of branches(); track b.branchId) {
            <li class="card p-5">
              <div class="flex items-start justify-between gap-3">
                <div class="min-w-0">
                  <h2 class="truncate font-semibold text-ink-900">{{ b.branchName }}</h2>
                  <p class="mt-0.5 text-xs text-ink-500">{{ b.headOfficeName || 'Head office' }}</p>
                </div>
                <span class="badge shrink-0"
                      [class]="b.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-ink-100 text-ink-600'">
                  {{ b.isActive ? 'Open' : 'Closed' }}
                </span>
              </div>

              <dl class="mt-3.5 space-y-1.5 text-xs">
                @if (addressOf(b); as addr) {
                  <div class="flex gap-2 text-ink-600">
                    <svg class="mt-0.5 shrink-0 text-ink-400" width="13" height="13" viewBox="0 0 24 24" fill="none"
                         stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                      <path d="M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0Z" /><circle cx="12" cy="10" r="3" />
                    </svg>
                    <span>{{ addr }}</span>
                  </div>
                }
                @if (b.phoneNumber) {
                  <div class="flex gap-2 text-ink-600">
                    <svg class="mt-0.5 shrink-0 text-ink-400" width="13" height="13" viewBox="0 0 24 24" fill="none"
                         stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                      <path d="M22 16.9v3a2 2 0 0 1-2.2 2 19.8 19.8 0 0 1-8.6-3.1 19.5 19.5 0 0 1-6-6A19.8 19.8 0 0 1 2 4.2 2 2 0 0 1 4 2h3a2 2 0 0 1 2 1.7c.1 1 .4 1.9.7 2.8a2 2 0 0 1-.5 2.1L8.1 9.9a16 16 0 0 0 6 6l1.3-1.1a2 2 0 0 1 2.1-.5c.9.3 1.8.6 2.8.7a2 2 0 0 1 1.7 2Z" />
                    </svg>
                    <span>{{ b.phoneNumber }}</span>
                  </div>
                }
                @if (b.email) {
                  <div class="flex gap-2 text-ink-600">
                    <svg class="mt-0.5 shrink-0 text-ink-400" width="13" height="13" viewBox="0 0 24 24" fill="none"
                         stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                      <rect x="2" y="4" width="20" height="16" rx="2" /><path d="m2 7 10 6 10-6" />
                    </svg>
                    <span class="truncate">{{ b.email }}</span>
                  </div>
                }
              </dl>

              <div class="mt-4 flex gap-2">
                <button type="button" class="btn-secondary btn-sm flex-1" (click)="openForm(b)">Edit</button>
                <button type="button" class="btn-sm rounded-lg px-2.5 text-ink-400 transition
                                             hover:bg-rose-50 hover:text-danger"
                        (click)="deleteTarget.set(b)" aria-label="Delete branch">
                  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                       stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M3 6h18M8 6V4a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1v2M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                  </svg>
                </button>
              </div>
            </li>
          }
        </ul>
      }
    </div>

    @if (formOpen()) {
      <app-modal
        [heading]="editing() ? 'Edit branch' : 'New branch'"
        maxWidth="34rem"
        (closed)="closeForm()"
      >
        <form [formGroup]="form" class="space-y-4">
          <div class="grid gap-4 sm:grid-cols-2">
            <div class="sm:col-span-2">
              <label class="label" for="branchName">Branch name <span class="text-danger">*</span></label>
              <input id="branchName" type="text" class="input" formControlName="branchName"
                     placeholder="Downtown" />
              @if (invalid('branchName')) { <p class="field-error">A branch name is required.</p> }
            </div>
            <div>
              <label class="label" for="branchPhone">Phone</label>
              <input id="branchPhone" type="tel" class="input" formControlName="phoneNumber"
                     placeholder="+92 300 1234567" />
            </div>
            <div>
              <label class="label" for="branchEmail">Email</label>
              <input id="branchEmail" type="email" class="input" formControlName="email"
                     placeholder="downtown@example.com" />
            </div>
          </div>

          <fieldset formGroupName="branchAddress" class="rounded-xl bg-ink-50 p-4">
            <legend class="px-1 text-xs font-semibold tracking-wide text-ink-500 uppercase">Address</legend>
            <div class="grid gap-3 sm:grid-cols-2">
              <div class="sm:col-span-2">
                <label class="label" for="addressLine1">Line 1</label>
                <input id="addressLine1" type="text" class="input" formControlName="addressLine1"
                       placeholder="12 Mall Road" />
              </div>
              <div class="sm:col-span-2">
                <label class="label" for="addressLine2">Line 2</label>
                <input id="addressLine2" type="text" class="input" formControlName="addressLine2" />
              </div>
              <div>
                <label class="label" for="city">City</label>
                <input id="city" type="text" class="input" formControlName="city" placeholder="Mianwali" />
              </div>
              <div>
                <label class="label" for="state">State / province</label>
                <input id="state" type="text" class="input" formControlName="state" placeholder="Punjab" />
              </div>
              <div>
                <label class="label" for="postalCode">Postal code</label>
                <input id="postalCode" type="text" class="input" formControlName="postalCode" />
              </div>
              <div>
                <label class="label" for="country">Country</label>
                <input id="country" type="text" class="input" formControlName="country" placeholder="Pakistan" />
              </div>
            </div>
          </fieldset>

          <div class="grid gap-4 sm:grid-cols-2">
            <div>
              <label class="label" for="latitude">Latitude</label>
              <input id="latitude" type="number" step="any" class="input" formControlName="latitude" />
            </div>
            <div>
              <label class="label" for="longitude">Longitude</label>
              <input id="longitude" type="number" step="any" class="input" formControlName="longitude" />
            </div>
          </div>

          <label class="flex cursor-pointer items-center gap-2.5 text-sm font-medium text-ink-700">
            <input type="checkbox" class="h-4 w-4 rounded accent-brand-500" formControlName="isActive" />
            Branch is open and accepting orders
          </label>
        </form>

        <div modalFooter class="flex justify-end gap-2">
          <button type="button" class="btn-secondary" (click)="closeForm()">Cancel</button>
          <button type="button" class="btn-primary" [disabled]="saving()" (click)="save()">
            @if (saving()) { <app-spinner [size]="14" /> }
            {{ editing() ? 'Save changes' : 'Create branch' }}
          </button>
        </div>
      </app-modal>
    }

    @if (deleteTarget(); as target) {
      <app-confirm
        heading="Delete this branch?"
        [message]="'“' + target.branchName + '” and its menu links will be removed.'"
        [busy]="deleting()"
        (confirmed)="confirmDelete()"
        (cancelled)="deleteTarget.set(null)"
      />
    }
  `,
})
export class AdminBranchesPage implements OnInit {
  private readonly admin = inject(AdminService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  readonly branches = signal<Branch[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly formOpen = signal(false);
  readonly editing = signal<Branch | null>(null);
  readonly saving = signal(false);
  readonly deleteTarget = signal<Branch | null>(null);
  readonly deleting = signal(false);

  readonly form = this.fb.nonNullable.group({
    branchName: ['', [Validators.required, Validators.maxLength(150)]],
    phoneNumber: [''],
    email: ['', Validators.email],
    latitude: [null as number | null],
    longitude: [null as number | null],
    isActive: [true],
    branchAddress: this.fb.nonNullable.group({
      addressLine1: [''],
      addressLine2: [''],
      city: [''],
      state: [''],
      postalCode: [''],
      country: ['Pakistan'],
    }),
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.admin.branches().subscribe({
      next: (list) => {
        this.branches.set(list);
        this.loading.set(false);
      },
      error: (err: Error) => {
        this.error.set(err.message);
        this.loading.set(false);
      },
    });
  }

  addressOf(b: Branch): string {
    const a = b.branchAddress;
    if (!a) return '';
    return [a.addressLine1, a.addressLine2, a.city, a.state, a.postalCode].filter(Boolean).join(', ');
  }

  invalid(name: 'branchName' | 'email'): boolean {
    const c = this.form.controls[name];
    return c.invalid && (c.touched || c.dirty);
  }

  openForm(b?: Branch): void {
    this.editing.set(b ?? null);
    this.form.reset({
      branchName: b?.branchName ?? '',
      phoneNumber: b?.phoneNumber ?? '',
      email: b?.email ?? '',
      latitude: b?.latitude ?? null,
      longitude: b?.longitude ?? null,
      isActive: b?.isActive ?? true,
      branchAddress: {
        addressLine1: b?.branchAddress?.addressLine1 ?? '',
        addressLine2: b?.branchAddress?.addressLine2 ?? '',
        city: b?.branchAddress?.city ?? '',
        state: b?.branchAddress?.state ?? '',
        postalCode: b?.branchAddress?.postalCode ?? '',
        country: b?.branchAddress?.country ?? 'Pakistan',
      },
    });
    this.formOpen.set(true);
  }

  closeForm(): void {
    this.formOpen.set(false);
    this.editing.set(null);
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const existing = this.editing();

    const payload: Partial<Branch> = {
      branchName: v.branchName,
      phoneNumber: v.phoneNumber || null,
      email: v.email || null,
      latitude: v.latitude,
      longitude: v.longitude,
      isActive: v.isActive,
      isDeleted: false,
      headOfficeId: existing?.headOfficeId ?? this.auth.headOfficeId() ?? undefined,
      addressId: existing?.addressId ?? null,
      branchAddress: {
        ...v.branchAddress,
        addressId: existing?.branchAddress?.addressId,
      },
    };

    this.saving.set(true);
    const req = existing
      ? this.admin.updateBranch(existing.branchId, { ...payload, branchId: existing.branchId })
      : this.admin.createBranch(payload);

    req.subscribe({
      next: () => {
        this.saving.set(false);
        this.toast.success(existing ? 'Branch updated.' : 'Branch created.');
        this.closeForm();
        this.load();
      },
      error: (err: Error) => {
        this.saving.set(false);
        this.toast.error(err.message);
      },
    });
  }

  confirmDelete(): void {
    const target = this.deleteTarget();
    if (!target) return;
    this.deleting.set(true);
    this.admin.deleteBranch(target.branchId).subscribe({
      next: () => {
        this.deleting.set(false);
        this.deleteTarget.set(null);
        this.toast.success('Branch deleted.');
        this.load();
      },
      error: (err: Error) => {
        this.deleting.set(false);
        this.toast.error(err.message);
      },
    });
  }
}
