/**
 * Production environment.
 *
 * Set `apiBaseUrl` to the absolute origin of the deployed .NET API, e.g.
 *   apiBaseUrl: 'https://api.yourdomain.com'
 * Leave it empty when the Angular bundle is served by the API itself (or by a
 * reverse proxy that forwards /api and /hubs to the API) - that is the
 * recommended setup because it avoids CORS entirely.
 */
export const environment = {
  production: true,
  apiBaseUrl: '',
  orderHubUrl: '/hubs/orderHub',
  defaultHeadOfficeId: 1,
  currency: 'PKR',
  currencyLocale: 'en-PK',
};
