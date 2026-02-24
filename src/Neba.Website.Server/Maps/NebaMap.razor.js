// NebaMap - Resusable Azure Maps Component
// Displays locations on an interactive map with clustering, popups, and focus capabilities
// Note: Assumes 'atlas' is available globally from the Azure Maps SDK CDN

import { trackError, createTimer } from "../wwwroot/js/telemetry-helper";

let map = null;
let dataSource = null;
let markers = new Map(); // Track markers by location ID
let currentPopup = null; // Track the currently open popup
let dotNetHelper = null; // Reference to .NET component for callbacks
let boundsChangeTimeout = null; // Timeout for debouncing bounds changes
let lastLocationHash = null; // Hash of last locations to detect changes
let markerClickInProgress = false; // Flag to track if a marker/cluster was just clicked

// Directions mode state
let routeDataSource = null; // Data source for route line
let routeLayer = null; // Layer for route display
let startMarker = null; // Starting location marker
let subscriptionKey = null; // Azure Maps subscription key (stored for routing API)

// NOTE: Module-level state means only one NebaMap instance per page is supported.
// Multi-instance support (Map<containerId, state>) is deferred until needed.

/**
 * Waits for the Azure Maps SDK to be loaded
 * @returns {Promise} Promise that resolves when atlas is available
 */
function waitForAtlas() {
    return new Promise((resolve) => {
        if (typeof atlas !== 'undefined') {
            resolve();
            return;
        }

        const checkAtlas = setInterval(() => {
            if (typeof atlas !== 'undefined') {
                clearInterval(checkAtlas);
                resolve();
            }
        }, 100);

        setTimeout(() => {
            clearInterval(checkAtlas);
            if (typeof atlas === 'undefined') {
                console.error('[NebaMap] Azure Maps SDK failed to load within timeout');
            }
        }, 10000);
    });
}

/**
 * Initializes the Azure Maps instance with authentication and initial markers
 * @param {Object} authConfig - Authentication configuration { accountId?, subscriptionKey? }
 * @param {Object} mapConfig - Map configuration { containerId, center, zoom, enableClustering, style }
 * @param {Array} locations - Array of location objects with coordinates and metadata
 * @param {Object} dotNetRef - Reference to .NET component for callbacks
 */
export async function initializeMap(authConfig, mapConfig, locations, dotNetRef) {
    console.log('[NebaMap] Initializing Azure Maps...');
    console.log('[NebaMap] Auth config:', { hasAccountId: !!authConfig.accountId, hasSubscriptionKey: !!authConfig.subscriptionKey });
    console.log('[NebaMap] Locations count:', locations.length);

    await waitForAtlas();
    console.log('[NebaMap] Azure Maps SDK loaded');

    dotNetHelper = dotNetRef;

    globalThis.azureMapsAuthConfig = authConfig;

    let authOptions;
    if (authConfig.subscriptionKey) {
        console.log('[NebaMap] Using subscription key authentication');
        subscriptionKey = authConfig.subscriptionKey;
        globalThis.azureMapsSubscriptionKey = subscriptionKey;
        authOptions = {
            authType: 'subscriptionKey',
            subscriptionKey: authConfig.subscriptionKey
        };
    } else if (authConfig.accountId) {
        console.log('[NebaMap] Using Azure AD authentication with account:', authConfig.accountId);
        authOptions = {
            authType: 'aad',
            clientId: authConfig.accountId,
            getToken: async function(resolve, reject) {
                try {
                    const response = await fetch('/.auth/me');
                    const data = await response.json();
                    if (data?.[0]?.access_token) {
                        resolve(data[0].access_token);
                    } else {
                        reject(new Error('No access token available'));
                    }
                } catch (error) {
                    reject(error);
                }
            }
        };
    } else {
        console.error('[NebaMap] No authentication configured for Azure Maps');
        return;
    }

    try {
        map = new atlas.Map(mapConfig.containerId, {
            authOptions: authOptions,
            center: mapConfig.center,
            zoom: mapConfig.zoom,
            language: 'en-US',
            style: mapConfig.style || 'road',
            showLogo: false,
            showFeedbackLink: false,
            renderWorldCopies: false,
            refreshExpiredTiles: false
            // preserveDrawingBuffer removed - degrades WebGL performance
        });

        map.events.add('ready', () => {
            console.log('[NebaMap] Map ready');

            dataSource = new atlas.source.DataSource(null, {
                cluster: mapConfig.enableClustering,
                clusterRadius: 50,
                clusterMaxZoom: 14,
                buffer: 64,
                tolerance: 0.375
            });
            map.sources.add(dataSource);

            if (mapConfig.enableClustering) {
                addClusterLayers();
            }

            const symbolLayer = new atlas.layer.SymbolLayer(dataSource, null, {
                iconOptions: {
                    image: 'pin-red',
                    anchor: 'center',
                    allowOverlap: false
                },
                filter: mapConfig.enableClustering ? ['!', ['has', 'point_count']] : null
            });
            map.layers.add(symbolLayer);

            map.events.add('click', symbolLayer, (e) => {
                if (e.shapes && e.shapes.length > 0) {
                    markerClickInProgress = true;
                    const properties = e.shapes[0].getProperties();
                    showPopup(e.shapes[0].getCoordinates(), properties);
                }
            });

            map.events.add('mouseenter', symbolLayer, () => {
                map.getCanvasContainer().style.cursor = 'pointer';
            });

            map.events.add('mouseleave', symbolLayer, () => {
                map.getCanvasContainer().style.cursor = 'grab';
            });

            updateMarkers(locations);
            fitBounds();

            map.events.add('moveend', () => {
                notifyBoundsChanged();
            });

            map.events.add('click', () => {
                setTimeout(() => {
                    if (!markerClickInProgress && currentPopup) {
                        currentPopup.close();
                        currentPopup = null;
                    }
                    markerClickInProgress = false;
                }, 0);
            });

            // Notify Blazor that the map is fully initialized
            dotNetHelper.invokeMethodAsync('NotifyMapReady')
                .catch(error => console.error('[NebaMap] Error notifying map ready:', error));
        });

    } catch (error) {
        console.error('[NebaMap] Failed to initialize map:', error);
    }
}

