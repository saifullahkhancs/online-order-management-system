import { ChangeDetectionStrategy, Component, Input, booleanAttribute, input, output } from '@angular/core';
import { FALLBACK_IMAGE } from '../../core/utils/format';

/* -------------------------------------------------------------------------- */
/* Spinner                                                                    */
/* -------------------------------------------------------------------------- */
@Component({
  selector: 'app-spinner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      class="inline-block animate-spin rounded-full border-current border-t-transparent align-[-0.125em]"
      [style.width.px]="size()"
      [style.height.px]="size()"
      [style.borderWidth.px]="size() > 24 ? 3 : 2"
      role="status"
      aria-label="Loading"
    ></span>
  `,
})
export class SpinnerComponent {
  readonly size = input(18);
}

/* -------------------------------------------------------------------------- */
/* Full-panel loading state                                                   */
/* -------------------------------------------------------------------------- */
@Component({
  selector: 'app-loading',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [SpinnerComponent],
  template: `
    <div class="flex flex-col items-center justify-center gap-3 py-16 text-ink-500">
      <app-spinner [size]="28" />
      <p class="text-sm font-medium">{{ label() }}</p>
    </div>
  `,
})
export class LoadingComponent {
  readonly label = input('Loading…');
}

/* -------------------------------------------------------------------------- */
/* Empty state                                                                */
/* -------------------------------------------------------------------------- */
@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col items-center justify-center gap-3 px-6 py-16 text-center">
      <div class="grid h-14 w-14 place-items-center rounded-full bg-ink-100 text-ink-400">
        <svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"
             stroke-linecap="round" stroke-linejoin="round">
          <path d="M3 7h18M6 7v12a2 2 0 0 0 2 2h8a2 2 0 0 0 2-2V7M9 7V5a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2v2" />
        </svg>
      </div>
      <h3 class="text-base font-semibold text-ink-800">{{ title() }}</h3>
      @if (message()) {
        <p class="max-w-sm text-sm text-ink-500">{{ message() }}</p>
      }
      <ng-content />
    </div>
  `,
})
export class EmptyStateComponent {
  readonly title = input('Nothing here yet');
  readonly message = input<string | null>(null);
}

/* -------------------------------------------------------------------------- */
/* Error state with retry                                                     */
/* -------------------------------------------------------------------------- */
@Component({
  selector: 'app-error-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col items-center justify-center gap-3 px-6 py-16 text-center">
      <div class="grid h-14 w-14 place-items-center rounded-full bg-rose-100 text-rose-600">
        <svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9"
             stroke-linecap="round" stroke-linejoin="round">
          <circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" />
        </svg>
      </div>
      <h3 class="text-base font-semibold text-ink-800">{{ title() }}</h3>
      <p class="max-w-md text-sm text-ink-500">{{ message() }}</p>
      <button type="button" class="btn-secondary mt-1" (click)="retry.emit()">Try again</button>
    </div>
  `,
})
export class ErrorStateComponent {
  readonly title = input('Could not load this');
  readonly message = input('Something went wrong.');
  readonly retry = output<void>();
}

/* -------------------------------------------------------------------------- */
/* Image with graceful fallback                                               */
/* -------------------------------------------------------------------------- */
@Component({
  selector: 'app-img',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <img
      [src]="failed ? fallback : (src() || fallback)"
      [alt]="alt()"
      [class]="cssClass()"
      loading="lazy"
      decoding="async"
      (error)="failed = true"
    />
  `,
})
export class ImgComponent {
  readonly src = input<string | null | undefined>(null);
  readonly alt = input('');
  readonly cssClass = input('h-full w-full object-cover');
  protected failed = false;
  protected readonly fallback = FALLBACK_IMAGE;
}

/* -------------------------------------------------------------------------- */
/* Modal / drawer shell                                                       */
/* -------------------------------------------------------------------------- */
@Component({
  selector: 'app-modal',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="fixed inset-0 z-50 flex items-end justify-center bg-ink-950/50 p-0 backdrop-blur-[2px] sm:items-center sm:p-6"
      (click)="onBackdrop($event)"
      role="dialog"
      aria-modal="true"
    >
      <div
        class="animate-fade-up flex max-h-[92vh] w-full flex-col overflow-hidden rounded-t-2xl bg-white
               shadow-pop sm:rounded-card"
        [style.maxWidth]="maxWidth()"
        (click)="$event.stopPropagation()"
      >
        <header class="flex shrink-0 items-start justify-between gap-4 border-b border-ink-100 px-5 py-4">
          <div>
            <h2 class="text-base font-semibold text-ink-900">{{ heading() }}</h2>
            @if (subheading()) {
              <p class="mt-0.5 text-sm text-ink-500">{{ subheading() }}</p>
            }
          </div>
          <button
            type="button"
            class="-mr-1 rounded-lg p-1.5 text-ink-400 transition hover:bg-ink-100 hover:text-ink-700"
            (click)="closed.emit()"
            aria-label="Close"
          >
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M18 6 6 18M6 6l12 12" stroke-linecap="round" />
            </svg>
          </button>
        </header>

        <div class="min-h-0 flex-1 overflow-y-auto px-5 py-4">
          <ng-content />
        </div>

        <footer class="shrink-0 border-t border-ink-100 bg-ink-50/60 px-5 py-3">
          <ng-content select="[modalFooter]" />
        </footer>
      </div>
    </div>
  `,
})
export class ModalComponent {
  readonly heading = input('');
  readonly subheading = input<string | null>(null);
  readonly maxWidth = input('34rem');
  readonly closed = output<void>();

  @Input({ transform: booleanAttribute }) dismissable = true;

  onBackdrop(_event: MouseEvent) {
    if (this.dismissable) this.closed.emit();
  }
}

/* -------------------------------------------------------------------------- */
/* Confirm dialog                                                             */
/* -------------------------------------------------------------------------- */
@Component({
  selector: 'app-confirm',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ModalComponent, SpinnerComponent],
  template: `
    <app-modal [heading]="heading()" maxWidth="26rem" (closed)="cancelled.emit()">
      <p class="text-sm leading-relaxed text-ink-600">{{ message() }}</p>
      <div modalFooter class="flex justify-end gap-2">
        <button type="button" class="btn-secondary" (click)="cancelled.emit()">Cancel</button>
        <button type="button" class="btn-danger" [disabled]="busy()" (click)="confirmed.emit()">
          @if (busy()) { <app-spinner [size]="14" /> }
          {{ confirmLabel() }}
        </button>
      </div>
    </app-modal>
  `,
})
export class ConfirmComponent {
  readonly heading = input('Are you sure?');
  readonly message = input('This action cannot be undone.');
  readonly confirmLabel = input('Delete');
  readonly busy = input(false);
  readonly confirmed = output<void>();
  readonly cancelled = output<void>();
}
