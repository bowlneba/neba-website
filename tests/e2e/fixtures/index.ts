/**
 * E2E Test Fixtures
 *
 * Usage in tests:
 *
 * ```typescript
 * import { test, expect, mockApi, API_ROUTES, collectionResponse } from './fixtures';
 * import { createWeatherForecasts } from './fixtures/mock-data';
 *
 * test('weather page shows forecasts', async ({ page }) => {
 *   // Set up API mock before navigation
 *   await mockApi()
 *     .success(API_ROUTES.weather, collectionResponse(createWeatherForecasts(5)))
 *     .applyTo(page);
 *
 *   await page.goto('/weather');
 *   await expect(page.locator('.weather-forecast')).toHaveCount(5);
 * });
 * ```
 */

export { test, expect, mockApi, mockApiResponse, mockErrorResponse, collectionResponse, API_ROUTES, ApiMockBuilder } from './test-fixtures';
export * from './mock-data';
