import { Pipe, PipeTransform } from '@angular/core';
import { environment } from '../../../environments/environment';
import { OrderStatusId } from '../models/order.models';

/* -------------------------------------------------------------------------- */
/* Money                                                                      */
/* -------------------------------------------------------------------------- */

const money = new Intl.NumberFormat(environment.currencyLocale, {
  style: 'currency',
  currency: environment.currency,
  minimumFractionDigits: 0,
  maximumFractionDigits: 2,
});

export function formatMoney(value: number | null | undefined): string {
  return money.format(Number(value ?? 0));
}

/** `{{ total | money }}` */
@Pipe({ name: 'money' })
export class MoneyPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    return formatMoney(value);
  }
}

/* -------------------------------------------------------------------------- */
/* Dates                                                                      */
/* -------------------------------------------------------------------------- */

/**
 * The API serialises `DateTime.UtcNow` without a `Z` suffix, so the browser
 * would read it as local time. This appends the marker when it is missing.
 */
export function parseApiDate(value: string | null | undefined): Date | null {
  if (!value) return null;
  const normalised = /[zZ]|[+-]\d{2}:?\d{2}$/.test(value) ? value : `${value}Z`;
  const d = new Date(normalised);
  return Number.isNaN(d.getTime()) ? null : d;
}

const dateTime = new Intl.DateTimeFormat(environment.currencyLocale, {
  dateStyle: 'medium',
  timeStyle: 'short',
});

export function formatDateTime(value: string | null | undefined): string {
  const d = parseApiDate(value);
  return d ? dateTime.format(d) : '—';
}

/** `{{ createdAt | when }}` -> "17 Aug 2026, 6:14 pm" */
@Pipe({ name: 'when' })
export class WhenPipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    return formatDateTime(value);
  }
}

/** "3 minutes ago" - used on the live-orders board. */
export function timeAgo(value: string | null | undefined): string {
  const d = parseApiDate(value);
  if (!d) return '—';
  const seconds = Math.max(0, Math.round((Date.now() - d.getTime()) / 1000));
  if (seconds < 45) return 'just now';
  const minutes = Math.round(seconds / 60);
  if (minutes < 60) return `${minutes} min ago`;
  const hours = Math.round(minutes / 60);
  if (hours < 24) return `${hours} hr${hours === 1 ? '' : 's'} ago`;
  const days = Math.round(hours / 24);
  return `${days} day${days === 1 ? '' : 's'} ago`;
}

/** `{{ createdAt | ago }}` */
@Pipe({ name: 'ago', pure: false })
export class AgoPipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    return timeAgo(value);
  }
}

/** "11:00:00" -> "11:00 am" */
export function formatTimeOnly(value: string | null | undefined): string {
  if (!value) return '—';
  const [h, m] = value.split(':');
  const hour = Number(h);
  if (Number.isNaN(hour)) return value;
  const suffix = hour >= 12 ? 'pm' : 'am';
  const twelve = hour % 12 === 0 ? 12 : hour % 12;
  return `${twelve}:${m ?? '00'} ${suffix}`;
}

/* -------------------------------------------------------------------------- */
/* Order status presentation                                                  */
/* -------------------------------------------------------------------------- */

export type StatusTone = 'pending' | 'active' | 'success' | 'danger' | 'neutral';

export interface StatusStyle {
  tone: StatusTone;
  /** Tailwind classes for a badge. */
  classes: string;
  label: string;
}

const STATUS_TONE: Record<string, StatusTone> = {
  Pending: 'pending',
  Confirmed: 'active',
  Preparing: 'active',
  ReadyForPickup: 'active',
  OutForDelivery: 'active',
  Delivered: 'success',
  Completed: 'success',
  Cancelled: 'danger',
  Rejected: 'danger',
  Refunded: 'neutral',
};

const TONE_CLASSES: Record<StatusTone, string> = {
  pending: 'bg-amber-100 text-amber-800 ring-1 ring-amber-200',
  active: 'bg-blue-100 text-blue-800 ring-1 ring-blue-200',
  success: 'bg-emerald-100 text-emerald-800 ring-1 ring-emerald-200',
  danger: 'bg-rose-100 text-rose-800 ring-1 ring-rose-200',
  neutral: 'bg-slate-100 text-slate-700 ring-1 ring-slate-200',
};

export function statusStyle(status: string | null | undefined): StatusStyle {
  const key = (status ?? '').replace(/\s+/g, '');
  const tone = STATUS_TONE[key] ?? 'neutral';
  return { tone, classes: TONE_CLASSES[tone], label: humanise(status) };
}

/** "OutForDelivery" -> "Out For Delivery" */
export function humanise(value: string | null | undefined): string {
  if (!value) return '—';
  return value.replace(/([a-z0-9])([A-Z])/g, '$1 $2').trim();
}

/** `{{ order.orderStatus | statusClass }}` */
@Pipe({ name: 'statusClass' })
export class StatusClassPipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    return statusStyle(value).classes;
  }
}

/** `{{ order.orderStatus | humanise }}` */
@Pipe({ name: 'humanise' })
export class HumanisePipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    return humanise(value);
  }
}

/** Progress steps shown on the customer tracking page. */
export function trackingSteps(orderTypeId: number | null | undefined): OrderStatusId[] {
  // Pickup / DineIn / Takeaway finish at ReadyForPickup -> Completed
  if (orderTypeId === 2 || orderTypeId === 4) {
    return [
      OrderStatusId.Pending,
      OrderStatusId.Confirmed,
      OrderStatusId.Preparing,
      OrderStatusId.ReadyForPickup,
      OrderStatusId.Completed,
    ];
  }
  if (orderTypeId === 3) {
    return [
      OrderStatusId.Pending,
      OrderStatusId.Confirmed,
      OrderStatusId.Preparing,
      OrderStatusId.Completed,
    ];
  }
  return [
    OrderStatusId.Pending,
    OrderStatusId.Confirmed,
    OrderStatusId.Preparing,
    OrderStatusId.OutForDelivery,
    OrderStatusId.Delivered,
  ];
}

/** Placeholder used whenever a product/category has no image. */
export const FALLBACK_IMAGE =
  'data:image/svg+xml;utf8,' +
  encodeURIComponent(
    `<svg xmlns="http://www.w3.org/2000/svg" width="400" height="300" viewBox="0 0 400 300">
       <rect width="400" height="300" fill="#f1f5f9"/>
       <g fill="none" stroke="#cbd5e1" stroke-width="8" stroke-linecap="round">
         <circle cx="200" cy="140" r="46"/>
         <path d="M150 196h100"/>
       </g>
     </svg>`,
  );
