import { test as base, Page, Route } from '@playwright/test';

/**
 * API route patterns for the NEBA API
 */
export const API_ROUTES = {
  // Add more browser-interceptable routes as needed (note: Blazor Server API calls
  // are made server-side and must be handled by the mock API server instead):
  // tournaments: '**/tournaments',
  // stats: '**/stats',
} as const;

/**
 * Mock response helper - creates a successful API response
 */
export function mockApiResponse<T>(data: T, status = 200) {
  return {
    status,
    contentType: 'application/json',
    body: JSON.stringify(data),
  };
}

/**
 * Collection response wrapper matching CollectionResponse<T>
 */
export function collectionResponse<T>(items: T[]) {
  return {
    items,
    count: items.length,
  };
}

/**
 * Error response helper
 */
export function mockErrorResponse(message: string, status = 500) {
  return {
    status,
    contentType: 'application/json',
    body: JSON.stringify({
      error: message,
      status,
    }),
  };
}

/**
 * API Mock Builder - fluent interface for setting up API mocks
 */
export class ApiMockBuilder {
  private readonly mocks: Map<string, (route: Route) => Promise<void>> = new Map();

  /**
   * Mock a successful response for a route
   */
  success<T>(pattern: string, data: T, status = 200): this {
    this.mocks.set(pattern, async (route) => {
      await route.fulfill(mockApiResponse(data, status));
    });
    return this;
  }

  /**
   * Mock an error response for a route
   */
  error(pattern: string, message: string, status = 500): this {
    this.mocks.set(pattern, async (route) => {
      await route.fulfill(mockErrorResponse(message, status));
    });
    return this;
  }

  /**
   * Mock a delayed response (for testing loading states)
   */
  delayed<T>(pattern: string, data: T, delayMs: number): this {
    this.mocks.set(pattern, async (route) => {
      await new Promise((resolve) => setTimeout(resolve, delayMs));
      await route.fulfill(mockApiResponse(data));
    });
    return this;
  }

  /**
   * Mock a network error
   */
  networkError(pattern: string): this {
    this.mocks.set(pattern, async (route) => {
      await route.abort('connectionfailed');
    });
    return this;
  }

  /**
   * Apply all mocks to a page
   */
  async applyTo(page: Page): Promise<void> {
    for (const [pattern, handler] of this.mocks) {
      await page.route(pattern, handler);
    }
  }
}

/**
 * Create a new API mock builder
 */
export function mockApi(): ApiMockBuilder {
  return new ApiMockBuilder();
}

/**
 * Extended test fixture with API mocking capabilities
 */
export const test = base.extend<{
  mockApi: ApiMockBuilder;
}>({
  mockApi: async ({ page }, use) => {
    const builder = new ApiMockBuilder();
    await use(builder);
    // Mocks are automatically cleaned up when page closes
  },
});

export { expect } from '@playwright/test';