/**
 * Adds cluster visualization layers to the map
 */
function addClusterLayers() {
    const clusterLayer = new atlas.layer.BubbleLayer(dataSource, null, {
        radius: 18,
        color: [
            'step',
            ['get', 'point_count'],
            '#0066b2',
            5, '#004080',
            10, '#002040'
        ],
        strokeWidth: 0,
        filter: ['has', 'point_count']
    });
    map.layers.add(clusterLayer);

    const clusterCountLayer = new atlas.layer.SymbolLayer(dataSource, null, {
        iconOptions: {
            image: 'none'
        },
        textOptions: {
            textField: ['get', 'point_count_abbreviated'],
            offset: [0, 0],
            color: '#ffffff',
            size: 12
        },
        filter: ['has', 'point_count']
    });
    map.layers.add(clusterCountLayer);

    map.events.add('click', clusterLayer, (e) => {
        if (e.shapes && e.shapes.length > 0) {
            const shape = e.shapes[0];
            const properties = shape.getProperties ? shape.getProperties() : shape.properties;

            if (properties?.cluster) {
                markerClickInProgress = true;
                const clusterId = properties.cluster_id;
                const coordinates = e.position;

                dataSource.getClusterExpansionZoom(clusterId).then((zoom) => {
                    map.setCamera({
                        center: coordinates,
                        zoom: zoom,
                        type: 'ease',
                        duration: 500
                    });
                });
            }
        }
    });

    map.events.add('mouseenter', clusterLayer, () => {
        map.getCanvasContainer().style.cursor = 'pointer';
    });

    map.events.add('mouseleave', clusterLayer, () => {
        map.getCanvasContainer().style.cursor = 'grab';
    });
}

/**
 * Updates the markers on the map with new location data
 * @param {Array} locations - Array of location objects
 */
