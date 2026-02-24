// Tests for NebaMap.razor.js
// Note: NebaMap uses module-level state; dispose() resets it between tests.

import {
  initializeMap,
  updateMarkers,
  focusOnLocation,
  fitBounds,
  closePopup,
  enterDirectionsPreview,
  showRoute,
  exitDirectionsMode,
  setMapStyle,
  dispose,
} from './NebaMap.razor.js';

// ---------------------------------------------------------------------------
// Test helpers
// ---------------------------------------------------------------------------

/**
 * Builds a fresh atlas mock. SymbolLayer is a real class so instanceof checks work.
 * DataSource calls are tracked so tests can inspect the main vs. route data sources.
 */
function createAtlasMock() {
  class SymbolLayerMock {
    constructor() {
      this.setOptions = jest.fn();
    }
  }

  const dataSources = [];
  function makeDataSource() {
    const ds = {
      add: jest.fn(),
      clear: jest.fn(),
      getShapes: jest.fn(() => []),
      getClusterExpansionZoom: jest.fn(() => Promise.resolve(15)),
    };
    dataSources.push(ds);
    return ds;
  }

  const addedLayers = [];
  const mockMap = {
    events: {
      add: jest.fn((event, targetOrCb, maybeCb) => {
        if (event === 'ready') {
          const cb = typeof targetOrCb === 'function' ? targetOrCb : maybeCb;
          cb(); // fire synchronously so initializeMap can be awaited
        }
      }),
    },
    layers: {
      add: jest.fn((layer) => addedLayers.push(layer)),
      remove: jest.fn(),
      getLayers: jest.fn(() => [...addedLayers]),
    },
    sources: {
      add: jest.fn(),
      remove: jest.fn(),
    },
    setCamera: jest.fn(),
    setStyle: jest.fn(),
    getCamera: jest.fn(() => ({ bounds: [-72, 41, -70, 43] })),
    getCanvasContainer: jest.fn(() => ({ style: {} })),
    dispose: jest.fn(),
  };

  const atlasMock = {
    Map: jest.fn(() => mockMap),
    Popup: jest.fn().mockImplementation(() => ({ open: jest.fn(), close: jest.fn() })),
    source: { DataSource: jest.fn(makeDataSource) },
    layer: {
      SymbolLayer: SymbolLayerMock,
      BubbleLayer: jest.fn(),
      LineLayer: jest.fn(),
    },
    data: {
      Feature: jest.fn((geometry, props) => ({
        geometry,
        properties: props,
        getCoordinates: jest.fn(() => geometry?.coordinates ?? geometry),
        getProperties: jest.fn(() => props),
      })),
      Point: jest.fn((coords) => ({ type: 'Point', coordinates: coords })),
      LineString: jest.fn((coords) => ({ type: 'LineString', coordinates: coords })),
      BoundingBox: { fromData: jest.fn(() => [-72, 41, -70, 43]) },
    },
  };

  return { atlasMock, mockMap, addedLayers, dataSources };
}

const defaultAuthConfig = { subscriptionKey: 'test-key-123' };
const defaultMapConfig = {
  containerId: 'test-map',
  center: [-71, 42],
  zoom: 10,
  enableClustering: false,
  style: 'road',
};

function makeLocation(overrides = {}) {
  return {
    id: 'loc-1',
    title: 'Test Location',
    description: 'A test bowling center',
    latitude: 42.36,
    longitude: -71.06,
    metadata: {},
    ...overrides,
  };
}

function makeSuccessfulRouteResponse(overrides = {}) {
  return {
    ok: true,
    json: () =>
      Promise.resolve({
        routes: [
          {
            summary: { lengthInMeters: 5000, travelTimeInSeconds: 300 },
            guidance: {
              instructions: [
                { message: 'Head north', travelDistance: 100 },
                { message: 'Turn right', travelDistance: 400 },
              ],
            },
            legs: [
              {
                points: [
                  { longitude: -71, latitude: 42 },
                  { longitude: -70, latitude: 43 },
                ],
              },
            ],
          },
        ],
        ...overrides,
      }),
  };
}

/**
 * Initializes the map with the given config and locations.
 * Returns the underlying mock objects for assertions.
 */
async function createInitializedMap(mapConfig = defaultMapConfig, locations = []) {
  const { atlasMock, mockMap, addedLayers, dataSources } = createAtlasMock();
  globalThis.atlas = atlasMock;

  const dotNetHelper = { invokeMethodAsync: jest.fn().mockResolvedValue(undefined) };
  await initializeMap(defaultAuthConfig, mapConfig, locations, dotNetHelper);

  return {
    atlasMock,
    mockMap,
    mockDataSource: dataSources[0],
    addedLayers,
    dataSources,
    dotNetHelper,
  };
}

