// DirectionsModal - Handles geolocation and address search for directions feature

/**
 * Gets the user's current location using the browser's Geolocation API
 * @returns {Promise<number[]>} Promise that resolves to [longitude, latitude]
 */
export async function getCurrentLocation() {
    return new Promise((resolve, reject) => {
        if (!navigator.geolocation) {
            reject(new Error('Geolocation is not supported by your browser'));
            return;
        }

        navigator.geolocation.getCurrentPosition(
            (position) => {
                const longitude = position.coords.longitude;
                const latitude = position.coords.latitude;
                console.log('[DirectionsModal] Got current location:', { latitude, longitude });
                resolve([longitude, latitude]);
            },
            (error) => {
                let errorMessage = 'Unable to retrieve your location';

                switch (error.code) {
                    case error.PERMISSION_DENIED:
                        errorMessage = 'Location access denied. Please enable location services.';
                        break;
                    case error.POSITION_UNAVAILABLE:
                        errorMessage = 'Location information unavailable.';
                        break;
                    case error.TIMEOUT:
                        errorMessage = 'Location request timed out.';
                        break;
                }

                console.error('[DirectionsModal] Geolocation error:', errorMessage, error);
                reject(new Error(errorMessage));
            },
            {
                enableHighAccuracy: false, // Don't require GPS, WiFi/cell tower is fine
                timeout: 10000, // 10 second timeout
                maximumAge: 300000 // Accept cached location up to 5 minutes old
            }
        );
    });
}

/**
 * Searches for address suggestions using Azure Maps Search API
 * Supports both subscription key (local dev) and Azure AD (production) authentication
 * @param {string} query - The address search query
 * @returns {Promise<Array>} Promise that resolves to array of address suggestions
 */
export async function searchAddress(query) {
    console.log('[DirectionsModal] Searching for address:', query);

    // Wait for Azure Maps SDK to be loaded
    if (typeof atlas === 'undefined') {
        console.error('[DirectionsModal] Azure Maps SDK not loaded');
        return [];
    }

    try {
        // Get authentication configuration
        const authConfig = globalThis.azureMapsAuthConfig;

        if (!authConfig) {
            console.error('[DirectionsModal] No Azure Maps auth configuration available');
            return [];
        }

        let url = `https://atlas.microsoft.com/search/address/json?` +
            `api-version=1.0` +
            `&query=${encodeURIComponent(query)}` +
            `&limit=5` +
            `&countrySet=US` +
            `&view=Auto`;

        let headers = {};

        // Determine authentication method
        if (authConfig.subscriptionKey) {
            // Local development: Use subscription key
            console.log('[DirectionsModal] Using subscription key authentication');
            url += `&subscription-key=${authConfig.subscriptionKey}`;
        } else if (authConfig.accountId) {
            // Production: Use Azure AD authentication
            console.log('[DirectionsModal] Using Azure AD authentication');
            const token = await getAzureADToken();
            if (!token) {
                console.error('[DirectionsModal] Failed to get Azure AD token');
                return [];
            }
            headers['Authorization'] = `Bearer ${token}`;
            headers['x-ms-client-id'] = authConfig.accountId;
        } else {
            console.error('[DirectionsModal] No valid authentication method available');
            return [];
        }

        console.log('[DirectionsModal] Fetching from Azure Maps Search API...');
        const response = await fetch(url, { headers });

        if (!response.ok) {
            const errorText = await response.text();
            console.error('[DirectionsModal] Search API error:', response.status, response.statusText, errorText);
            return [];
        }

        const data = await response.json();
        console.log('[DirectionsModal] Search API response:', data);

        if (!data.results || data.results.length === 0) {
            console.log('[DirectionsModal] No results found for query:', query);
            return [];
        }

        // Transform results to our format
        // Note: Using PascalCase to match C# AddressSuggestion class for proper deserialization
        const suggestions = data.results.map(result => ({
            Address: result.address.freeformAddress,
            Locality: result.address.municipality || result.address.countrySubdivision,
            Latitude: result.position.lat,
            Longitude: result.position.lon
        }));

        console.log('[DirectionsModal] Found address suggestions:', suggestions.length, suggestions);
        return suggestions;
    } catch (error) {
        console.error('[DirectionsModal] Error searching address:', error);
        return [];
    }
}

/**
 * Opens a URL in a new browser tab
 * @param {string} url - The URL to open
 */
export function openInNewTab(url) {
    window.open(url, '_blank', 'noopener,noreferrer');
}

// ---------------------------------------------------------------------------
// Route mini-map (shown inside the modal once directions are calculated)
// ---------------------------------------------------------------------------

let routeMap = null;

/**
 * Waits for Azure Maps SDK to be available.
 * @param {number} timeoutMs - Maximum wait time in milliseconds
 * @returns {Promise<void>}
 */
function waitForAtlas(timeoutMs = 10000) {
    return new Promise((resolve, reject) => {
        if (typeof atlas !== 'undefined') {
            resolve();
            return;
        }

        const startedAt = Date.now();
        const interval = setInterval(() => {
            if (typeof atlas !== 'undefined') {
                clearInterval(interval);
                resolve();
                return;
            }

            if (Date.now() - startedAt >= timeoutMs) {
                clearInterval(interval);
                reject(new Error('Azure Maps SDK did not load in time'));
            }
        }, 100);
    });
}

/**
 * Waits until the target container exists and has dimensions.
 * @param {string} containerId - DOM id for the map container
 * @param {number} timeoutMs - Maximum wait time in milliseconds
 * @returns {Promise<HTMLElement|null>}
 */
