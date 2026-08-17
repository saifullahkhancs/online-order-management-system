import { Injectable, signal } from '@angular/core';

export type ToastKind = 'success' | 'error' | 'info' | 'warning';

export interface Toast {
  id: number;
  kind: ToastKind;
  text: string;
}

/**
 * Non-blocking notifications. Rendered once, by <app-toast-host> in App.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private seq = 0;
  private readonly _toasts = signal<Toast[]>([]);
  readonly toasts = this._toasts.asReadonly();

  success(text: string, ms = 3200) {
    this.push('success', text, ms);
  }
  error(text: string, ms = 5200) {
    this.push('error', text, ms);
  }
  info(text: string, ms = 3200) {
    this.push('info', text, ms);
  }
  warning(text: string, ms = 4200) {
    this.push('warning', text, ms);
  }

  dismiss(id: number) {
    this._toasts.update((list) => list.filter((t) => t.id !== id));
  }

  private push(kind: ToastKind, text: string, ms: number) {
    const id = ++this.seq;
    this._toasts.update((list) => [...list, { id, kind, text }]);
    setTimeout(() => this.dismiss(id), ms);
  }
}
