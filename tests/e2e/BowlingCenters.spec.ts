import { test, expect } from '@playwright/test';

/**
 * Injects a lightweight atlas mock and pre-sets azureMapsAuthConfig.
 *
 * Why this is needed:
 * - NebaMap.razor.js calls waitForAtlas() which checks `typeof atlas !== 'undefined'`
 * - initializeMap() sets the module-level `map` variable via `new atlas.Map(...)`,
 *   which showRoute() requires to be non-null before calling the Azure Maps Route API
 * - atlas.layer.SymbolLayer must be a real constructor so instanceof checks in
 *   enterDirectionsPreview / exitDirectionsMode work correctly
 * - The 'ready' event fires after a short delay so NotifyMapReady → HandleBoundsChanged
 *   is called, keeping _currentMapBounds in sync
 */
function atlasMockScript() {
  class SymbolLayer {
    setOptions(_options: unknown) {}
  }
  class BubbleLayer {}
  class LineLayer {}

  function makeDataSource() {
    const shapes: unknown[] = [];
    return {
      add(data: unknown) {
        if (Array.isArray(data)) shapes.push(...data);
        else if (data != null) shapes.push(data);
      },
      clear() {
        shapes.length = 0;
      },
      getShapes() {
        return [...shapes];
      },
      getClusterExpansionZoom() {
        return Promise.resolve(12);
      },
    };
  }

  function makeMap() {
    const layers: unknown[] = [];
    return {
      events: {
        add(event: string, targetOrCb: unknown, maybeCb?: unknown) {
          if (event === 'ready') {
            const cb =
              typeof targetOrCb === 'function'
                ? targetOrCb
                : typeof maybeCb === 'function'
                  ? maybeCb
                  : null;
            // Fire 'ready' asynchronously so Blazor's NotifyMapReady gets invoked
            if (typeof cb === 'function') setTimeout(cb as () => void, 50);
          }
        },
      },
      layers: {
        add(layer: unknown) {
          layers.push(layer);
        },
        remove() {},
        getLayers() {
          return [...layers];
        },
      },
      sources: { add() {}, remove() {} },
      setCamera() {},
      setStyle() {},
      getCamera() {
        return { bounds: [-74, 40, -65, 48] };
      },
      getCanvasContainer() {
        return { style: {} };
      },
      dispose() {},
    };
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  (window as any).atlas = {
    Map: makeMap,
    source: { DataSource: makeDataSource },
    data: {
      Feature: (geometry: unknown, props: unknown) => ({
        type: 'Feature',
        geometry,
        properties: props,
        getProperties() {
          return props;
        },
        getCoordinates() {
          return (geometry as { coordinates?: unknown })?.coordinates;
        },
      }),
      Point: (coords: unknown) => ({ type: 'Point', coordinates: coords }),
      BoundingBox: { fromData: () => [-74, 40, -65, 48] },
    },
    layer: { SymbolLayer, BubbleLayer, LineLayer },
    Popup: class {
      open() {}
      close() {}
    },
  };

  // Pre-set auth config so searchAddress() in DirectionsModal.razor.js works
  // even before initializeMap() has completed on first render.
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  (window as any).azureMapsAuthConfig = { subscriptionKey: 'mock-key-for-e2e' };
}

// 20117 m ≈ 12.5 mi  →  FormattedDistance = "12.5 mi"
// 1440 s = 24 min    →  FormattedTravelTime = "24 min"
// Shape of an Azure Maps Route Directions 2025-01-01 response (GeoJSON, [lon, lat]).
const MOCK_ROUTE_RESPONSE = {
  type: 'FeatureCollection',
  features: [
    {
      type: 'Feature',
      geometry: { type: 'Point', coordinates: [-71.0589, 42.3601] },
      properties: { type: 'Waypoint', order: { inputIndex: 0 } },
    },
    ...[
      { text: 'Head north on Main St', distanceInMeters: 500 },
      { text: 'Turn right onto Route 9', distanceInMeters: 15000 },
      { text: 'Arrive at Lucky Strike Lanes', distanceInMeters: 0 },
    ].map(({ text, distanceInMeters }) => ({
      type: 'Feature',
      geometry: { type: 'Point', coordinates: [-71.0589, 42.3601] },
      properties: { type: 'ManeuverPoint', instruction: { text }, distanceInMeters },
    })),
    {
      type: 'Feature',
      geometry: {
        type: 'MultiLineString',
        coordinates: [
          [
            [-71.0589, 42.3601],
            [-71.1002, 42.4012],
          ],
        ],
      },
      properties: {
        type: 'RoutePath',
        distanceInMeters: 20117,
        durationInSeconds: 1380,
        durationTrafficInSeconds: 1440,
      },
    },
  ],
  alternativeRoutes: [],
};

// Shape of an Azure Maps Search Geocoding 2026-01-01 response (GeoJSON, [lon, lat]).
const MOCK_ADDRESS_SEARCH_RESPONSE = {
  type: 'FeatureCollection',
  features: [
    {
      type: 'Feature',
      geometry: { type: 'Point', coordinates: [-71.0589, 42.3601] },
      properties: {
        type: 'Address',
        address: {
          formattedAddress: '123 Main St, Boston, MA 02101',
          locality: 'Boston',
          adminDistricts: [{ shortName: 'MA' }],
          countryRegion: { ISO: 'US' },
        },
      },
    },
  ],
};

test.describe('Bowling Centers page', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    // Block the real Azure Maps SDK (JS + CSS) so our mock atlas isn't overwritten
    await page.route('https://atlas.microsoft.com/**', (route) => route.abort());

    // Inject mock atlas global before any page scripts execute
    await page.addInitScript(atlasMockScript);

    // Mock Azure Maps address search API (called from DirectionsModal.razor.js)
    await page.route('https://atlas.microsoft.com/geocode**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(MOCK_ADDRESS_SEARCH_RESPONSE),
      });
    });

    // Mock Azure Maps route directions API (called from NebaMap.razor.js showRoute())
    await page.route('https://atlas.microsoft.com/route/directions**', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(MOCK_ROUTE_RESPONSE),
      });
    });

    await page.goto('/bowling-centers');
    await page.waitForSelector('.neba-card');
  });

  test('displays bowling center cards from the API', async ({ page }) => {
    await expect(page.locator('h3:has-text("Lucky Strike Lanes")')).toBeVisible();
    await expect(page.locator('h3:has-text("Strike Zone")')).toBeVisible();
  });

  test('opens directions modal when Get Directions is clicked', async ({ page }) => {
    await page.locator('button:has-text("Get Directions")').first().click();

    await expect(page.locator('text=Directions to Lucky Strike Lanes')).toBeVisible();
    await expect(page.locator('#address-input')).toBeVisible();
    await expect(page.locator('button:has-text("Use My Current Location")')).toBeVisible();
  });

  test('shows address suggestions when user types a starting address', async ({ page }) => {
    await page.locator('button:has-text("Get Directions")').first().click();
    await page.waitForSelector('#address-input');

    await page.fill('#address-input', '123 Main St, Boston');

    await expect(page.locator('button:has-text("123 Main St, Boston, MA 02101")')).toBeVisible();
  });

  test('shows route summary after selecting an address suggestion', async ({ page }) => {
    await page.locator('button:has-text("Get Directions")').first().click();
    await page.waitForSelector('#address-input');

    await page.fill('#address-input', '123 Main St, Boston');
    await page.locator('button:has-text("123 Main St, Boston, MA 02101")').click();

    // RouteData.FormattedDistance = "12.5 mi", FormattedTravelTime = "24 min"
    await expect(page.getByText('12.5 mi', { exact: true })).toBeVisible({ timeout: 10_000 });
    await expect(page.getByText('24 min', { exact: true })).toBeVisible();
  });

  test('shows turn-by-turn directions after expanding the directions panel', async ({ page }) => {
    await page.locator('button:has-text("Get Directions")').first().click();
    await page.waitForSelector('#address-input');

    await page.fill('#address-input', '123 Main St, Boston');
    await page.locator('button:has-text("123 Main St, Boston, MA 02101")').click();

    await expect(page.getByText('12.5 mi', { exact: true })).toBeVisible({ timeout: 10_000 });

    await page.locator('button:has-text("Turn-by-turn directions")').click();

    await expect(page.locator('text=Head north on Main St')).toBeVisible();
    await expect(page.locator('text=Turn right onto Route 9')).toBeVisible();
  });

  test('closes the directions modal when Cancel is clicked', async ({ page }) => {
    await page.locator('button:has-text("Get Directions")').first().click();
    await expect(page.locator('#address-input')).toBeVisible();

    await page.locator('button:has-text("Cancel")').click();

    await expect(page.locator('#address-input')).not.toBeVisible();
  });
});