// ---------------------------------------------------------------------------

describe('NebaMap', () => {
  beforeEach(() => {
    jest.spyOn(console, 'log').mockImplementation(() => {});
    jest.spyOn(console, 'warn').mockImplementation(() => {});
    jest.spyOn(console, 'error').mockImplementation(() => {});

    dispose(); // reset all module-level state between tests
    delete globalThis.azureMapsAuthConfig;
    delete globalThis.azureMapsSubscriptionKey;
  });

  afterEach(() => {
    jest.restoreAllMocks();
    jest.clearAllTimers();
    jest.useRealTimers();
    delete global.fetch;
  });

  // -------------------------------------------------------------------------
  describe('initializeMap', () => {
    test('logs an error and skips map creation when no auth is configured', async () => {
      const { atlasMock } = createAtlasMock();
      globalThis.atlas = atlasMock;

      await initializeMap({}, defaultMapConfig, [], { invokeMethodAsync: jest.fn() });

      expect(console.error).toHaveBeenCalledWith(
        '[NebaMap] No authentication configured for Azure Maps',
      );
      expect(atlasMock.Map).not.toHaveBeenCalled();
    });

    test('stores subscription key globally when using subscription key auth', async () => {
      await createInitializedMap();

      expect(globalThis.azureMapsSubscriptionKey).toBe('test-key-123');
    });

    test('creates the Map with the correct container and config', async () => {
      const { atlasMock } = createAtlasMock();
      globalThis.atlas = atlasMock;

      await initializeMap(
        defaultAuthConfig,
        defaultMapConfig,
        [],
        { invokeMethodAsync: jest.fn().mockResolvedValue(undefined) },
      );

      expect(atlasMock.Map).toHaveBeenCalledWith(
        'test-map',
        expect.objectContaining({ center: [-71, 42], zoom: 10 }),
      );
    });

    test('notifies Blazor via NotifyMapReady when the ready event fires', async () => {
      const { dotNetHelper } = await createInitializedMap();

      expect(dotNetHelper.invokeMethodAsync).toHaveBeenCalledWith('NotifyMapReady');
    });

    test('adds cluster layers when enableClustering is true', async () => {
      const clusterConfig = { ...defaultMapConfig, enableClustering: true };
      const { atlasMock } = createAtlasMock();
      globalThis.atlas = atlasMock;

      await initializeMap(
        defaultAuthConfig,
        clusterConfig,
        [],
        { invokeMethodAsync: jest.fn().mockResolvedValue(undefined) },
      );

      expect(atlasMock.layer.BubbleLayer).toHaveBeenCalled();
    });

    test('does not add cluster layers when enableClustering is false', async () => {
      const { atlasMock } = await createInitializedMap();

      expect(atlasMock.layer.BubbleLayer).not.toHaveBeenCalled();
    });
  });

  // -------------------------------------------------------------------------
  describe('closePopup', () => {
    test('does nothing when no popup is open', () => {
      expect(() => closePopup()).not.toThrow();
    });
  });

  // -------------------------------------------------------------------------
  describe('setMapStyle', () => {
    test('warns when map is not initialized', () => {
      setMapStyle('road');

      expect(console.warn).toHaveBeenCalledWith(
        '[NebaMap] Cannot change map style - map not initialized',
      );
    });

    test('warns and skips setStyle for unrecognised style names', async () => {
      const { mockMap } = await createInitializedMap();

      setMapStyle('hand-drawn');

      expect(console.warn).toHaveBeenCalledWith('[NebaMap] Invalid map style:', 'hand-drawn');
      expect(mockMap.setStyle).not.toHaveBeenCalled();
    });

    test.each([
      'road',
      'satellite',
      'satellite_road_labels',
      'grayscale_dark',
      'grayscale_light',
      'night',
      'road_shaded_relief',
    ])('applies valid style "%s" to the map', async (style) => {
      const { mockMap } = await createInitializedMap();
      mockMap.setStyle.mockClear();

      setMapStyle(style);

      expect(mockMap.setStyle).toHaveBeenCalledWith({ style });
    });
  });

  // -------------------------------------------------------------------------
  describe('updateMarkers', () => {
    test('warns when data source is not initialized', () => {
      updateMarkers([makeLocation()]);

      expect(console.warn).toHaveBeenCalledWith('[NebaMap] Data source not initialized');
    });

    test('skips re-render when the location ID set has not changed', async () => {
      const { mockDataSource } = await createInitializedMap();
      const locations = [makeLocation({ id: 'abc' })];

      updateMarkers(locations);
      const addCount = mockDataSource.add.mock.calls.length;

      updateMarkers(locations); // same IDs → same hash

      expect(mockDataSource.add.mock.calls.length).toBe(addCount);
      expect(console.log).toHaveBeenCalledWith(expect.stringContaining('Locations unchanged'));
    });

    test('re-renders when the location ID set changes', async () => {
      const { mockDataSource } = await createInitializedMap();

      updateMarkers([makeLocation({ id: 'x' })]);
      const addCountAfterFirst = mockDataSource.add.mock.calls.length;

      updateMarkers([makeLocation({ id: 'y' })]); // different IDs → different hash

      expect(mockDataSource.add.mock.calls.length).toBeGreaterThan(addCountAfterFirst);
    });

    test('filters out locations with NaN latitude', async () => {
      await createInitializedMap();

      updateMarkers([makeLocation({ id: 'bad', latitude: NaN })]);

      expect(console.warn).toHaveBeenCalledWith(
        expect.stringContaining('Skipping location with invalid coordinates'),
        'bad',
        NaN,
        -71.06,
      );
    });

    test('filters out locations with Infinity longitude', async () => {
      await createInitializedMap();

      updateMarkers([makeLocation({ id: 'bad', longitude: Infinity })]);

      expect(console.warn).toHaveBeenCalledWith(
        expect.stringContaining('Skipping location with invalid coordinates'),
        'bad',
        42.36,
        Infinity,
      );
    });

    test('filters out locations with null coordinates', async () => {
      await createInitializedMap();

      updateMarkers([makeLocation({ id: 'bad', latitude: null })]);

      expect(console.warn).toHaveBeenCalledWith(
        expect.stringContaining('Skipping location with invalid coordinates'),
        'bad',
        null,
        -71.06,
      );
    });

    test('adds only valid features to the data source', async () => {
      const { mockDataSource } = await createInitializedMap();

      updateMarkers([
        makeLocation({ id: 'valid' }),
        makeLocation({ id: 'bad-nan', latitude: NaN }),
        makeLocation({ id: 'bad-inf', longitude: Infinity }),
      ]);

      const features = mockDataSource.add.mock.calls.at(-1)[0];
      expect(features).toHaveLength(1);
    });

    test('spreads location metadata into feature properties', async () => {
      await createInitializedMap();

      updateMarkers([makeLocation({ id: 'a', metadata: { league: 'mens', lanes: 32 } })]);

      const featureCalls = globalThis.atlas.data.Feature.mock.calls;
      const props = featureCalls.at(-1)[1];
      expect(props).toMatchObject({ league: 'mens', lanes: 32 });
    });

    test('clears the data source before adding new features', async () => {
      const { mockDataSource } = await createInitializedMap();
      mockDataSource.clear.mockClear();

      updateMarkers([makeLocation({ id: 'new' })]);

      expect(mockDataSource.clear).toHaveBeenCalled();
    });
  });

  // -------------------------------------------------------------------------
  describe('focusOnLocation', () => {
    test('warns when the location id is not in the marker registry', async () => {
      await createInitializedMap();

      focusOnLocation('no-such-id');

      expect(console.warn).toHaveBeenCalledWith(
        '[NebaMap] Cannot focus on location:',
        'no-such-id',
      );
    });

    test('sets camera to zoom 15 at the location', async () => {
      jest.useFakeTimers();
      const { mockMap } = await createInitializedMap(defaultMapConfig, [
        makeLocation({ id: 'target' }),
      ]);
      mockMap.setCamera.mockClear();

      focusOnLocation('target');

      expect(mockMap.setCamera).toHaveBeenCalledWith(
        expect.objectContaining({ zoom: 15, type: 'ease', duration: 1000 }),
      );
    });

    test('opens a popup after a 1100 ms delay', async () => {
      jest.useFakeTimers();
      await createInitializedMap(defaultMapConfig, [makeLocation({ id: 'target' })]);

      focusOnLocation('target');

      expect(globalThis.atlas.Popup).not.toHaveBeenCalled();
      jest.advanceTimersByTime(1100);
      expect(globalThis.atlas.Popup).toHaveBeenCalled();
    });
  });

  // -------------------------------------------------------------------------
  describe('fitBounds', () => {
    test('does nothing when map is not initialized', () => {
      expect(() => fitBounds()).not.toThrow();
    });

    test('does nothing when the data source has no shapes', async () => {
      const { mockDataSource, atlasMock } = await createInitializedMap();
      mockDataSource.getShapes.mockReturnValue([]);
      atlasMock.data.BoundingBox.fromData.mockClear();

      fitBounds();

      expect(atlasMock.data.BoundingBox.fromData).not.toHaveBeenCalled();
    });

    test('sets camera to fit all shapes when shapes exist', async () => {
      const { mockMap, mockDataSource } = await createInitializedMap();
      const shape = { id: 'shape-1' };
      mockDataSource.getShapes.mockReturnValue([shape]);
      mockMap.setCamera.mockClear();

      fitBounds();

      expect(globalThis.atlas.data.BoundingBox.fromData).toHaveBeenCalledWith([shape]);
      expect(mockMap.setCamera).toHaveBeenCalledWith(
        expect.objectContaining({ padding: 50 }),
      );
    });
  });

  // -------------------------------------------------------------------------
  describe('enterDirectionsPreview', () => {
    test('warns when the location is not in the marker registry', async () => {
      await createInitializedMap();

      enterDirectionsPreview('ghost-id');

      expect(console.warn).toHaveBeenCalledWith(
        '[NebaMap] Cannot enter directions preview for location:',
        'ghost-id',
      );
    });

    test('zooms to the destination at zoom level 13', async () => {
      const { mockMap } = await createInitializedMap(defaultMapConfig, [
        makeLocation({ id: 'dest' }),
      ]);
      mockMap.setCamera.mockClear();

      enterDirectionsPreview('dest');

      expect(mockMap.setCamera).toHaveBeenCalledWith(
        expect.objectContaining({ zoom: 13, type: 'ease' }),
      );
    });

    test('calls setOptions on symbol layers to dim non-selected markers', async () => {
      const { mockMap, atlasMock } = await createInitializedMap(defaultMapConfig, [
        makeLocation({ id: 'dest' }),
      ]);

      // Layers added during initializeMap are SymbolLayer instances
      const symbolLayers = mockMap.layers
        .getLayers()
        .filter((l) => l instanceof atlasMock.layer.SymbolLayer);
      expect(symbolLayers.length).toBeGreaterThan(0);

      enterDirectionsPreview('dest');

      symbolLayers.forEach((layer) => {
        expect(layer.setOptions).toHaveBeenCalledWith(
          expect.objectContaining({ iconOptions: expect.any(Object) }),
        );
      });
    });
  });

  // -------------------------------------------------------------------------
  describe('showRoute', () => {
    test('throws when map is not initialized', async () => {
      // dispose() in beforeEach leaves map null
      await expect(showRoute([-71, 42], [-70, 43])).rejects.toThrow('Map not initialized');
    });

    test('throws when auth config is not set', async () => {
      await createInitializedMap();
      delete globalThis.azureMapsAuthConfig;

      await expect(showRoute([-71, 42], [-70, 43])).rejects.toThrow(
        'Authentication not configured',
      );
    });

    test('throws when the route API returns a non-ok response', async () => {
      await createInitializedMap();
      global.fetch = jest
        .fn()
        .mockResolvedValue({ ok: false, status: 503, statusText: 'Service Unavailable' });

      await expect(showRoute([-71, 42], [-70, 43])).rejects.toThrow('Route API error: 503');
    });

    test('throws when the API response contains no routes', async () => {
      await createInitializedMap();
      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve({ routes: [] }),
      });

      await expect(showRoute([-71, 42], [-70, 43])).rejects.toThrow('No route found');
    });

    test('returns distance and travel time from the route summary', async () => {
      await createInitializedMap();
      global.fetch = jest.fn().mockResolvedValue(makeSuccessfulRouteResponse());

      const result = await showRoute([-71, 42], [-70, 43]);

      expect(result.DistanceMeters).toBe(5000);
      expect(result.TravelTimeSeconds).toBe(300);
    });

    test('maps guidance instructions to the Instructions array', async () => {
      await createInitializedMap();
      global.fetch = jest.fn().mockResolvedValue(makeSuccessfulRouteResponse());

      const result = await showRoute([-71, 42], [-70, 43]);

      expect(result.Instructions).toHaveLength(2);
      expect(result.Instructions[0]).toMatchObject({ Text: 'Head north', DistanceMeters: 100 });
    });

    test('falls back to instructionGroups when instructions array is absent', async () => {
      await createInitializedMap();
      global.fetch = jest.fn().mockResolvedValue({
        ok: true,
        json: () =>
          Promise.resolve({
            routes: [
              {
                summary: { lengthInMeters: 2000, travelTimeInSeconds: 120 },
                guidance: {
                  instructionGroups: [
                    { instructions: [{ message: 'Follow the road', travelDistance: 500 }] },
                  ],
                },
                legs: [{ points: [{ longitude: -71, latitude: 42 }] }],
              },
            ],
          }),
      });

      const result = await showRoute([-71, 42], [-70, 43]);

      expect(result.Instructions).toHaveLength(1);
      expect(result.Instructions[0].Text).toBe('Follow the road');
    });

    test('includes the subscription key in the route API URL', async () => {
      await createInitializedMap();
      global.fetch = jest.fn().mockResolvedValue(makeSuccessfulRouteResponse());

      await showRoute([-71, 42], [-70, 43]);

      expect(global.fetch.mock.calls[0][0]).toContain('subscription-key=test-key-123');
    });

    test('encodes origin and destination as lat,lon pairs in the query', async () => {
      await createInitializedMap();
      global.fetch = jest.fn().mockResolvedValue(makeSuccessfulRouteResponse());

      // origin: [-71, 42] → lat=42, lon=-71  |  destination: [-70, 43] → lat=43, lon=-70
      await showRoute([-71, 42], [-70, 43]);

      expect(global.fetch.mock.calls[0][0]).toContain('query=42,-71:43,-70');
    });
  });

  // -------------------------------------------------------------------------
  describe('exitDirectionsMode', () => {
    test('is safe to call when no route is active', async () => {
      await createInitializedMap();

      expect(() => exitDirectionsMode()).not.toThrow();
    });

    test('removes route layer and data source after showRoute', async () => {
      const { mockMap } = await createInitializedMap();
      global.fetch = jest.fn().mockResolvedValue(makeSuccessfulRouteResponse());
      await showRoute([-71, 42], [-70, 43]);

      mockMap.layers.remove.mockClear();
      mockMap.sources.remove.mockClear();

      exitDirectionsMode();

      expect(mockMap.layers.remove).toHaveBeenCalled();
      expect(mockMap.sources.remove).toHaveBeenCalled();
    });

    test('restores full opacity on all symbol layers', async () => {
      const { mockMap, atlasMock } = await createInitializedMap(defaultMapConfig, [
        makeLocation({ id: 'dest' }),
      ]);
      global.fetch = jest.fn().mockResolvedValue(makeSuccessfulRouteResponse());
      await showRoute([-71, 42], [-70, 43]);

      const symbolLayers = mockMap.layers
        .getLayers()
        .filter((l) => l instanceof atlasMock.layer.SymbolLayer);

      symbolLayers.forEach((l) => l.setOptions.mockClear());

      exitDirectionsMode();

      symbolLayers.forEach((layer) => {
        expect(layer.setOptions).toHaveBeenCalledWith(
          expect.objectContaining({ iconOptions: { opacity: 1 } }),
        );
      });
    });
  });

  // -------------------------------------------------------------------------
  describe('dispose', () => {
    test('is safe to call when map is already null', () => {
      // beforeEach already called dispose(); call again to confirm idempotent
      expect(() => dispose()).not.toThrow();
    });

    test('calls map.dispose() on the underlying Atlas map', async () => {
      const { mockMap } = await createInitializedMap();

      dispose();

      expect(mockMap.dispose).toHaveBeenCalled();
    });

    test('resets state so updateMarkers warns after disposal', async () => {
      await createInitializedMap();

      dispose();
      updateMarkers([makeLocation()]);

      expect(console.warn).toHaveBeenCalledWith('[NebaMap] Data source not initialized');
    });

    test('resets state so focusOnLocation warns after disposal', async () => {
      await createInitializedMap(defaultMapConfig, [makeLocation({ id: 'loc' })]);

      dispose();
      focusOnLocation('loc');

      expect(console.warn).toHaveBeenCalledWith('[NebaMap] Cannot focus on location:', 'loc');
    });

    test('resets state so setMapStyle warns after disposal', async () => {
      await createInitializedMap();

      dispose();
      setMapStyle('road');

      expect(console.warn).toHaveBeenCalledWith(
        '[NebaMap] Cannot change map style - map not initialized',
      );
    });
  });
});