export function updateMarkers(locations) {
    if (!dataSource) {
        console.warn('[NebaMap] Data source not initialized');
        return;
    }

    const locationHash = locations
        .map(l => l.id)
        .sort((a, b) => String(a).localeCompare(String(b), undefined, { numeric: true }))
        .join('|');
    if (locationHash === lastLocationHash) {
        console.log('[NebaMap] Locations unchanged, skipping marker update (cached)');
        return;
    }
    lastLocationHash = locationHash;

    console.log('[NebaMap] Updating markers:', locations.length);

    dataSource.clear();
    markers.clear();

    const features = locations
        .filter(location => {
            const isValid = typeof location.latitude === 'number' &&
                          typeof location.longitude === 'number' &&
                          !Number.isNaN(location.latitude) &&
                          !Number.isNaN(location.longitude) &&
                          Number.isFinite(location.latitude) &&
                          Number.isFinite(location.longitude);

            if (!isValid) {
                console.warn(`[NebaMap] Skipping location with invalid coordinates:`, location.id, location.latitude, location.longitude);
            }
            return isValid;
        })
        .map(location => {
            const feature = new atlas.data.Feature(
                new atlas.data.Point([location.longitude, location.latitude]),
                {
                    id: location.id,
                    title: location.title,
                    description: location.description,
                    ...location.metadata
                }
            );
            markers.set(location.id, feature);
            return feature;
        });

    console.log(`[NebaMap] Adding ${features.length} valid markers to map`);
    dataSource.add(features);
}

/**
 * Focuses the map on a specific location
 * @param {string} locationId - The ID of the location to focus on
 */
export function focusOnLocation(locationId) {
    if (!map || !markers.has(locationId)) {
        console.warn('[NebaMap] Cannot focus on location:', locationId);
        return;
    }

    const feature = markers.get(locationId);
    const coordinates = feature.geometry.coordinates;
    const properties = feature.properties;

    map.setCamera({
        center: coordinates,
        zoom: 15,
        type: 'ease',
        duration: 1000
    });

    setTimeout(() => {
        showPopup(coordinates, properties);
    }, 1100);
}

/**
 * Fits the map bounds to show all markers
 */
export function fitBounds() {
    if (!map || !dataSource) {
        return;
    }

    const shapes = dataSource.getShapes();
    if (shapes.length > 0) {
        const bounds = atlas.data.BoundingBox.fromData(shapes);
        map.setCamera({
            bounds: bounds,
            padding: 50
        });
    }
}

/**
 * Closes any open popup on the map
 */
export function closePopup() {
    if (currentPopup) {
        currentPopup.close();
        currentPopup = null;
    }
}

/**
 * Shows an info popup for a location
 * @param {Array} coordinates - [longitude, latitude]
 * @param {Object} properties - Location properties
 */
function showPopup(coordinates, properties) {
    if (currentPopup) {
        currentPopup.close();
    }

    const content = `
        <div style="padding: 12px; max-width: 280px;">
            <div style="font-weight: 700; font-size: 16px; color: #0066b2; margin-bottom: 8px;">
                ${properties.title}
            </div>
            <div style="font-size: 14px; color: #4b5563;">
                ${properties.description}
            </div>
        </div>
    `;

    currentPopup = new atlas.Popup({
        position: coordinates,
        content: content,
        pixelOffset: [0, -18]
    });

    currentPopup.open(map);
}

/**
 * Notifies the Blazor component about map bounds changes (debounced 150ms)
 */
function notifyBoundsChanged() {
    if (!map || !dotNetHelper) {
        return;
    }

    if (boundsChangeTimeout) {
        clearTimeout(boundsChangeTimeout);
    }

    boundsChangeTimeout = setTimeout(() => {
        const camera = map.getCamera();
        const bounds = camera.bounds;

        if (bounds) {
            const mapBounds = {
                north: bounds[3],
                south: bounds[1],
                east: bounds[2],
                west: bounds[0]
            };

            dotNetHelper.invokeMethodAsync('NotifyBoundsChanged', mapBounds)
                .catch(error => console.error('[NebaMap] Error notifying bounds changed:', error));
        }
    }, 150);
}

/**
 * Enters directions preview mode - zooms to selected location and dims other markers
 * @param {string} locationId - The ID of the destination location
 */
export function enterDirectionsPreview(locationId) {
    if (!map || !markers.has(locationId)) {
        console.warn('[NebaMap] Cannot enter directions preview for location:', locationId);
        return;
    }

    const feature = markers.get(locationId);
    const coordinates = feature.geometry.coordinates;

    closePopup();

    map.setCamera({
        center: coordinates,
        zoom: 13,
        type: 'ease',
        duration: 1000
    });

    const symbolLayers = map.layers.getLayers().filter(l => l instanceof atlas.layer.SymbolLayer);
    symbolLayers.forEach(layer => {
        layer.setOptions({
            iconOptions: {
                opacity: ['case',
                    ['==', ['get', 'id'], locationId],
                    1,
                    0.3
                ]
            }
        });
    });
}

