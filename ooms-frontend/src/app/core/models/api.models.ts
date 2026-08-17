/**
 * Envelope returned by nearly every OOMS endpoint.
 * Mirrors Restaurant.Application/Dtos/ApiResponse.cs.
 */
export interface ApiResponse<T = unknown> {
  statusCode: number;
  message: string;
  data: T | null;
}

/** Shape used by list screens that support client-side paging. */
export interface Page<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

/** Generic option for selects. */
export interface SelectOption<T = number> {
  value: T;
  label: string;
}
