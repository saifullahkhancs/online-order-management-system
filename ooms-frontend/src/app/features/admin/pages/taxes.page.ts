import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Branch, Tax } from '../../../core/models/catalog.models';
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
 * Tax rules.
 *
 * A tax row is head-office wide but is *applied* through `BranchTaxes`, so the
 * form carries a `branchIds` array the API fans out into that join table. The
 * cart totals endpoint only sums taxes attached to the ordering branch.
 *
 * Note the update endpoint is a body-only `PUT /api/Tax` — the id travels in
 * the payload, not the URL.
 */
@Component({
  selector: 'app-admin-taxes-page',
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
          <h1 class="text-2xl font-bold tracking-tight text-ink-900">Taxes</h1>
          <p class="mt-1 text-sm text-ink-500">
            Applied to the cart subtotal in priority order at checkout.
          </p>
        </div>
        <button type="button" class="btn-primary" (click)="openForm()">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.6"
               stroke-linecap="round"><path d="M12 5v14M5 12h14" /></svg>
          New tax
        </button>
      </header>

      @if (loading()) {
        <app-loading label="Loading taxes…" />
      } @else if (error()) {
        <app-error-state title="Could not load taxes" [message]="error()!" (retry)="load()" />
      } @else if (!taxes().length) {
        <div class="card">
          <app-empty-state
            title="No taxes configured"
            message="Without a tax rule the cart total equals the subtotal."
          >
            <button type="button" class="btn-primary mt-2" (click)="openForm()">New tax</button>
          </app-empty-state>
        </div>
      } @else {
        <div class="table-wrap">
          <table class="tbl">
            <thead>
              <tr>
                <th class="w-20">Priority</th>
                <th>Name</th>
                <th class="w-24">Code</th>
                <th class="w-28 text-right">Rate</th>
                <th class="w-28">Type</th>
                <th class="w-24">Status</th>
                <th class="w-24 text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              @for (t of taxes(); track t.taxId) {
                <tr>
                  <td>
                    <span class="grid h-6 w-6 place-items-center rounded-md bg-ink-100 text-xs
                                 font-bold tabular-nums text-ink-600">
                      {{ t.priority ?? 0 }}
                    </span>
                  </td>
                  <td class="font-semibold text-ink-900">{{ t.name }}</td>
                  <td class="font-mono text-xs text-ink-500">{{ t.code || '—' }}</td>
                  <td class="text-right font-semibold tabular-nums">
                    {{ t.isPercentage === false ? '' : '' }}{{ t.rate }}{{ t.isPercentage === false ? '' : '%' }}
                  </td>
                  <td>
                    <span class="badge bg-ink-100 text-ink-600">
                      {{ t.isCompound ? 'Compound' : 'Simple' }}
                    </span>
                  </td>
                  <td>
                    <span class="badge"
                          [class]="t.isActive === false
                            ? 'bg-ink-100 text-ink-600'
                            : 'bg-emerald-100 text-emerald-700'">
                      {{ t.isActive === false ? 'Inactive' : 'Active' }}
                    </span>
                  </td>
                  <td>
                    <div class="flex justify-end gap-1">
                      <button type="button" class="rounded-md p-1.5 text-ink-400 transition hover:bg-ink-100 hover:text-ink-800"
                              (click)="openForm(t)" aria-label="Edit" title="Edit">
                        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                             stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                          <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                          <path d="M18.5 2.5a2.1 2.1 0 0 1 3 3L12 15l-4 1 1-4Z" />
                        </svg>
                      </button>
                      <button type="button" class="rounded-md p-1.5 text-ink-400 transition hover:bg-rose-50 hover:text-danger"
                              (click)="deleteTarget.set(t)" aria-label="Delete" title="Delete">
                        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                             stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                          <path d="M3 6h18M8 6V4a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1v2M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                        </svg>
                      </button>
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>

    @if (formOpen()) {
      <app-modal
        [heading]="editing() ? 'Edit tax' : 'New tax'"
        subheading="Taxes only apply to the branches you tick below."
        maxWidth="30rem"
        (closed)="closeForm()"
      >
        <form [formGroup]="form" class="space-y-4">
          <div class="grid gap-4 sm:grid-cols-2">
            <div class="sm:col-span-2">
              <label class="label" for="taxName">Name <span class="text-danger">*</span></label>
              <input id="taxName" type="text" class="input" formControlName="name" placeholder="Sales Tax" />
              @if (invalid('name')) { <p class="field-error">A tax name is required.</p> }
            </div>
            <div>
              <label class="label" for="taxCode">Code</label>
              <input id="taxCode" type="text" class="input" formControlName="code" placeholder="GST" />
            </div>
            <div>
              <label class="label" for="taxRate">Rate <span class="text-danger">*</span></label>
              <input id="taxRate" type="number" min="0" step="0.01" class="input" formControlName="rate" />
              @if (invalid('rate')) { <p class="field-error">Enter a rate of 0 or more.</p> }
            </div>
            <div>
              <label class="label" for="taxPriority">Priority</label>
              <input id="taxPriority" type="number" min="0" step="1" class="input" formControlName="priority" />
              <p class="mt-1 text-[11px] text-ink-400">Lower runs first.</p>
            </div>
          </div>

          <div class="space-y-2 rounded-xl bg-ink-50 p-4">
            <label class="flex cursor-pointer items-center gap-2.5 text-sm font-medium text-ink-700">
              <input type="checkbox" class="h-4 w-4 rounded accent-brand-500" formControlName="isPercentage" />
              Percentage of the subtotal (otherwise a flat amount)
            </label>
            <label class="flex cursor-pointer items-center gap-2.5 text-sm font-medium text-ink-700">
              <input type="checkbox" class="h-4 w-4 rounded accent-brand-500" formControlName="isCompound" />
              Compound — charged on top of earlier taxes
            </label>
            <label class="flex cursor-pointer items-center gap-2.5 text-sm font-medium text-ink-700">
              <input type="checkbox" class="h-4 w-4 rounded accent-brand-500" formControlName="isActive" />
              Active
            </label>
          </div>

          <div>
            <span class="label">Applies to branches</span>
            @if (!branches().length) {
              <p class="text-xs text-ink-500">No branches yet.</p>
            } @else {
              <div class="max-h-40 space-y-1 overflow-y-auto rounded-xl ring-1 ring-ink-200 p-2">
                @for (b of branches(); track b.branchId) {
                  <label class="flex cursor-pointer items-center gap-2.5 rounded-lg px-2 py-1.5 text-sm
                                text-ink-700 transition hover:bg-ink-50">
                    <input type="checkbox" class="h-4 w-4 rounded accent-brand-500"
                           [checked]="selectedBranches().has(b.branchId)"
                           (change)="toggleBranch(b.branchId)" />
                    {{ b.branchName }}
                  </label>
                }
              </div>
            }
          </div>
        </form>

        <div modalFooter class="flex justify-end gap-2">
          <button type="button" class="btn-secondary" (click)="closeForm()">Cancel</button>
          <button type="button" class="btn-primary" [disabled]="saving()" (click)="save()">
            @if (saving()) { <app-spinner [size]="14" /> }
            {{ editing() ? 'Save changes' : 'Create tax' }}
          </button>
        </div>
      </app-modal>
    }

    @if (deleteTarget(); as target) {
      <app-confirm
        heading="Delete this tax?"
        [message]="'“' + target.name + '” will no longer be applied to new orders.'"
        [busy]="deleting()"
        (confirmed)="confirmDelete()"
        (cancelled)="deleteTarget.set(null)"
      />
    }
  `,
})
export class AdminTaxesPage implements OnInit {
  private readonly admin = inject(AdminService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  readonly taxes = signal<Tax[]>([]);
  readonly branches = signal<Branch[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly formOpen = signal(false);
  readonly editing = signal<Tax | null>(null);
  readonly saving = signal(false);
  readonly selectedBranches = signal<Set<number>>(new Set());

  readonly deleteTarget = signal<Tax | null>(null);
  readonly deleting = signal(false);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    code: [''],
    rate: [0, [Validators.required, Validators.min(0)]],
    priority: [1],
    isPercentage: [true],
    isCompound: [false],
    isActive: [true],
  });

  ngOnInit(): void {
    this.load();
    this.admin.branches().subscribe({ next: (list) => this.branches.set(list) });
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.admin.taxes().subscribe({
      next: (list) => {
        this.taxes.set([...list].sort((a, b) => (a.priority ?? 0) - (b.priority ?? 0)));
        this.loading.set(false);
      },
      error: (err: Error) => {
        this.error.set(err.message);
        this.loading.set(false);
      },
    });
  }

  invalid(name: 'name' | 'rate'): boolean {
    const c = this.form.controls[name];
    return c.invalid && (c.touched || c.dirty);
  }

  toggleBranch(id: number): void {
    const next = new Set(this.selectedBranches());
    if (next.has(id)) next.delete(id);
    else next.add(id);
    this.selectedBranches.set(next);
  }

  openForm(t?: Tax): void {
    this.editing.set(t ?? null);
    // The list endpoint does not project BranchTaxes, so an edit starts with
    // every branch ticked - saving re-applies the tax everywhere unless the
    // operator narrows it.
    this.selectedBranches.set(
      new Set(t ? this.branches().map((b) => b.branchId) : this.branches().map((b) => b.branchId)),
    );
    this.form.reset({
      name: t?.name ?? '',
      code: t?.code ?? '',
      rate: t?.rate ?? 0,
      priority: t?.priority ?? 1,
      isPercentage: t?.isPercentage ?? true,
      isCompound: t?.isCompound ?? false,
      isActive: t?.isActive ?? true,
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
    const branchIds = [...this.selectedBranches()];

    const base = {
      name: v.name,
      code: v.code || null,
      rate: v.rate,
      priority: v.priority,
      isPercentage: v.isPercentage,
      isCompound: v.isCompound,
      isActive: v.isActive,
      headOfficeId: existing?.headOfficeId ?? this.auth.headOfficeId() ?? undefined,
      branchIds,
    };

    this.saving.set(true);
    const req = existing
      ? this.admin.updateTax({ ...base, taxId: existing.taxId })
      : this.admin.createTax(base);

    req.subscribe({
      next: () => {
        this.saving.set(false);
        this.toast.success(existing ? 'Tax updated.' : 'Tax created.');
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
    this.admin.deleteTax(target.taxId).subscribe({
      next: () => {
        this.deleting.set(false);
        this.deleteTarget.set(null);
        this.toast.success('Tax deleted.');
        this.load();
      },
      error: (err: Error) => {
        this.deleting.set(false);
        this.toast.error(err.message);
      },
    });
  }
}