function waitForVisibleContainer(containerId, timeoutMs = 2000) {
    return new Promise((resolve) => {
        const startedAt = Date.now();

        const check = () => {
            const element = document.getElementById(containerId);
            if (element && element.clientWidth > 0 && element.clientHeight > 0) {
                resolve(element);
                return;
            }

            if (Date.now() - startedAt >= timeoutMs) {
                resolve(element ?? null);
                return;
            }

            requestAnimationFrame(check);
        };

        check();
    });
}

/**
 * Initializes a compact Azure Maps instance inside the directions modal,
 * rendering the route line and start/end markers.
 * @param {string} containerId - ID of the DOM element to render the map into
 * @param {number[]} origin - [longitude, latitude] of the user's starting point
 * @param {number[]} destination - [longitude, latitude] of the bowling center
 * @param {string|null} routeGeoJson - GeoJSON Feature string for the route line
 */
export async function initializeRouteMap(containerId, origin, destination, routeGeoJson) {
    disposeRouteMap();

    try {
        await waitForAtlas();
    } catch (error) {
        console.error('[DirectionsModal] Azure Maps SDK unavailable for route map:', error);
        return;
    }

    const container = await waitForVisibleContainer(containerId);
    if (!container) {
        console.error('[DirectionsModal] Route map container not found:', containerId);
        return;
    }

    const authConfig = globalThis.azureMapsAuthConfig;
    if (!authConfig) {
        console.error('[DirectionsModal] No Azure Maps auth config available for route map');
        return;
    }

    const mapOptions = { language: 'en-US' };

    if (authConfig.subscriptionKey) {
        mapOptions.authOptions = { authType: 'subscriptionKey', subscriptionKey: authConfig.subscriptionKey };
    } else if (authConfig.accountId) {
        mapOptions.authOptions = { authType: 'aad', clientId: authConfig.accountId };
    } else {
        console.error('[DirectionsModal] No valid auth method available for route map');
        return;
    }

    try {
        routeMap = new atlas.Map(containerId, mapOptions);
    } catch (error) {
        console.error('[DirectionsModal] Failed to create route map:', error);
        return;
    }

    routeMap.events.add('error', (event) => {
        console.error('[DirectionsModal] Route map error event:', event);
    });

    routeMap.events.add('ready', () => {
        try {
            const dataSource = new atlas.source.DataSource();
            routeMap.sources.add(dataSource);

            if (routeGeoJson) {
                try {
                    const routeFeature = JSON.parse(routeGeoJson);
                    if (routeFeature?.geometry?.type === 'LineString' && routeFeature?.geometry?.coordinates?.length >= 2) {
                        dataSource.add(routeFeature);
                        routeMap.layers.add(new atlas.layer.LineLayer(dataSource, null, {
                            strokeColor: '#0066b2',
                            strokeWidth: 4,
                            strokeOpacity: 0.8,
                            filter: ['==', ['geometry-type'], 'LineString']
                        }));
                    } else {
                        console.warn('[DirectionsModal] Route GeoJSON missing usable LineString geometry');
                    }
                } catch (error) {
                    console.error('[DirectionsModal] Failed to parse RouteGeoJson for mini-map:', error);
                }
            }

            const hasOrigin = Array.isArray(origin) && origin.length === 2;
            const hasDestination = Array.isArray(destination) && destination.length === 2;
            if (hasOrigin && hasDestination) {
                dataSource.add(new atlas.data.Feature(new atlas.data.Point(origin), { pointType: 'origin' }));
                dataSource.add(new atlas.data.Feature(new atlas.data.Point(destination), { pointType: 'destination' }));

                routeMap.layers.add(new atlas.layer.SymbolLayer(dataSource, null, {
                    iconOptions: { image: 'pin-blue', anchor: 'center', allowOverlap: true },
                    filter: ['==', ['get', 'pointType'], 'origin']
                }));

                routeMap.layers.add(new atlas.layer.SymbolLayer(dataSource, null, {
                    iconOptions: { image: 'pin-red', anchor: 'center', allowOverlap: true },
                    filter: ['==', ['get', 'pointType'], 'destination']
                }));

                let boundsData = [
                    new atlas.data.Point(origin),
                    new atlas.data.Point(destination)
                ];
                if (routeGeoJson) {
                    try {
                        const rf = JSON.parse(routeGeoJson);
                        if (rf?.geometry?.coordinates?.length >= 2) {
                            boundsData = rf.geometry.coordinates.map(c => new atlas.data.Point(c));
                        }
                    } catch { /* fall back to origin/destination */ }
                }
                const bounds = atlas.data.BoundingBox.fromData(boundsData);
                routeMap.setCamera({ bounds, padding: 40 });
            }

            // Ensure the map paints correctly in modal layouts after first frame.
            routeMap.resize();
            setTimeout(() => {
                if (routeMap) {
                    routeMap.resize();
                }
            }, 150);
        } catch (error) {
            console.error('[DirectionsModal] Error rendering route mini-map:', error);
        }
    });
}

/**
 * Disposes the route mini-map instance if one exists.
 */
export function disposeRouteMap() {
    if (routeMap) {
        routeMap.dispose();
        routeMap = null;
    }
}

/**
 * Gets an Azure AD access token for Azure Maps API calls
 * In production with Azure App Service, this uses the built-in authentication
 * @returns {Promise<string|null>} The access token or null if not available
 */
async function getAzureADToken() {
    try {
        // In Azure App Service with Easy Auth enabled, we can get the token from /.auth/me
        const response = await fetch('/.auth/me');

        if (!response.ok) {
            console.warn('[DirectionsModal] Could not fetch auth info from /.auth/me');
            return null;
        }

        const data = await response.json();

        if (data?.[0]?.access_token) {
            return data[0].access_token;
        }

        console.warn('[DirectionsModal] No access token found in auth response');
        return null;
    } catch (error) {
        console.error('[DirectionsModal] Error getting Azure AD token:', error);
        return null;
    }
}