/**
 * Calculates and displays a route from origin to destination using Azure Maps Route API
 * @param {number[]} origin - Origin coordinates [longitude, latitude]
 * @param {number[]} destination - Destination coordinates [longitude, latitude]
 * @returns {Promise<Object>} Route data with distance, time, and instructions
 */
export async function showRoute(origin, destination) {
    const timer = createTimer('map.route_calculation');

    if (!map) {
        const error = new Error('Map not initialized');
        trackError(error.message, 'map.route', error.stack);
        timer.stop(false, { error: 'map_not_initialized' });
        throw error;
    }

    const authConfig = globalThis.azureMapsAuthConfig;
    if (!authConfig) {
        const error = new Error('Authentication not configured');
        trackError(error.message, 'map.route', error.stack);
        timer.stop(false, { error: 'auth_not_configured' });
        throw error;
    }

    console.log('[NebaMap] Calculating route from', origin, 'to', destination);

    try {
        let url = `https://atlas.microsoft.com/route/directions/json?` +
            `api-version=1.0` +
            `&query=${origin[1]},${origin[0]}:${destination[1]},${destination[0]}` +
            `&travelMode=car` +
            `&instructionsType=text` +
            `&guidance=true`;

        let headers = {};

        if (authConfig.subscriptionKey) {
            url += `&subscription-key=${authConfig.subscriptionKey}`;
        } else if (authConfig.accountId) {
            const token = await getAzureADTokenForRoute();
            if (!token) {
                const error = new Error('Failed to get Azure AD token for route calculation');
                trackError(error.message, 'map.route', error.stack);
                timer.stop(false, { error: 'token_acquisition_failed' });
                throw error;
            }
            headers['Authorization'] = `Bearer ${token}`;
            headers['x-ms-client-id'] = authConfig.accountId;
        }

        const response = await fetch(url, { headers });

        if (!response.ok) {
            const error = new Error(`Route API error: ${response.status} ${response.statusText}`);
            trackError(error.message, 'map.route', error.stack);
            timer.stop(false, { error: 'api_error', status_code: response.status });
            throw error;
        }

        const data = await response.json();

        if (!data.routes || data.routes.length === 0) {
            const error = new Error('No route found');
            trackError(error.message, 'map.route', error.stack);
            timer.stop(false, { error: 'no_route_found' });
            throw error;
        }

        const route = data.routes[0];
        const summary = route.summary;

        let instructions = [];

        if (route.guidance?.instructions && route.guidance.instructions.length > 0) {
            instructions = route.guidance.instructions.map(instruction => ({
                Text: instruction.message || instruction.instructionType || instruction.text || 'Continue',
                DistanceMeters: instruction.travelDistance || instruction.routeOffsetInMeters || 0
            }));
        } else if (route.guidance?.instructionGroups && route.guidance.instructionGroups.length > 0) {
            const allInstructions = route.guidance.instructionGroups.flatMap(group => group.instructions || []);
            instructions = allInstructions.map(instruction => ({
                Text: instruction.message || instruction.instructionType || instruction.text || 'Continue',
                DistanceMeters: instruction.travelDistance || instruction.routeOffsetInMeters || 0
            }));
        }

        const routeData = {
            DistanceMeters: summary.lengthInMeters,
            TravelTimeSeconds: summary.travelTimeInSeconds,
            Instructions: instructions,
            RouteGeoJson: null
        };

        drawRoute(route, origin, destination);

        timer.stop(true, {
            distance_meters: summary.lengthInMeters,
            travel_time_seconds: summary.travelTimeInSeconds,
            instruction_count: instructions.length
        });

        console.log('[NebaMap] Route calculated:', routeData);
        return routeData;
    } catch (error) {
        console.error('[NebaMap] Error calculating route:', error);
        trackError(error.message, 'map.route', error.stack);
        timer.stop(false, { error: error.message });
        throw error;
    }
}

/**
 * Draws the route line and start marker on the map
 */
