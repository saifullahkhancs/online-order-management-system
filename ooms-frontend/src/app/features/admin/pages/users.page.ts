import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { AppUser, Role } from '../../../core/models/auth.models';
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

type Tab = 'users' | 'roles';

/**
 * Users, their role assignments, and the role catalogue itself.
 *
 * Role assignment is a separate endpoint (`POST /api/UserRoles/AssignUserRoles`)
 * from user CRUD, so saving a user with changed roles fires two requests.
 */
@Component({
  selector: 'app-admin-users-page',
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
          <h1 class="text-2xl font-bold tracking-tight text-ink-900">Users &amp; roles</h1>
          <p class="mt-1 text-sm text-ink-500">Who can sign in to the console, and what they may do.</p>
        </div>
        <button type="button" class="btn-primary"
                (click)="tab() === 'users' ? openUserForm() : openRoleForm()">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.6"
               stroke-linecap="round"><path d="M12 5v14M5 12h14" /></svg>
          {{ tab() === 'users' ? 'New user' : 'New role' }}
        </button>
      </header>

      <div class="mb-4 flex flex-wrap items-center gap-3">
        <div class="flex rounded-lg bg-white p-1 ring-1 ring-ink-200">
          @for (t of tabs; track t.id) {
            <button type="button" class="rounded-md px-3.5 py-1.5 text-xs font-semibold transition"
                    [class]="tab() === t.id ? 'bg-ink-900 text-white' : 'text-ink-600 hover:text-ink-900'"
                    (click)="tab.set(t.id)">
              {{ t.label }}
            </button>
          }
        </div>

        @if (tab() === 'users') {
          <div class="relative min-w-48 flex-1 sm:max-w-xs">
            <svg class="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-ink-400"
                 width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2">
              <circle cx="11" cy="11" r="7" /><path d="m20 20-3.5-3.5" stroke-linecap="round" />
            </svg>
            <input type="search" class="input py-2 pl-9 text-xs" placeholder="Search users…"
                   [ngModel]="query()" (ngModelChange)="query.set($event)" aria-label="Search users" />
          </div>
        }
      </div>

      @if (loading()) {
        <app-loading label="Loading…" />
      } @else if (error()) {
        <app-error-state title="Could not load" [message]="error()!" (retry)="load()" />
      } @else if (tab() === 'users') {
        @if (!filteredUsers().length) {
          <div class="card">
            <app-empty-state
              title="No users"
              [message]="query() ? 'Nothing matches your search.' : 'Invite a colleague to the console.'"
            />
          </div>
        } @else {
          <div class="table-wrap">
            <table class="tbl">
              <thead>
                <tr>
                  <th>User</th>
                  <th class="w-56">Roles</th>
                  <th class="w-40">Branches</th>
                  <th class="w-24">Status</th>
                  <th class="w-32 text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                @for (u of filteredUsers(); track u.userId) {
                  <tr>
                    <td>
                      <div class="flex items-center gap-3">
                        <span class="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-ink-800
                                     text-[11px] font-bold text-white">
                          {{ u.username.slice(0, 2).toUpperCase() }}
                        </span>
                        <div class="min-w-0">
                          <p class="font-semibold text-ink-900">{{ u.username }}</p>
                          <p class="truncate text-xs text-ink-500">{{ u.email }}</p>
                        </div>
                      </div>
                    </td>
                    <td>
                      <div class="flex flex-wrap gap-1">
                        @for (r of u.roles ?? []; track r.roleId) {
                          <span class="badge bg-brand-50 text-brand-700">{{ r.roleName }}</span>
                        } @empty {
                          <span class="text-xs text-ink-400">No roles</span>
                        }
                      </div>
                    </td>
                    <td class="text-xs text-ink-600">
                      {{ (u.branches ?? []).length ? branchNames(u) : 'All / none' }}
                    </td>
                    <td>
                      <span class="badge"
                            [class]="u.isActive === false
                              ? 'bg-ink-100 text-ink-600'
                              : 'bg-emerald-100 text-emerald-700'">
                        {{ u.isActive === false ? 'Disabled' : 'Active' }}
                      </span>
                    </td>
                    <td>
                      <div class="flex justify-end gap-1">
                        <button type="button" class="rounded-md px-2 py-1.5 text-[11px] font-semibold
                                                     text-ink-500 transition hover:bg-ink-100 hover:text-ink-800"
                                (click)="openReset(u)" title="Reset password">
                          Reset
                        </button>
                        <button type="button" class="rounded-md p-1.5 text-ink-400 transition hover:bg-ink-100 hover:text-ink-800"
                                (click)="openUserForm(u)" aria-label="Edit" title="Edit">
                          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                               stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                            <path d="M18.5 2.5a2.1 2.1 0 0 1 3 3L12 15l-4 1 1-4Z" />
                          </svg>
                        </button>
                        <button type="button" class="rounded-md p-1.5 text-ink-400 transition hover:bg-rose-50 hover:text-danger"
                                (click)="deleteUserTarget.set(u)" aria-label="Delete" title="Delete">
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
      } @else {
        @if (!roles().length) {
          <div class="card"><app-empty-state title="No roles" message="Create a role to group permissions." /></div>
        } @else {
          <ul class="grid gap-3.5 sm:grid-cols-2 xl:grid-cols-3">
            @for (r of roles(); track r.roleId) {
              <li class="card p-5">
                <div class="flex items-start justify-between gap-3">
                  <h2 class="font-semibold text-ink-900">{{ r.roleName }}</h2>
                  <span class="badge shrink-0"
                        [class]="r.isActive === false
                          ? 'bg-ink-100 text-ink-600'
                          : 'bg-emerald-100 text-emerald-700'">
                    {{ r.isActive === false ? 'Inactive' : 'Active' }}
                  </span>
                </div>
                <p class="mt-1 min-h-8 text-xs leading-relaxed text-ink-500">
                  {{ r.roleDescription || 'No description.' }}
                </p>
                <p class="mt-2 text-[11px] text-ink-400">{{ usersWithRole(r) }} user(s)</p>
                <div class="mt-3 flex gap-2">
                  <button type="button" class="btn-secondary btn-sm flex-1" (click)="openRoleForm(r)">Edit</button>
                  <button type="button" class="btn-sm rounded-lg px-2.5 text-ink-400 transition
                                               hover:bg-rose-50 hover:text-danger"
                          (click)="deleteRoleTarget.set(r)" aria-label="Delete role">
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
      }
    </div>

    <!-- ========================== user form ========================== -->
    @if (userFormOpen()) {
      <app-modal
        [heading]="editingUser() ? 'Edit user' : 'New user'"
        maxWidth="30rem"
        (closed)="closeUserForm()"
      >
        <form [formGroup]="userForm" class="space-y-4">
          <div>
            <label class="label" for="username">Username <span class="text-danger">*</span></label>
            <input id="username" type="text" class="input" formControlName="username" autocomplete="off" />
            @if (userInvalid('username')) { <p class="field-error">A username is required.</p> }
          </div>
          <div>
            <label class="label" for="userEmail">Email <span class="text-danger">*</span></label>
            <input id="userEmail" type="email" class="input" formControlName="email" autocomplete="off" />
            @if (userInvalid('email')) { <p class="field-error">Enter a valid email address.</p> }
          </div>
          <div>
            <label class="label" for="userPhone">Phone</label>
            <input id="userPhone" type="tel" class="input" formControlName="phoneNumber" />
          </div>

          @if (!editingUser()) {
            <div>
              <label class="label" for="userPassword">Password <span class="text-danger">*</span></label>
              <input id="userPassword" type="password" class="input" formControlName="password"
                     autocomplete="new-password" />
              @if (userInvalid('password')) { <p class="field-error">At least 6 characters.</p> }
            </div>
          }

          <div>
            <span class="label">Roles</span>
            @if (!roles().length) {
              <p class="text-xs text-ink-500">No roles defined yet.</p>
            } @else {
              <div class="space-y-1 rounded-xl p-2 ring-1 ring-ink-200">
                @for (r of roles(); track r.roleId) {
                  <label class="flex cursor-pointer items-center gap-2.5 rounded-lg px-2 py-1.5 text-sm
                                text-ink-700 transition hover:bg-ink-50">
                    <input type="checkbox" class="h-4 w-4 rounded accent-brand-500"
                           [checked]="selectedRoles().has(r.roleId)" (change)="toggleRole(r.roleId)" />
                    {{ r.roleName }}
                  </label>
                }
              </div>
            }
          </div>

          <label class="flex cursor-pointer items-center gap-2.5 text-sm font-medium text-ink-700">
            <input type="checkbox" class="h-4 w-4 rounded accent-brand-500" formControlName="isActive" />
            Account is active
          </label>
        </form>

        <div modalFooter class="flex justify-end gap-2">
          <button type="button" class="btn-secondary" (click)="closeUserForm()">Cancel</button>
          <button type="button" class="btn-primary" [disabled]="savingUser()" (click)="saveUser()">
            @if (savingUser()) { <app-spinner [size]="14" /> }
            {{ editingUser() ? 'Save changes' : 'Create user' }}
          </button>
        </div>
      </app-modal>
    }

    <!-- ========================== role form ========================== -->
    @if (roleFormOpen()) {
      <app-modal
        [heading]="editingRole() ? 'Edit role' : 'New role'"
        maxWidth="26rem"
        (closed)="closeRoleForm()"
      >
        <form [formGroup]="roleForm" class="space-y-4">
          <div>
            <label class="label" for="roleName">Role name <span class="text-danger">*</span></label>
            <input id="roleName" type="text" class="input" formControlName="roleName" placeholder="OrderTaker" />
            @if (roleForm.controls.roleName.invalid && roleForm.controls.roleName.touched) {
              <p class="field-error">A role name is required.</p>
            }
            <p class="mt-1 text-[11px] leading-relaxed text-ink-400">
              The API authorises on these exact strings — SystemAdmin, SuperAdmin, Admin and OrderTaker are
              the ones its <code>[Authorize(Roles = …)]</code> attributes recognise.
            </p>
          </div>
          <div>
            <label class="label" for="roleDescription">Description</label>
            <textarea id="roleDescription" rows="2" class="input resize-none"
                      formControlName="roleDescription"></textarea>
          </div>
          <label class="flex cursor-pointer items-center gap-2.5 text-sm font-medium text-ink-700">
            <input type="checkbox" class="h-4 w-4 rounded accent-brand-500" formControlName="isActive" />
            Active
          </label>
        </form>

        <div modalFooter class="flex justify-end gap-2">
          <button type="button" class="btn-secondary" (click)="closeRoleForm()">Cancel</button>
          <button type="button" class="btn-primary" [disabled]="savingRole()" (click)="saveRole()">
            @if (savingRole()) { <app-spinner [size]="14" /> }
            {{ editingRole() ? 'Save changes' : 'Create role' }}
          </button>
        </div>
      </app-modal>
    }

    <!-- ======================= password reset ======================= -->
    @if (resetTarget(); as target) {
      <app-modal
        heading="Reset password"
        [subheading]="'A new password for ' + target.username"
        maxWidth="24rem"
        (closed)="resetTarget.set(null)"
      >
        <label class="label" for="newPassword">New password</label>
        <input id="newPassword" type="text" class="input" autocomplete="new-password"
               [ngModel]="newPassword()" (ngModelChange)="newPassword.set($event)" />
        <p class="mt-1.5 text-[11px] text-ink-400">
          Share it over a secure channel — the API does not email it.
        </p>

        <div modalFooter class="flex justify-end gap-2">
          <button type="button" class="btn-secondary" (click)="resetTarget.set(null)">Cancel</button>
          <button type="button" class="btn-primary"
                  [disabled]="newPassword().length < 6 || resetting()" (click)="confirmReset()">
            @if (resetting()) { <app-spinner [size]="14" /> }
            Reset password
          </button>
        </div>
      </app-modal>
    }

    @if (deleteUserTarget(); as target) {
      <app-confirm
        heading="Delete this user?"
        [message]="target.username + ' will lose access to the console immediately.'"
        [busy]="deleting()"
        (confirmed)="confirmDeleteUser()"
        (cancelled)="deleteUserTarget.set(null)"
      />
    }

    @if (deleteRoleTarget(); as target) {
      <app-confirm
        heading="Delete this role?"
        [message]="'“' + target.roleName + '” will be removed from everyone who holds it.'"
        [busy]="deleting()"
        (confirmed)="confirmDeleteRole()"
        (cancelled)="deleteRoleTarget.set(null)"
      />
    }
  `,
})
export class AdminUsersPage implements OnInit {
  private readonly admin = inject(AdminService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  readonly tabs: { id: Tab; label: string }[] = [
    { id: 'users', label: 'Users' },
    { id: 'roles', label: 'Roles' },
  ];

  readonly tab = signal<Tab>('users');
  readonly users = signal<AppUser[]>([]);
  readonly roles = signal<Role[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly query = signal('');

  readonly userFormOpen = signal(false);
  readonly editingUser = signal<AppUser | null>(null);
  readonly savingUser = signal(false);
  readonly selectedRoles = signal<Set<number>>(new Set());

  readonly roleFormOpen = signal(false);
  readonly editingRole = signal<Role | null>(null);
  readonly savingRole = signal(false);

  readonly resetTarget = signal<AppUser | null>(null);
  readonly newPassword = signal('');
  readonly resetting = signal(false);

  readonly deleteUserTarget = signal<AppUser | null>(null);
  readonly deleteRoleTarget = signal<Role | null>(null);
  readonly deleting = signal(false);

  readonly userForm = this.fb.nonNullable.group({
    username: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: [''],
    password: ['', [Validators.required, Validators.minLength(6)]],
    isActive: [true],
  });

  readonly roleForm = this.fb.nonNullable.group({
    roleName: ['', [Validators.required, Validators.maxLength(60)]],
    roleDescription: [''],
    isActive: [true],
  });

  readonly filteredUsers = computed(() => {
    const q = this.query().trim().toLowerCase();
    if (!q) return this.users();
    return this.users().filter((u) =>
      [u.username, u.email, u.phoneNumber].filter(Boolean).some((v) => v!.toLowerCase().includes(q)),
    );
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.admin.users().subscribe({
      next: (list) => {
        this.users.set(list);
        this.loading.set(false);
      },
      error: (err: Error) => {
        this.error.set(err.message);
        this.loading.set(false);
      },
    });
    this.admin.roles().subscribe({ next: (list) => this.roles.set(list) });
  }

  branchNames(u: AppUser): string {
    return (u.branches ?? []).map((b) => b.branchName).filter(Boolean).join(', ');
  }

  usersWithRole(r: Role): number {
    return this.users().filter((u) => (u.roles ?? []).some((x) => x.roleId === r.roleId)).length;
  }

  userInvalid(name: keyof typeof this.userForm.controls): boolean {
    const c = this.userForm.controls[name];
    return c.invalid && (c.touched || c.dirty);
  }

  toggleRole(id: number): void {
    const next = new Set(this.selectedRoles());
    if (next.has(id)) next.delete(id);
    else next.add(id);
    this.selectedRoles.set(next);
  }

  /* ------------------------------ users ------------------------------ */

  openUserForm(u?: AppUser): void {
    this.editingUser.set(u ?? null);
    this.selectedRoles.set(new Set((u?.roles ?? []).map((r) => r.roleId)));
    this.userForm.reset({
      username: u?.username ?? '',
      email: u?.email ?? '',
      phoneNumber: u?.phoneNumber ?? '',
      password: '',
      isActive: u?.isActive ?? true,
    });
    // Password is only required when creating.
    const pwd = this.userForm.controls.password;
    if (u) pwd.clearValidators();
    else pwd.setValidators([Validators.required, Validators.minLength(6)]);
    pwd.updateValueAndValidity();
    this.userFormOpen.set(true);
  }

  closeUserForm(): void {
    this.userFormOpen.set(false);
    this.editingUser.set(null);
  }

  saveUser(): void {
    if (this.userForm.invalid) {
      this.userForm.markAllAsTouched();
      return;
    }
    const v = this.userForm.getRawValue();
    const existing = this.editingUser();
    const roleIds = [...this.selectedRoles()];

    const payload: Partial<AppUser> = {
      username: v.username,
      email: v.email,
      phoneNumber: v.phoneNumber || null,
      isActive: v.isActive,
      isDeleted: false,
      headOfficeId: existing?.headOfficeId ?? this.auth.headOfficeId() ?? undefined,
    };
    if (!existing) payload.password = v.password;

    this.savingUser.set(true);
    const req = existing
      ? this.admin.updateUser(existing.userId, { ...payload, userId: existing.userId })
      : this.admin.createUser(payload);

    req.subscribe({
      next: (saved) => {
        const userId = existing?.userId ?? (saved as AppUser | null)?.userId;
        // Role assignment is a second, separate call.
        if (userId && roleIds.length) {
          this.admin.assignUserRoles(userId, roleIds).subscribe({
            next: () => this.finishUserSave(!!existing),
            error: (err: Error) => {
              this.savingUser.set(false);
              this.toast.error(`User saved but roles failed: ${err.message}`);
              this.closeUserForm();
              this.load();
            },
          });
        } else {
          this.finishUserSave(!!existing);
        }
      },
      error: (err: Error) => {
        this.savingUser.set(false);
        this.toast.error(err.message);
      },
    });
  }

  private finishUserSave(wasEdit: boolean): void {
    this.savingUser.set(false);
    this.toast.success(wasEdit ? 'User updated.' : 'User created.');
    this.closeUserForm();
    this.load();
  }

  confirmDeleteUser(): void {
    const target = this.deleteUserTarget();
    if (!target) return;
    this.deleting.set(true);
    this.admin.deleteUser(target.userId).subscribe({
      next: () => {
        this.deleting.set(false);
        this.deleteUserTarget.set(null);
        this.toast.success('User deleted.');
        this.load();
      },
      error: (err: Error) => {
        this.deleting.set(false);
        this.toast.error(err.message);
      },
    });
  }

  /* ------------------------------ roles ------------------------------ */

  openRoleForm(r?: Role): void {
    this.editingRole.set(r ?? null);
    this.roleForm.reset({
      roleName: r?.roleName ?? '',
      roleDescription: r?.roleDescription ?? '',
      isActive: r?.isActive ?? true,
    });
    this.roleFormOpen.set(true);
  }

  closeRoleForm(): void {
    this.roleFormOpen.set(false);
    this.editingRole.set(null);
  }

  saveRole(): void {
    if (this.roleForm.invalid) {
      this.roleForm.markAllAsTouched();
      return;
    }
    const v = this.roleForm.getRawValue();
    const existing = this.editingRole();

    this.savingRole.set(true);
    const req = existing
      ? this.admin.updateRole(existing.roleId, { ...v, roleId: existing.roleId })
      : this.admin.createRole(v);

    req.subscribe({
      next: () => {
        this.savingRole.set(false);
        this.toast.success(existing ? 'Role updated.' : 'Role created.');
        this.closeRoleForm();
        this.load();
      },
      error: (err: Error) => {
        this.savingRole.set(false);
        this.toast.error(err.message);
      },
    });
  }

  confirmDeleteRole(): void {
    const target = this.deleteRoleTarget();
    if (!target) return;
    this.deleting.set(true);
    this.admin.deleteRole(target.roleId).subscribe({
      next: () => {
        this.deleting.set(false);
        this.deleteRoleTarget.set(null);
        this.toast.success('Role deleted.');
        this.load();
      },
      error: (err: Error) => {
        this.deleting.set(false);
        this.toast.error(err.message);
      },
    });
  }

  /* ---------------------------- password ---------------------------- */

  openReset(u: AppUser): void {
    this.resetTarget.set(u);
    this.newPassword.set('');
  }

  confirmReset(): void {
    const target = this.resetTarget();
    if (!target) return;
    this.resetting.set(true);
    this.auth
      .resetPassword({ userId: target.userId, email: target.email, newPassword: this.newPassword() })
      .subscribe({
        next: () => {
          this.resetting.set(false);
          this.resetTarget.set(null);
          this.toast.success('Password reset.');
        },
        error: (err: Error) => {
          this.resetting.set(false);
          this.toast.error(err.message);
        },
      });
  }
}
