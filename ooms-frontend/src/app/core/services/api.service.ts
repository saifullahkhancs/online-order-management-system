import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api.models';

export type QueryParams = Record<
  string,
  string | number | boolean | null | undefined | ReadonlyArray<string | number>
>;

/**
 * Thin wrapper over HttpClient that understands the OOMS `ApiResponse`
 * envelope.
 *
 * Every OOMS controller returns `StatusCode(response.StatusCode, response)`
 * where `response` is `{ statusCode, message, data }`. A couple of endpoints
 * (RoleController) return the bare payload instead, so `unwrap` tolerates
 * both shapes.
 */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  /* ---------------------------------------------------------------------- */
  /* Envelope-aware helpers - use these for normal calls                     */
  /* ---------------------------------------------------------------------- */

  get<T>(path: string, params?: QueryParams): Observable<T> {
    return this.http
      .get<ApiResponse<T> | T>(this.url(path), { params: toHttpParams(params) })
      .pipe(map(unwrap<T>), catchError(handleError));
  }

  post<T>(path: string, body?: unknown, params?: QueryParams): Observable<T> {
    return this.http
      .post<ApiResponse<T> | T>(this.url(path), body ?? {}, { params: toHttpParams(params) })
      .pipe(map(unwrap<T>), catchError(handleError));
  }

  put<T>(path: string, body?: unknown, params?: QueryParams): Observable<T> {
    return this.http
      .put<ApiResponse<T> | T>(this.url(path), body ?? {}, { params: toHttpParams(params) })
      .pipe(map(unwrap<T>), catchError(handleError));
  }

  delete<T>(path: string, params?: QueryParams): Observable<T> {
    return this.http
      .delete<ApiResponse<T> | T>(this.url(path), { params: toHttpParams(params) })
      .pipe(map(unwrap<T>), catchError(handleError));
  }

  /**
   * Multipart POST/PUT - required by ProductsController and CategoryController,
   * which bind `[FromForm]` and accept an `imageFile`.
   */
  postForm<T>(path: string, form: FormData): Observable<T> {
    return this.http
      .post<ApiResponse<T> | T>(this.url(path), form)
      .pipe(map(unwrap<T>), catchError(handleError));
  }

  putForm<T>(path: string, form: FormData): Observable<T> {
    return this.http
      .put<ApiResponse<T> | T>(this.url(path), form)
      .pipe(map(unwrap<T>), catchError(handleError));
  }

  /** Returns the full envelope when the caller needs `message`/`statusCode`. */
  raw<T>(
    method: 'GET' | 'POST' | 'PUT' | 'DELETE',
    path: string,
    body?: unknown,
    params?: QueryParams,
  ): Observable<ApiResponse<T>> {
    return this.http
      .request<ApiResponse<T>>(method, this.url(path), {
        body,
        params: toHttpParams(params),
      })
      .pipe(catchError(handleError));
  }

  private url(path: string): string {
    const clean = path.startsWith('/') ? path : `/${path}`;
    return `${this.base}${clean}`;
  }
}

/* -------------------------------------------------------------------------- */
/* Helpers                                                                    */
/* -------------------------------------------------------------------------- */

function isEnvelope(v: unknown): v is ApiResponse<unknown> {
  return (
    typeof v === 'object' &&
    v !== null &&
    'statusCode' in v &&
    'message' in v &&
    'data' in v
  );
}

/** Pulls `.data` out of an `ApiResponse`, or passes bare payloads through. */
function unwrap<T>(body: ApiResponse<T> | T): T {
  return (isEnvelope(body) ? (body.data as T) : (body as T));
}

function toHttpParams(params?: QueryParams): HttpParams | undefined {
  if (!params) return undefined;
  let hp = new HttpParams();
  for (const [key, value] of Object.entries(params)) {
    if (value === null || value === undefined || value === '') continue;
    if (Array.isArray(value)) {
      for (const v of value) hp = hp.append(key, String(v));
    } else {
      hp = hp.set(key, String(value));
    }
  }
  return hp;
}

/** Normalises every backend failure into an `Error` carrying a usable message. */
export function handleError(err: HttpErrorResponse) {
  let message = 'Something went wrong. Please try again.';

  if (err.error instanceof ProgressEvent || err.status === 0) {
    message = 'Cannot reach the API. Is the .NET backend running on port 5108?';
  } else if (typeof err.error === 'string' && err.error.trim()) {
    message = err.error;
  } else if (isEnvelope(err.error) && err.error.message) {
    message = err.error.message;
  } else if (err.error?.title) {
    // ASP.NET ProblemDetails / model-validation payload
    const details = err.error.errors
      ? Object.values(err.error.errors as Record<string, string[]>)
          .flat()
          .join(' ')
      : '';
    message = details ? `${err.error.title} ${details}` : err.error.title;
  } else if (err.message) {
    message = err.message;
  }

  const wrapped = new Error(message) as Error & { status?: number };
  wrapped.status = err.status;
  return throwError(() => wrapped);
}
