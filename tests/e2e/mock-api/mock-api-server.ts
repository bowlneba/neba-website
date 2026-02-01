/**
 * Mock API Server for E2E Tests
 *
 * This server provides mock responses for the NEBA API endpoints,
 * allowing E2E tests to run without the full Aspire stack.
 *
 * Add new routes to the `routes` object as the API grows.
 */
import { createServer, IncomingMessage, ServerResponse } from 'node:http';

const MOCK_WEATHER_FORECAST = {
  items: [
    { date: '2024-01-15', temperatureC: 20, temperatureF: 68, summary: 'Mild' },
    { date: '2024-01-16', temperatureC: 25, temperatureF: 77, summary: 'Warm' },
    { date: '2024-01-17', temperatureC: 15, temperatureF: 59, summary: 'Cool' },
    { date: '2024-01-18', temperatureC: 30, temperatureF: 86, summary: 'Hot' },
    { date: '2024-01-19', temperatureC: 10, temperatureF: 50, summary: 'Chilly' },
  ],
  count: 5,
};

function setCorsHeaders(res: ServerResponse): void {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
}

function sendJsonResponse(res: ServerResponse, data: unknown, statusCode = 200): void {
  res.writeHead(statusCode, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify(data));
}

const routes: Record<string, unknown> = {
  '/health': { status: 'healthy' },
  '/weatherforecast': MOCK_WEATHER_FORECAST,
  // Add more routes as the API grows:
  // '/tournaments': { items: [], count: 0 },
  // '/bowling-centers': { items: [], count: 0 },
};

function handleRequest(req: IncomingMessage, res: ServerResponse): void {
  setCorsHeaders(res);

  if (req.method === 'OPTIONS') {
    res.writeHead(200);
    res.end();
    return;
  }

  if (req.method === 'GET') {
    const data = routes[req.url || ''];
    if (data) {
      sendJsonResponse(res, data);
      return;
    }
  }

  sendJsonResponse(res, { error: 'Not Found' }, 404);
}

function closeServer(server: ReturnType<typeof createServer>): Promise<void> {
  return new Promise((resolve) => {
    server.close(() => {
      console.log('Mock API server closed');
      resolve();
    });
  });
}

export function startMockApiServer(port = 5151): Promise<{ close: () => Promise<void> }> {
  return new Promise((resolve) => {
    const server = createServer(handleRequest);

    server.listen(port, () => {
      console.log(`Mock API server listening on http://localhost:${port}`);
      resolve({
        close: () => closeServer(server),
      });
    });
  });
}
