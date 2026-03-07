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

export interface PhoneNumberResponse {
  phoneNumberType: string;
  phoneNumber: string;
}

export interface BowlingCenterSummaryResponse {
  certificationNumber: string;
  name: string;
  status: string;
  street: string;
  unit?: string | null;
  city: string;
  state: string;
  postalCode: string;
  latitude: number;
  longitude: number;
  phoneNumbers: PhoneNumberResponse[];
}

export function createBowlingCenterResponse(
  overrides: Partial<BowlingCenterSummaryResponse> = {}
): BowlingCenterSummaryResponse {
  return {
    certificationNumber: '12345',
    name: 'Lucky Strike Lanes',
    status: 'Open',
    street: '100 Bowling Way',
    city: 'Boston',
    state: 'MA',
    postalCode: '02101',
    latitude: 42.3601,
    longitude: -71.0589,
    phoneNumbers: [{ phoneNumberType: 'Work', phoneNumber: '6175550100' }],
    ...overrides,
  };
}
