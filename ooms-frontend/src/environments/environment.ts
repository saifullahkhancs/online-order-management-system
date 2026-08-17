/**
 * Development environment.
 *
 * `apiBaseUrl` is intentionally a RELATIVE path. `ng serve` proxies every
 * request beginning with /api and /hubs to the .NET API using the rules in
 * proxy.conf.json. Keeping it relative means the browser never has to know the
 * API host, which also makes the app work behind any reverse proxy.
 *
 * To point the dev server at a different API, edit proxy.conf.json - not this
 * file.
 */
export const environment = {
  production: false,
  /** Base URL for REST calls. Empty string => same origin => proxied. */
  apiBaseUrl: '',
  /** SignalR hub used by the admin live-orders board. */
  orderHubUrl: '/hubs/orderHub',
  /**
   * Head office the storefront belongs to. The public menu flow starts with
   * GET /api/Menus/GetBranchByRestId/{headOfficeId}.
   */
  defaultHeadOfficeId: 1,
  /** ISO 4217 code used by the currency pipe. */
  currency: 'PKR',
  currencyLocale: 'en-PK',
};
