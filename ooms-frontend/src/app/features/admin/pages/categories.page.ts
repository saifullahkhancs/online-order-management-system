import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Category } from '../../../core/models/catalog.models';
import { AdminService } from '../../../core/services/admin.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import {
  ConfirmComponent,
  EmptyStateComponent,
  ErrorStateComponent,
  ImgComponent,
  LoadingComponent,
  ModalComponent,
  SpinnerComponent,
} from '../../../shared/components/ui.components';

/**
 * Menu categories. Like products these are `[FromForm]` endpoints with an
 * optional Cloudinary image, so the payload is FormData.
 */
@Component({
  selector: 'app-admin-categories-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    ImgComponent,
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
          <h1 class="text-2xl font-bold tracking-tight text-ink-900">Categories</h1>
          <p class="mt-1 text-sm text-ink-500">How the storefront menu is grouped and ordered.</p>
        </div>
        <button type="button" class="btn-primary" (click)="openForm()">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.6"
               stroke-linecap="round"><path d="M12 5v14M5 12h14" /></svg>
          New category
        </button>
      </header>

      <div class="mb-4 flex flex-wrap items-center gap-3">
        <div class="relative min-w-48 flex-1 sm:max-w-xs">
          <svg class="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-ink-400"
               width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2">
            <circle cx="11" cy="11" r="7" /><path d="m20 20-3.5-3.5" stroke-linecap="round" />
          </svg>
          <input type="search" class="input py-2 pl-9 text-xs" placeholder="Search categories…"
                 [ngModel]="query()" (ngModelChange)="query.set($event)" aria-label="Search categories" />
        </div>
        <span class="text-xs text-ink-500">{{ filtered().length }} of {{ categories().length }}</span>
      </div>

      @if (loading()) {
        <app-loading label="Loading categories…" />
      } @else if (error()) {
        <app-error-state title="Could not load categories" [message]="error()!" (retry)="load()" />
      } @else if (!filtered().length) {
        <div class="card">
          <app-empty-state
            title="No categories"
            [message]="query() ? 'Nothing matches your search.' : 'Create a category such as Burgers or Drinks.'"
          >
            @if (!query()) {
              <button type="button" class="btn-primary mt-2" (click)="openForm()">New category</button>
            }
          </app-empty-state>
        </div>
      } @else {
        <ul class="grid gap-3.5 sm:grid-cols-2 xl:grid-cols-3">
          @for (c of filtered(); track c.categoryId) {
            <li class="card group overflow-hidden">
              <div class="relative h-28 bg-ink-100">
                <app-img [src]="c.imageUrl" [alt]="c.categoryName ?? ''" />
                <span class="absolute top-2.5 right-2.5 badge"
                      [class]="c.isActive === false ? 'bg-ink-800/80 text-white' : 'bg-emerald-500/90 text-white'">
                  {{ c.isActive === false ? 'Hidden' : 'Live' }}
                </span>
              </div>
              <div class="p-4">
                <h2 class="truncate font-semibold text-ink-900">{{ c.categoryName }}</h2>
                <p class="mt-0.5 line-clamp-2 min-h-8 text-xs leading-relaxed text-ink-500">
                  {{ c.description || 'No description.' }}
                </p>
                <div class="mt-3 flex gap-2">
                  <button type="button" class="btn-secondary btn-sm flex-1" (click)="openForm(c)">Edit</button>
                  <button type="button" class="btn-sm rounded-lg px-2.5 text-ink-400 transition
                                               hover:bg-rose-50 hover:text-danger"
                          (click)="deleteTarget.set(c)" aria-label="Delete category">
                    <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                         stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                      <path d="M3 6h18M8 6V4a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1v2M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                    </svg>
                  </button>
                </div>
              </div>
            </li>
          }
        </ul>
      }
    </div>

    @if (formOpen()) {
      <app-modal
        [heading]="editing() ? 'Edit category' : 'New category'"
        maxWidth="30rem"
        (closed)="closeForm()"
      >
        <form [formGroup]="form" class="space-y-4">
          <div>
            <label class="label" for="categoryName">Name <span class="text-danger">*</span></label>
            <input id="categoryName" type="text" class="input" formControlName="categoryName"
                   placeholder="Burgers" />
            @if (invalid('categoryName')) { <p class="field-error">A category name is required.</p> }
          </div>

          <div>
            <label class="label" for="catDescription">Description</label>
            <textarea id="catDescription" rows="2" maxlength="500" class="input resize-none"
                      formControlName="description" placeholder="Flame-grilled, served with fries"></textarea>
          </div>

          <label class="flex cursor-pointer items-center gap-2.5 text-sm font-medium text-ink-700">
            <input type="checkbox" class="h-4 w-4 rounded accent-brand-500" formControlName="isActive" />
            Visible on the storefront
          </label>

          <div>
            <label class="label">Image</label>
            <div class="flex items-center gap-4">
              <div class="h-20 w-20 shrink-0 overflow-hidden rounded-lg bg-ink-100 ring-1 ring-ink-200">
                <app-img [src]="previewUrl() ?? editing()?.imageUrl" alt="Category image preview" />
              </div>
              <div class="min-w-0 flex-1">
                <input type="file" accept="image/*" class="block w-full text-xs text-ink-600
                         file:mr-3 file:rounded-lg file:border-0 file:bg-ink-100 file:px-3 file:py-2
                         file:text-xs file:font-semibold file:text-ink-700 hover:file:bg-ink-200"
                       (change)="pickImage($event)" />
                <p class="mt-1.5 text-[11px] text-ink-400">Leave empty to keep the current image.</p>
              </div>
            </div>
          </div>
        </form>

        <div modalFooter class="flex justify-end gap-2">
          <button type="button" class="btn-secondary" (click)="closeForm()">Cancel</button>
          <button type="button" class="btn-primary" [disabled]="saving()" (click)="save()">
            @if (saving()) { <app-spinner [size]="14" /> }
            {{ editing() ? 'Save changes' : 'Create category' }}
          </button>
        </div>
      </app-modal>
    }

    @if (deleteTarget(); as target) {
      <app-confirm
        heading="Delete this category?"
        [message]="'“' + target.categoryName + '” will be removed. Products in it are not deleted.'"
        [busy]="deleting()"
        (confirmed)="confirmDelete()"
        (cancelled)="deleteTarget.set(null)"
      />
    }
  `,
})
export class AdminCategoriesPage implements OnInit {
  private readonly admin = inject(AdminService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  readonly categories = signal<Category[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly query = signal('');

  readonly formOpen = signal(false);
  readonly editing = signal<Category | null>(null);
  readonly saving = signal(false);
  readonly imageFile = signal<File | null>(null);
  readonly previewUrl = signal<string | null>(null);

  readonly deleteTarget = signal<Category | null>(null);
  readonly deleting = signal(false);

  readonly form = this.fb.nonNullable.group({
    categoryName: ['', [Validators.required, Validators.maxLength(150)]],
    description: ['', Validators.maxLength(500)],
    isActive: [true],
  });

  readonly filtered = computed(() => {
    const q = this.query().trim().toLowerCase();
    if (!q) return this.categories();
    return this.categories().filter((c) =>
      [c.categoryName, c.description].filter(Boolean).some((v) => v!.toLowerCase().includes(q)),
    );
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.admin.categories().subscribe({
      next: (list) => {
        this.categories.set(list);
        this.loading.set(false);
      },
      error: (err: Error) => {
        this.error.set(err.message);
        this.loading.set(false);
      },
    });
  }

  invalid(name: keyof typeof this.form.controls): boolean {
    const c = this.form.controls[name];
    return c.invalid && (c.touched || c.dirty);
  }

  openForm(c?: Category): void {
    this.editing.set(c ?? null);
    this.clearImage();
    this.form.reset({
      categoryName: c?.categoryName ?? '',
      description: c?.description ?? '',
      isActive: c?.isActive ?? true,
    });
    this.formOpen.set(true);
  }

  closeForm(): void {
    this.formOpen.set(false);
    this.editing.set(null);
    this.clearImage();
  }

  pickImage(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0] ?? null;
    this.imageFile.set(file);
    this.previewUrl.set(file ? URL.createObjectURL(file) : null);
  }

  clearImage(): void {
    const url = this.previewUrl();
    if (url) URL.revokeObjectURL(url);
    this.imageFile.set(null);
    this.previewUrl.set(null);
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const existing = this.editing();

    const payload: Partial<Category> = {
      categoryName: v.categoryName,
      description: v.description || null,
      isActive: v.isActive,
      isDeleted: false,
      headOfficeId: existing?.headOfficeId ?? this.auth.headOfficeId() ?? undefined,
    };

    this.saving.set(true);
    const req = existing
      ? this.admin.updateCategory(existing.categoryId, payload, this.imageFile())
      : this.admin.createCategory(payload, this.imageFile());

    req.subscribe({
      next: () => {
        this.saving.set(false);
        this.toast.success(existing ? 'Category updated.' : 'Category created.');
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
    this.admin.deleteCategory(target.categoryId).subscribe({
      next: () => {
        this.deleting.set(false);
        this.deleteTarget.set(null);
        this.toast.success('Category deleted.');
        this.load();
      },
      error: (err: Error) => {
        this.deleting.set(false);
        this.toast.error(err.message);
      },
    });
  }
}