function drawRoute(route, origin, destination) {
    if (!routeDataSource) {
        routeDataSource = new atlas.source.DataSource();
        map.sources.add(routeDataSource);

        routeLayer = new atlas.layer.LineLayer(routeDataSource, null, {
            strokeColor: '#0066b2',
            strokeWidth: 5,
            strokeOpacity: 0.8
        });
        map.layers.add(routeLayer, 'labels');
    }

    routeDataSource.clear();

    const routeLine = route.legs[0].points.map(p => [p.longitude, p.latitude]);
    routeDataSource.add(new atlas.data.Feature(
        new atlas.data.LineString(routeLine)
    ));

    const startPoint = new atlas.data.Feature(
        new atlas.data.Point(origin),
        { type: 'start' }
    );
    routeDataSource.add(startPoint);

    if (!startMarker) {
        startMarker = new atlas.layer.SymbolLayer(routeDataSource, null, {
            iconOptions: {
                image: 'pin-blue',
                anchor: 'center',
                size: 1
            },
            filter: ['==', ['get', 'type'], 'start']
        });
        map.layers.add(startMarker);
    }

    const bounds = atlas.data.BoundingBox.fromData([
        new atlas.data.Point(origin),
        new atlas.data.Point(destination)
    ]);

    map.setCamera({
        bounds: bounds,
        padding: 80,
        type: 'ease',
        duration: 1000
    });
}

/**
 * Exits directions mode and returns to overview
 */
export function exitDirectionsMode() {
    console.log('[NebaMap] Exiting directions mode');

    if (routeLayer) {
        map.layers.remove(routeLayer);
        routeLayer = null;
    }

    if (startMarker) {
        map.layers.remove(startMarker);
        startMarker = null;
    }

    if (routeDataSource) {
        map.sources.remove(routeDataSource);
        routeDataSource = null;
    }

    const symbolLayers = map.layers.getLayers().filter(l => l instanceof atlas.layer.SymbolLayer);
    symbolLayers.forEach(layer => {
        layer.setOptions({
            iconOptions: {
                opacity: 1
            }
        });
    });

    fitBounds();
}

/**
 * Changes the map style/view
 * @param {string} style - 'road', 'satellite', or 'satellite_road_labels'
 */
export function setMapStyle(style) {
    if (!map) {
        console.warn('[NebaMap] Cannot change map style - map not initialized');
        return;
    }

    const validStyles = ['road', 'satellite', 'satellite_road_labels', 'grayscale_dark', 'grayscale_light', 'night', 'road_shaded_relief'];
    if (!validStyles.includes(style)) {
        console.warn('[NebaMap] Invalid map style:', style);
        return;
    }

    map.setStyle({ style: style });
}

/**
 * Gets an Azure AD access token via App Service Easy Auth
 */
async function getAzureADTokenForRoute() {
    try {
        const response = await fetch('/.auth/me');

        if (!response.ok) {
            console.warn('[NebaMap] Could not fetch auth info from /.auth/me');
            return null;
        }

        const data = await response.json();

        if (data?.[0]?.access_token) {
            return data[0].access_token;
        }

        console.warn('[NebaMap] No access token found in auth response');
        return null;
    } catch (error) {
        console.error('[NebaMap] Error getting Azure AD token:', error);
        return null;
    }
}

/**
 * Cleans up the map instance and removes all event listeners
 */
export function dispose() {
    console.log('[NebaMap] Disposing map resources...');

    if (boundsChangeTimeout) {
        clearTimeout(boundsChangeTimeout);
        boundsChangeTimeout = null;
    }

    if (currentPopup) {
        currentPopup.close();
        currentPopup = null;
    }

    if (routeLayer) {
        map?.layers.remove(routeLayer);
        routeLayer = null;
    }

    if (startMarker) {
        map?.layers.remove(startMarker);
        startMarker = null;
    }

    if (routeDataSource) {
        map?.sources.remove(routeDataSource);
        routeDataSource = null;
    }

    if (map) {
        map.dispose();
        map = null;
    }

    dataSource = null;
    markers.clear();
    dotNetHelper = null;
    lastLocationHash = null;
    markerClickInProgress = false;
    subscriptionKey = null;

    console.log('[NebaMap] Map disposed successfully');
}