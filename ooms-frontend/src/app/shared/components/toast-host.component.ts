import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastService } from '../../core/services/toast.service';

/** Fixed-position notification stack. Mounted once by App. */
@Component({
  selector: 'app-toast-host',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="pointer-events-none fixed inset-x-0 top-4 z-[100] flex flex-col items-center gap-2 px-4"
      aria-live="polite"
      aria-atomic="true"
    >
      @for (t of toasts.toasts(); track t.id) {
        <div
          class="animate-fade-up pointer-events-auto flex w-full max-w-md items-start gap-3 rounded-card
                 px-4 py-3 shadow-pop ring-1"
          [class]="tone(t.kind)"
          role="status"
        >
          <span class="mt-0.5 shrink-0" [innerHTML]="icon(t.kind)"></span>
          <p class="flex-1 text-sm leading-snug font-medium">{{ t.text }}</p>
          <button
            type="button"
            class="shrink-0 rounded p-0.5 opacity-60 transition hover:opacity-100"
            (click)="toasts.dismiss(t.id)"
            aria-label="Dismiss notification"
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
              <path d="M18 6 6 18M6 6l12 12" stroke-linecap="round" />
            </svg>
          </button>
        </div>
      }
    </div>
  `,
})
export class ToastHostComponent {
  readonly toasts = inject(ToastService);

  tone(kind: string): string {
    switch (kind) {
      case 'success':
        return 'bg-emerald-50 text-emerald-900 ring-emerald-200';
      case 'error':
        return 'bg-rose-50 text-rose-900 ring-rose-200';
      case 'warning':
        return 'bg-amber-50 text-amber-900 ring-amber-200';
      default:
        return 'bg-white text-ink-800 ring-ink-200';
    }
  }

  icon(kind: string): string {
    const common = 'width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"';
    switch (kind) {
      case 'success':
        return `<svg ${common}><path d="m20 6-11 11-5-5"/></svg>`;
      case 'error':
        return `<svg ${common}><circle cx="12" cy="12" r="9"/><path d="M12 8v5M12 16h.01"/></svg>`;
      case 'warning':
        return `<svg ${common}><path d="M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z"/><path d="M12 9v4M12 17h.01"/></svg>`;
      default:
        return `<svg ${common}><circle cx="12" cy="12" r="9"/><path d="M12 16v-4M12 8h.01"/></svg>`;
    }
  }
}
