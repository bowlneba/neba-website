/**
 * Mock data factories for E2E tests
 *
 * These factories generate test data that matches the API contract types.
 * Use these when mocking API responses in tests.
 */

/**
 * Document response matching GetDocumentResponse
 */
export interface DocumentResponse {
  html: string;
}

/**
 * Generate a mock document response
 */
export function createDocumentResponse(overrides: Partial<DocumentResponse> = {}): DocumentResponse {
  return {
    html: '<h1>Test Document</h1><h2>Section 1</h2><p>Test content.</p>',
    ...overrides,
  };
}

export function createTournamentRulesResponse(): DocumentResponse {
  return createDocumentResponse({
    html: '<h1>NEBA Tournament Rules</h1><h2>Section 1: Eligibility</h2><p>Test content.</p>',
  });
}

export function createBylawsResponse(): DocumentResponse {
  return createDocumentResponse({
    html: '<h1>NEBA Bylaws</h1><h2>Article I: Name</h2><p>Test content.</p>',
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
