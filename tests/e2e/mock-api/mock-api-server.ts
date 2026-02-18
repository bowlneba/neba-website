/**
 * Mock API Server for E2E Tests
 *
 * This server provides mock responses for the NEBA API endpoints,
 * allowing E2E tests to run without the full Aspire stack.
 *
 * Add new routes to the `routes` object as the API grows.
 */
import { createServer, IncomingMessage, ServerResponse } from 'node:http';

const MOCK_TOURNAMENT_RULES_HTML = `
<h1>NEBA Tournament Rules</h1>
<h2>Section 1: Eligibility</h2>
<p>All participants must be registered NEBA members in good standing.
For membership requirements, see the <a href="/bylaws">NEBA Bylaws</a>.</p>
<h2>Section 2: Equipment Standards</h2>
<p>All bowling equipment must conform to USBC specifications.</p>
<h2>Section 3: Scoring</h2>
<p>Official scoring will follow standard USBC guidelines.</p>
`;

const MOCK_BYLAWS_HTML = `
<h1>NEBA Bylaws</h1>
<h2>Article I: Name</h2>
<p>This organization shall be known as the New England Bowling Association.</p>
<h2>Article II: Mission</h2>
<p>The mission of NEBA is to promote amateur bowling throughout New England.</p>
`;

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
  '/documents/tournament-rules': { html: MOCK_TOURNAMENT_RULES_HTML },
  '/documents/bylaws': { html: MOCK_BYLAWS_HTML },
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
