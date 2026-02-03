/**
 * Mock data factories for E2E tests
 *
 * These factories generate test data that matches the API contract types.
 * Use these when mocking API responses in tests.
 */

/**
 * Weather forecast response matching WeatherForecastResponse
 */
export interface WeatherForecast {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
}

const seededRandom = (seed: number) => {
  let t = seed;
  return () => {
    t += 0x6d2b79f5;
    let x = t;
    x = Math.imul(x ^ (x >>> 15), x | 1);
    x ^= x + Math.imul(x ^ (x >>> 7), x | 61);
    return ((x ^ (x >>> 14)) >>> 0) / 4294967296;
  };
};

/**
 * Generate mock weather forecast data
 */
export function createWeatherForecast(overrides: Partial<WeatherForecast> = {}): WeatherForecast {
  return {
    date: new Date().toISOString(),
    temperatureC: 20,
    temperatureF: 68,
    summary: 'Mild',
    ...overrides,
  };
}

/**
 * Generate multiple weather forecasts
 */
export function createWeatherForecasts(count: number): WeatherForecast[] {
  const summaries = ['Freezing', 'Bracing', 'Chilly', 'Cool', 'Mild', 'Warm', 'Balmy', 'Hot', 'Sweltering', 'Scorching'];
  const rng = seededRandom(0xc0ffee);

  return Array.from({ length: count }, (_, i) => {
    const date = new Date();
    date.setDate(date.getDate() + i);
    const tempC = Math.floor(rng() * 40) - 10;

    return createWeatherForecast({
      date: date.toISOString(),
      temperatureC: tempC,
      temperatureF: 32 + Math.floor(tempC * 1.8),
      summary: summaries[Math.floor(rng() * summaries.length)],
    });
  });
}

// Add more mock data factories as your API grows:
//
// export interface Tournament {
//   id: string;
//   name: string;
//   date: string;
//   location: string;
// }
//
// export function createTournament(overrides: Partial<Tournament> = {}): Tournament {
//   return {
//     id: crypto.randomUUID(),
//     name: 'Test Tournament',
//     date: new Date().toISOString(),
//     location: 'Test Bowling Center',
//     ...overrides,
//   };
// }
