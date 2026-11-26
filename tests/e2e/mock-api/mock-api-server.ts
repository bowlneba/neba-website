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
<h2 id="rule-601-invitational-tournament-rules">Rule 601- Invitational Tournament Rules</h2>
<p>Invitational eligibility is by invitation only.</p>
<h2 id="rule-602-masters-tournament-rules">Rule 602- Masters Tournament Rules</h2>
<p>Masters qualifiers advance by average.</p>
<h2 id="rule-401-doubles-tournament-format">Rule 401 - Doubles Tournament Format</h2>
<h4 id="h.401a">401a. Team Composition</h4>
<p>A doubles team may have at most one champion.</p>
<h4 id="h.401b">401b. Champion Definition</h4>
<p>A champion is a bowler who won a NEBA title in the last ten years.</p>
<h4 id="h.401c">401c. Re-Entries</h4>
<p>Re-entries are permitted.</p>
`;

const MOCK_BYLAWS_HTML = `
<h1>NEBA Bylaws</h1>
<h2>Article I: Name</h2>
<p>This organization shall be known as the New England Bowlers' Association.</p>
<h2>Article II: Mission</h2>
<p>The mission of NEBA is to promote amateur bowling throughout New England.</p>
`;

function setCorsHeaders(res: ServerResponse): void {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, PUT, DELETE, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
}

function readRequestBody(req: IncomingMessage): Promise<string> {
  return new Promise((resolve, reject) => {
    let body = '';
    req.on('data', (chunk: string | Uint8Array) => { body += chunk; });
    req.on('end', () => resolve(body));
    req.on('error', reject);
  });
}

// An unsigned JWT with a far-future "exp", so the website's token-expiry check treats it as fresh.
function mockJwt(): string {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({ sub: '01JX0000000000000000000001', exp: 4102444800 })}.mock`;
}

function sendJsonResponse(res: ServerResponse, data: unknown, statusCode = 200): void {
  res.writeHead(statusCode, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify(data));
}

// Sends the mocked error response for a route registered via /__mock/fail and returns
// true if it did, so the caller can bail out of its own handling.
function sendMockOverrideErrorIfSet(res: ServerResponse, pathname: string): boolean {
  const override = mockOverrides.get(pathname);

  if (override?.status != null && override.status >= 400) {
    // With a detail, answer in the same ProblemDetails shape the real API uses, so the website's
    // ApiExecutor can show the message. Without one, a bare body (the website shows its generic text).
    const body = override.detail
      ? { status: override.status, errors: [{ name: 'generalErrors', reason: override.detail }] }
      : { error: 'Mock error' };
    sendJsonResponse(res, body, override.status);
    return true;
  }

  return false;
}

interface TournamentSponsorFixture {
  sponsorId: string;
  name: string;
  slug: string;
  logoUrl: string | null;
  websiteUrl: string | null;
  tagPhrase: string | null;
  titleSponsor: boolean;
  sponsorshipAmount: number;
}

interface SponsorMetaFixture {
  name: string;
  slug: string;
  logoUrl: string | null;
  websiteUrl: string | null;
  tagPhrase: string | null;
}

function slugify(title: string): string {
  return title
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-/, '')
    .replace(/-$/, '');
}

const MOCK_BOWLING_CENTERS = {
  items: [
    {
      certificationNumber: '12345',
      name: 'Lucky Strike Lanes',
      status: 'Open',
      street: '100 Bowling Way',
      unit: null,
      city: 'Boston',
      state: 'MA',
      postalCode: '02101',
      latitude: 42.3601,
      longitude: -71.0589,
      phoneNumbers: [{ phoneNumberType: 'Work', phoneNumber: '6175550100' }],
    },
    {
      certificationNumber: '67890',
      name: 'Strike Zone',
      status: 'Open',
      street: '200 Pin Street',
      unit: null,
      city: 'Cambridge',
      state: 'MA',
      postalCode: '02139',
      latitude: 42.3736,
      longitude: -71.1097,
      phoneNumbers: [{ phoneNumberType: 'Work', phoneNumber: '6175550200' }],
    },
  ],
  totalItems: 2,
};

const MOCK_SPONSORS_ACTIVE = {
  items: [
    {
      sponsorId: '01JX0000000000000000000101',
      name: 'Acme Bowling Supply',
      slug: 'acme-bowling-supply',
      logoUrl: null,
      isCurrentSponsor: true,
      priority: 1,
      tier: 'Title Sponsor',
      category: 'Manufacturer',
      tagPhrase: 'Setting the standard since 1962',
      description: 'The premier supplier of bowling equipment and accessories across New England.',
      websiteUrl: 'https://example.com/acme',
      facebookUrl: null,
      instagramUrl: null,
    },
    {
      sponsorId: '01JX0000000000000000000001',
      name: 'Pro Shop Plus',
      slug: 'pro-shop-plus',
      logoUrl: null,
      isCurrentSponsor: true,
      priority: 1,
      tier: 'Premier',
      category: 'Pro Shop',
      tagPhrase: null,
      description: 'Your local pro shop for all bowling needs.',
      websiteUrl: null,
      facebookUrl: null,
      instagramUrl: null,
    },
    {
      sponsorId: '01JX0000000000000000000102',
      name: 'Regional Lanes',
      slug: 'regional-lanes',
      logoUrl: null,
      isCurrentSponsor: true,
      priority: 1,
      tier: 'Standard',
      category: 'Bowling Center',
      tagPhrase: null,
      description: null,
      websiteUrl: null,
      facebookUrl: null,
      instagramUrl: null,
    },
  ],
  totalItems: 3,
};

const MOCK_SPONSOR_PRO_SHOP_PLUS = {
  id: '01JX0000000000000000000001',
  name: 'Pro Shop Plus',
  slug: 'pro-shop-plus',
  isCurrentSponsor: true,
  priority: 1,
  tier: 'Premier',
  category: 'Pro Shop',
  logoUrl: null,
  websiteUrl: 'https://example.com/proshopplus',
  tagPhrase: 'Everything for the serious bowler',
  description: 'Your local pro shop for all bowling needs. We carry the latest equipment from all major manufacturers.',
  promotionalNotes: '10% discount for NEBA members on all merchandise.',
  liveReadText: 'Pro Shop Plus — where champions are made. Visit us at 123 Main Street.',
  facebookUrl: null,
  instagramUrl: 'https://instagram.com/proshopplus',
  businessStreet: '123 Main Street',
  businessUnit: null,
  businessCity: 'Boston',
  businessState: 'MA',
  businessPostalCode: '02101',
  businessCountry: 'US',
  businessEmailAddress: 'info@proshopplus.example.com',
  phoneNumbers: [{ phoneNumberType: 'Work', phoneNumber: '6175550123' }],
  sponsorContactName: null,
  sponsorContactEmailAddress: null,
  sponsorContactPhoneNumber: null,
  sponsorContactPhoneNumberType: null,
  tournamentsSponsored: [
    {
      tournamentId: '01JX0000000000000000000010', // MOCK_TOURNAMENT_ID
      name: 'NEBA Spring Classic',
      startDate: '2024-09-21',
      endDate: '2024-09-21',
      titleSponsor: true,
    },
  ],
};

// 'old-sponsor' deliberately has no mock route: it's inactive, and the real API returns
// not-found for callers without the Sponsors management permission (see SponsorDetail.razor).

export const PRIMARY_BOWLER_ID = '01JX1111111111111111111111';
export const SECONDARY_BOWLER_ID = '01JX2222222222222222222222';
export const MOCK_SEASON_ID = '01JX0000000000000000020001';
export const MOCK_TOURNAMENT_ID = '01JX0000000000000000000010';

// ...existing code...


// ── Series fixtures (Series.spec.ts) ─────────────────────────────────────────────────────────
// The Series pages and the Series tag, callout and Finale card on the tournament pages.
export const MOCK_SERIES_SEASON_ID = '01JX0000000000000000020002';
export const MOCK_SERIES_PAST_SEASON_ID = '01JX0000000000000000020003';
export const MOCK_SERIES_ID = '01JX0000000000000000000500'; // current Series with a Finale and a logo
export const MOCK_SERIES_NO_FINALE_ID = '01JX0000000000000000000501'; // past Series, no Finale, locked
export const MOCK_SERIES_LOGO_ID = '01JX0000000000000000000502'; // logo edit; one copy per project
export const MOCK_SERIES_NAME = 'Vincent Hall Memorial Series';
export const MOCK_SERIES_LOGO_URL = 'http://localhost:5151/mock-storage/series/logo/vincent-hall.png';
export const MOCK_SERIES_FINALE_DATE = '2030-09-28'; // a Saturday
export const MOCK_SERIES_QUALIFYING_STAGE_TOURNAMENT_ID = '01JX0000000000000000000300';
export const MOCK_SERIES_PLACEMENT_STAGE_TOURNAMENT_ID = '01JX0000000000000000000304';
export const MOCK_PLAIN_TOURNAMENT_NAME = 'NEBA Spring Classic';

function isoDate(offsetDays: number): string {
  const date = new Date();
  date.setDate(date.getDate() + offsetDays);
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

// Name, start date, kind, advancing count, team size, squad date.
const SERIES_STAGE_ROWS: [string, string, string, number | null, number, string | null][] = [
  ["Callahan's Bowl-O-Rama", '2025-01-24', 'Qualifying', 8, 1, null],
  ['Trios Team Event', '2025-02-21', 'Qualifying', 2, 3, null],
  ['NEBA Doubles, Baker Finals', '2025-02-22', 'Qualifying', 4, 2, null],
  ['Sparetime', '2025-03-22', 'Qualifying', 8, 1, null],
  ['BuddiesProShop.com Open', '2025-04-26', 'Qualifying', 8, 1, null],
  ['Westgate Lanes', '2025-05-31', 'Qualifying', 8, 1, null],
  ['Over 50 / Under Baker Doubles', '2025-06-13', 'Qualifying', 4, 2, null],
  ['Singles, Cranston', '2025-06-14', 'Qualifying', 8, 1, null],
  ["Callahan's Bowl-O-Rama Survivor", '2025-07-12', 'Survivor', 40, 1, '2025-07-12'],
  ["Lang's Singles Open", '2025-08-23', 'Survivor', 20, 1, '2025-08-23'],
  ['East Providence Singles', '2025-09-13', 'Placement', null, 1, null],
];

const TEAM_SIZE_TYPES: Record<number, string> = { 1: 'Singles', 2: 'Doubles' };

function mockSeriesStages(rows = SERIES_STAGE_ROWS): object[] {
  return rows.map(([name, start, kind, count, teamSize, squad], index) => ({
    id: `01JX00000000000000000006${String(index).padStart(2, '0')}`,
    order: index + 1,
    kind,
    status: 'Completed',
    advancingCount: count,
    squadDate: squad,
    squadStartTime: squad ? '09:00:00' : null,
    tournamentId: `01JX00000000000000000007${String(index).padStart(2, '0')}`,
    tournamentName: name,
    tournamentStartDate: start,
    tournamentEndDate: start,
    tournamentType: TEAM_SIZE_TYPES[teamSize] ?? 'Trios',
    teamSize,
  }));
}

const MOCK_SERIES_FINALE = {
  finalistCount: 4,
  date: MOCK_SERIES_FINALE_DATE,
  startTime: '14:00:00',
  bowlingCenterId: '12345',
  bowlingCenterName: 'Lucky Strike Lanes',
  bowlingCenterCity: 'Boston',
  bowlingCenterState: 'MA',
};

// A Series' logo after a PUT /series/{id}/logo; a missing key means the fixture's own logo.
const seriesLogos = new Map<string, string | null>();

function mockSeriesDetail(id: string): Record<string, unknown> | null {
  const year = new Date().getFullYear();
  const logoFor = (own: string | null) => (seriesLogos.has(id) ? seriesLogos.get(id) : own);
  const isLogoSeries = E2E_PROJECT_NAMES.some((projectName) => id === perProjectTournamentId(MOCK_SERIES_LOGO_ID, projectName));

  const advancementBaseId = advFixtureBaseId(id);
  if (advancementBaseId) return mockAdvancementSeriesDetail(id, advancementBaseId);

  if (id === MOCK_SERIES_ID) {
    return {
      id,
      name: MOCK_SERIES_NAME,
      seasonId: MOCK_SERIES_SEASON_ID,
      seasonDescription: `${year} Season`,
      seasonStartDate: `${year}-01-01`,
      seasonEndDate: `${year + 1}-12-31`,
      isLocked: false,
      finale: MOCK_SERIES_FINALE,
      logoUrl: logoFor(MOCK_SERIES_LOGO_URL),
      stages: mockSeriesStages(),
    };
  }

  if (isLogoSeries) {
    return {
      id,
      name: 'Logo Edit Series',
      seasonId: MOCK_SERIES_SEASON_ID,
      seasonDescription: `${year} Season`,
      seasonStartDate: `${year}-01-01`,
      seasonEndDate: `${year + 1}-12-31`,
      isLocked: false,
      finale: null,
      logoUrl: logoFor(null),
      stages: mockSeriesStages(SERIES_STAGE_ROWS.slice(0, 3)),
    };
  }

  if (id === MOCK_SERIES_NO_FINALE_ID) {
    return {
      id,
      name: '2024 Vincent Hall Memorial Series',
      seasonId: MOCK_SERIES_PAST_SEASON_ID,
      seasonDescription: '2024 Season',
      seasonStartDate: '2024-01-01',
      seasonEndDate: '2024-12-31',
      isLocked: true,
      finale: null,
      logoUrl: null,
      stages: mockSeriesStages([SERIES_STAGE_ROWS[0], SERIES_STAGE_ROWS[5], SERIES_STAGE_ROWS[8], SERIES_STAGE_ROWS[10]]),
    };
  }

  return null;
}

function mockSeriesSummary(detail: Record<string, unknown>): object {
  const stages = detail.stages as { kind: string; tournamentId: string }[];
  const count = (kind: string) => stages.filter((stage) => stage.kind === kind).length;
  return {
    id: detail.id,
    name: detail.name,
    seasonId: detail.seasonId,
    seasonDescription: detail.seasonDescription,
    seasonStartDate: detail.seasonStartDate,
    seasonEndDate: detail.seasonEndDate,
    qualifyingStageCount: count('Qualifying'),
    survivorStageCount: count('Survivor'),
    placementStageCount: count('Placement'),
    isLocked: detail.isLocked,
    logoUrl: detail.logoUrl,
    stageTournamentIds: stages.map((stage) => stage.tournamentId),
    nextStage:
      detail.id === MOCK_SERIES_ID
        ? { kind: 'Qualifying', tournamentId: '01JX0000000000000000000705', tournamentName: 'Westgate Lanes', tournamentStartDate: '2025-05-31', tournamentEndDate: '2025-05-31' }
        : null,
  };
}

function mockSeriesFinales(): object {
  const finale = (date: string, withHost: boolean) => ({
    finalistCount: 4,
    date,
    startTime: '14:00:00',
    bowlingCenterId: withHost ? '12345' : null,
    bowlingCenterName: withHost ? 'Lucky Strike Lanes' : null,
    bowlingCenterCity: withHost ? 'Boston' : null,
    bowlingCenterState: withHost ? 'MA' : null,
  });

  return {
    items: [
      // Dates are relative to today so these stay upcoming and past whenever the suite runs.
      { seriesId: MOCK_SERIES_ID, seriesName: MOCK_SERIES_NAME, seasonId: MOCK_SEASON_ID, logoUrl: MOCK_SERIES_LOGO_URL, finale: finale(isoDate(30), true) },
      { seriesId: '01JX0000000000000000000510', seriesName: 'Fall Challenge Series', seasonId: MOCK_SEASON_ID, logoUrl: null, finale: finale(isoDate(60), false) },
      { seriesId: '01JX0000000000000000000511', seriesName: 'Winter Classic Series', seasonId: MOCK_SEASON_ID, logoUrl: null, finale: finale(isoDate(-10), true) },
    ],
    totalItems: 3,
  };
}

// The tournaments the Series editor offers for the Series season: one per Stage of the reference Series.
function mockSeriesSeasonTournaments(): object {
  return {
    items: SERIES_STAGE_ROWS.map(([name, start], index) =>
      seriesStageSeasonItem(`01JX00000000000000000007${String(index).padStart(2, '0')}`, name, start, null)
    ),
  };
}

function resolveSeriesRoute(pathname: string): object | null {
  if (pathname === '/series') {
    const details = [MOCK_SERIES_ID, MOCK_SERIES_NO_FINALE_ID].map((id) => mockSeriesDetail(id) as Record<string, unknown>);
    return { items: details.map(mockSeriesSummary), totalItems: details.length };
  }
  if (pathname === '/series/finales') return mockSeriesFinales();
  if (pathname.startsWith('/series/')) return mockSeriesDetail(pathname.slice('/series/'.length));
  return null;
}

// ── Series advancement fixtures (GET /series/{id}/advancement and its writes) ────────────────
// Seven fixture Series show the states the advancement UI has. The three Playwright projects share one
// mock server and the writes change the fixture, so each project uses its own copy of an id
// (perProjectAdvancementSeriesId); the base id works too. State is built on first use and POST
// /__mock/series-advancement/reset?seriesId= (no seriesId: every Series) restores the fixtures.
// None of these ids is in the /series list.
export const MOCK_ADVANCEMENT_PENDING_ID = '01JX0000000000000000000520'; // every Stage Pending
export const MOCK_ADVANCEMENT_MID_SEASON_ID = '01JX0000000000000000000521'; // Computed Stale Qualifying, Survivor needs a squad, Placement Pending
export const MOCK_ADVANCEMENT_COMPLETE_ID = '01JX0000000000000000000522'; // Official Stages, Backfill ran, Placement Computed
export const MOCK_ADVANCEMENT_OVERRIDES_ID = '01JX0000000000000000000523'; // Official Qualifying, then Computed with an Add and a Clear
export const MOCK_ADVANCEMENT_SQUAD_ID = '01JX0000000000000000000524'; // Survivor Stage with two squads to pick
export const MOCK_ADVANCEMENT_CANCELLED_ID = '01JX0000000000000000000525'; // locked, one Pending Stage with a Cancelled tournament
export const MOCK_ADVANCEMENT_REVIEW_ID = '01JX0000000000000000000526'; // both Qualifying Stages Computed, Backfill ran: Stage 1 is ready to mark Official
export const MOCK_ADVANCEMENT_FINALE_ID = '01JX0000000000000000000527'; // Qualifying and Survivor Stages Official, Placement Computed and ready to mark Official, a bowler who already advanced
export const MOCK_ADVANCEMENT_ADMIN_ID = '01JX0000000000000000000001'; // the createdBy of the fixture overrides

// perProjectTournamentId keeps only the last digit of the base id, so 502 and 522 would collide: this one keeps all three.
export function perProjectAdvancementSeriesId(baseId: string, projectName: string): string {
  const index = E2E_PROJECT_NAMES.indexOf(projectName as (typeof E2E_PROJECT_NAMES)[number]);
  if (index < 0) {
    throw new Error(`Unknown Playwright project "${projectName}"; add it to E2E_PROJECT_NAMES.`);
  }

  return `${baseId.slice(0, -5)}Q${index}${baseId.slice(-3)}`;
}

interface AdvFieldEntry {
  bowlerId: string;
  bowlerName: string;
  sourceStageId?: string;
  place?: number;
  backfillPoints?: number;
  viaOverride: boolean;
}

interface AdvOverride {
  overrideId: string;
  bowlerId: string;
  bowlerName: string;
  kind: 'Add' | 'Clear';
  reason: string;
  createdBy: string;
  createdAt: string;
  applied: boolean;
}

interface AdvOutcome {
  bowlerId: string;
  bowlerName: string;
  blockTotal?: number;
  advanced: boolean;
}

interface AdvStage {
  stageId: string;
  order: number;
  kind: 'Qualifying' | 'Survivor' | 'Placement';
  status: 'Pending' | 'Computed' | 'Official';
  isStale: boolean;
  needsSquad: boolean;
  computedAt?: string;
  officialAt?: string;
  slotCount?: number;
  vacantSlotCount?: number;
  tournamentId: string;
  tournamentStatus: 'Scheduled' | 'Completed' | 'Cancelled';
  resolvedSquadId?: string;
  entries: AdvFieldEntry[];
  alreadyAdvanced: unknown[];
  overrides: AdvOverride[];
  survivorOutcomes: AdvOutcome[];
}

interface AdvPosition {
  bowlerId: string;
  bowlerName: string;
  position: number;
  bowled: boolean;
  isFinalist?: boolean;
}

interface AdvResultRow {
  bowlerId: string;
  bowlerName: string;
  place: number;
}

interface AdvSquad {
  squadId: string;
  date: string;
  startTime: string;
  hasScores: boolean;
}

interface AdvState {
  series3: string;
  version: number;
  backfillAsOf: string | null;
  stages: AdvStage[];
  positions: AdvPosition[];
  results: Record<string, AdvResultRow[]>;
  squads: Record<string, AdvSquad[]>;
  nextOverride: number;
}

const ADV_FIXTURE_NAMES: Record<string, string> = {
  [MOCK_ADVANCEMENT_PENDING_ID]: 'Advancement Pending Series',
  [MOCK_ADVANCEMENT_MID_SEASON_ID]: 'Advancement Mid-Season Series',
  [MOCK_ADVANCEMENT_COMPLETE_ID]: 'Advancement Complete Series',
  [MOCK_ADVANCEMENT_OVERRIDES_ID]: 'Advancement Overrides Series',
  [MOCK_ADVANCEMENT_SQUAD_ID]: 'Advancement Squad Series',
  [MOCK_ADVANCEMENT_CANCELLED_ID]: 'Advancement Cancelled Series',
  [MOCK_ADVANCEMENT_REVIEW_ID]: 'Advancement Review Series',
  [MOCK_ADVANCEMENT_FINALE_ID]: 'Advancement Finale Series',
};

// Name, start date, kind, advancing count, squad date. Every Stage is singles; a Qualifying Stage has 4 slots.
const ADV_STAGE_ROWS: [string, string, AdvStage['kind'], number | null, string | null][] = [
  ['Series Qualifier One', '2026-03-21', 'Qualifying', 4, null],
  ['Series Qualifier Two', '2026-04-18', 'Qualifying', 4, null],
  ['Series Survivor Round', '2026-07-12', 'Survivor', 4, '2026-07-12'],
  ['Series Placement Round', '2026-08-23', 'Placement', null, null],
];

const ADV_SLOTS = 4;
const ADV_COMPUTED_AT = '2026-03-23T18:00:00+00:00';
const ADV_OFFICIAL_AT = '2026-03-24T18:15:00+00:00';
const ADV_BACKFILL_AS_OF = '2026-04-18';

const ADV_BOWLER_NAMES = [
  'Marcus Alvarez', 'Dana Brennan', 'Priya Castellano', 'Tom Doyle', 'Elena Ferreira', 'Jorge Gallagher', 'Ruth Hartwell',
  'Kenji Iverson', 'Sam Jankowski', 'Alicia Kowalczyk', 'Nate Lombardi', 'Maria Mercer', 'Owen Nakamura', 'Leah Okafor',
  'Victor Pelletier', 'Hannah Quinlan', 'Ray Rowe', 'Tessa Sorensen', 'Ian Tran', 'Carla Underhill',
];

function advId(tail: string): string {
  return `01JX${tail.padStart(22, '0')}`;
}

function advBowler(index: number): { bowlerId: string; bowlerName: string } {
  return { bowlerId: advId(`4${String(index).padStart(4, '0')}`), bowlerName: ADV_BOWLER_NAMES[index % ADV_BOWLER_NAMES.length] };
}

function advSquadId(series3: string, order: number, letter: 'A' | 'B'): string {
  return advId(`7${series3}${String(order).padStart(2, '0')}${letter === 'A' ? 1 : 2}`);
}

function advPendingState(series3: string): AdvState {
  const stages: AdvStage[] = ADV_STAGE_ROWS.map(([, , kind], index) => ({
    stageId: advId(`8${series3}${String(index + 1).padStart(2, '0')}`),
    order: index + 1,
    kind,
    status: 'Pending',
    isStale: false,
    needsSquad: false,
    tournamentId: advId(`9${series3}${String(index + 1).padStart(2, '0')}`),
    tournamentStatus: 'Scheduled',
    entries: [],
    alreadyAdvanced: [],
    overrides: [],
    survivorOutcomes: [],
  }));

  // The tournament's result rows: the slot finishers (places 1 to 4), then six more the Add bowler picker offers.
  const resultRows = (first: number): AdvResultRow[] => [
    ...[0, 1, 2, 3].map((k) => ({ ...advBowler(first + k), place: k + 1 })),
    ...[10, 11, 12, 13, 14, 15].map((b, k) => ({ ...advBowler(b), place: ADV_SLOTS + k + 1 })),
  ];

  const results: Record<string, AdvResultRow[]> = {
    [stages[0].stageId]: resultRows(0),
    [stages[1].stageId]: resultRows(4),
    [stages[2].stageId]: [0, 1, 2, 3, 4, 5, 6, 7].map((b, k) => ({ ...advBowler(b), place: k + 1 })),
    [stages[3].stageId]: [],
  };

  const squads: Record<string, AdvSquad[]> = {
    [stages[2].stageId]: [
      { squadId: advSquadId(series3, 3, 'A'), date: '2026-07-12', startTime: '09:30:00', hasScores: true },
      { squadId: advSquadId(series3, 3, 'B'), date: '2026-07-12', startTime: '13:00:00', hasScores: false },
    ],
  };

  return { series3, version: 0, backfillAsOf: null, stages, positions: [], results, squads, nextOverride: 1 };
}

// An all-Pending state built from a Series detail's own Stages (the fallback for every other Series).
function advFallbackState(detail: Record<string, unknown>): AdvState {
  const state = advPendingState('000');
  const detailStages = detail.stages as { id: string; order: number; kind: string; tournamentId: string }[];
  state.stages = detailStages.map((stage) => ({
    stageId: stage.id,
    order: stage.order,
    kind: stage.kind as AdvStage['kind'],
    status: 'Pending',
    isStale: false,
    needsSquad: false,
    tournamentId: stage.tournamentId,
    tournamentStatus: 'Scheduled',
    entries: [],
    alreadyAdvanced: [],
    overrides: [],
    survivorOutcomes: [],
  }));
  state.results = {};
  state.squads = {};
  return state;
}

// Mirrors the API: a Computed, non-stale Qualifying or Survivor Stage can be marked Official once every earlier one is Official
// and, for the last Qualifying Stage, the Backfill ran.
function advCanMakeOfficial(state: AdvState, stage: AdvStage): boolean {
  const lastQualifying = state.stages.findLast((s) => s.kind === 'Qualifying');
  const awaitsBackfill = stage === lastQualifying && state.backfillAsOf === null;
  const earlierReady = state.stages.every((s) => s.order >= stage.order || s.status === 'Official');
  return stage.status === 'Computed' && !stage.isStale && !awaitsBackfill && earlierReady;
}

function advSetComputed(_state: AdvState, stage: AdvStage, official: boolean): void {
  stage.status = official ? 'Official' : 'Computed';
  stage.tournamentStatus = 'Completed';
  stage.computedAt = ADV_COMPUTED_AT;
  stage.officialAt = official ? ADV_OFFICIAL_AT : undefined;
}

function advFillQualifying(state: AdvState, order: number, official: boolean, vacant = 0): void {
  const stage = state.stages[order - 1];
  const slotRows = state.results[stage.stageId].filter((row) => row.place <= ADV_SLOTS).slice(0, ADV_SLOTS - vacant);
  stage.entries = slotRows.map((row) => ({ ...row, sourceStageId: stage.stageId, viaOverride: false }));
  stage.slotCount = ADV_SLOTS;
  stage.vacantSlotCount = vacant;
  advSetComputed(state, stage, official);
}

function advAddBackfill(state: AdvState, bowlerIndex: number, points: number): void {
  const lastQualifying = state.stages.findLast((s) => s.kind === 'Qualifying') as AdvStage;
  lastQualifying.entries.push({ ...advBowler(bowlerIndex), backfillPoints: points, viaOverride: false });
  state.backfillAsOf = ADV_BACKFILL_AS_OF;
}

function advComputeSurvivor(state: AdvState, stage: AdvStage, official: boolean, squadId?: string): void {
  const field = state.stages.filter((s) => s.kind === 'Qualifying').flatMap((s) => s.entries);
  // Invented block totals; the last bowler bowled no full block.
  stage.survivorOutcomes = field.map((entry, k) => {
    const blockTotal = k === field.length - 1 ? undefined : 1480 - k * 11;
    return { bowlerId: entry.bowlerId, bowlerName: entry.bowlerName, blockTotal, advanced: blockTotal !== undefined && k < ADV_SLOTS };
  });
  stage.needsSquad = false;
  stage.resolvedSquadId = squadId ?? state.squads[stage.stageId]?.[0]?.squadId;
  advSetComputed(state, stage, official);
}

function advComputePlacement(state: AdvState, stage: AdvStage): void {
  const survivor = state.stages.find((s) => s.kind === 'Survivor') as AdvStage;
  state.positions = survivor.survivorOutcomes
    .filter((outcome) => outcome.advanced)
    .map((outcome, k) => ({ bowlerId: outcome.bowlerId, bowlerName: outcome.bowlerName, position: k + 1, bowled: k < 3, isFinalist: k < 2 }));
  stage.status = 'Computed';
  stage.tournamentStatus = 'Completed';
  stage.computedAt = ADV_COMPUTED_AT;
}

function advBuildFixture(baseId: string): AdvState {
  const state = advPendingState(baseId.slice(-3));
  const [q1, q2, survivor, placement] = state.stages;

  switch (baseId) {
    case MOCK_ADVANCEMENT_MID_SEASON_ID:
      advFillQualifying(state, 1, false);
      q1.isStale = true;
      survivor.needsSquad = true;
      state.version = 7;
      break;
    case MOCK_ADVANCEMENT_COMPLETE_ID:
      advFillQualifying(state, 1, true);
      advFillQualifying(state, 2, true, 1);
      advAddBackfill(state, 8, 520);
      advComputeSurvivor(state, survivor, true);
      advComputePlacement(state, placement);
      state.version = 31;
      break;
    case MOCK_ADVANCEMENT_OVERRIDES_ID: {
      advFillQualifying(state, 1, true);
      advFillQualifying(state, 2, false);
      // An Add (a result-row bowler outside the slots) and a Clear (a slot finisher).
      advApplyOverride(state, q2, 'Add', advBowler(10).bowlerId, 'Walk-in registration confirmed by the tournament director.');
      advApplyOverride(state, q2, 'Clear', advBowler(7).bowlerId, 'Membership lapsed before the event.');
      state.version = 12;
      break;
    }
    case MOCK_ADVANCEMENT_SQUAD_ID:
      advFillQualifying(state, 1, true);
      advFillQualifying(state, 2, true, 1);
      advAddBackfill(state, 8, 520);
      survivor.tournamentStatus = 'Completed';
      survivor.needsSquad = true;
      state.version = 20;
      break;
    case MOCK_ADVANCEMENT_REVIEW_ID:
      advFillQualifying(state, 1, false);
      advFillQualifying(state, 2, false, 1);
      advAddBackfill(state, 8, 520);
      state.version = 9;
      break;
    case MOCK_ADVANCEMENT_FINALE_ID: {
      advFillQualifying(state, 1, true);
      advFillQualifying(state, 2, true, 1);
      advAddBackfill(state, 8, 520);
      advComputeSurvivor(state, survivor, true);
      advComputePlacement(state, placement);
      // The first Stage's winner also places in the second Stage, so that row shows as already advanced.
      const winner = q1.entries[0];
      q2.alreadyAdvanced = [{ bowlerId: winner.bowlerId, bowlerName: winner.bowlerName, place: 1, earlierStageId: q1.stageId, earlierStageOrder: 1 }];
      state.version = 33;
      break;
    }
    case MOCK_ADVANCEMENT_CANCELLED_ID:
      advFillQualifying(state, 1, true);
      q2.tournamentStatus = 'Cancelled';
      state.version = 5;
      break;
    default:
      break;
  }

  return state;
}

// Applies an Add or a Clear to a Computed Qualifying Stage. Returns an error to send, or the new override.
function advApplyOverride(
  state: AdvState,
  stage: AdvStage,
  kind: 'Add' | 'Clear',
  bowlerId: string,
  reason: string
): AdvOverride | { status: number; name: string; reason: string } {
  const result = state.results[stage.stageId]?.find((row) => row.bowlerId === bowlerId);

  if (stage.overrides.some((o) => o.bowlerId === bowlerId)) {
    return { status: 409, name: 'Series.Override', reason: 'This bowler already has an override on this Stage.' };
  }

  if (kind === 'Add') {
    if (!result) return { status: 422, name: 'Series.Override', reason: 'This bowler has no result row in the Stage tournament.' };
    if (state.stages.some((s) => s.entries.some((e) => e.bowlerId === bowlerId))) {
      return { status: 409, name: 'Series.Override', reason: 'This bowler is already in the field.' };
    }
    stage.entries.push({ ...result, sourceStageId: stage.stageId, viaOverride: true });
  } else {
    const index = stage.entries.findIndex((e) => e.bowlerId === bowlerId && !e.viaOverride && e.sourceStageId === stage.stageId);
    if (index < 0) return { status: 422, name: 'Series.Override', reason: 'This bowler is not in a filled slot of this Stage.' };
    stage.entries.splice(index, 1);
    stage.vacantSlotCount = (stage.vacantSlotCount ?? 0) + 1;
  }

  const created: AdvOverride = {
    overrideId: advId(`6${state.series3}${String(state.nextOverride++).padStart(4, '0')}`),
    bowlerId,
    bowlerName: result?.bowlerName ?? advBowlerName(bowlerId),
    kind,
    reason,
    createdBy: MOCK_ADVANCEMENT_ADMIN_ID,
    createdAt: '2026-03-23T15:00:00+00:00',
    applied: true,
  };
  stage.overrides.push(created);
  state.version++;
  return created;
}

function advBowlerName(bowlerId: string): string {
  const index = Number.parseInt(bowlerId.slice(-4), 10);
  return ADV_BOWLER_NAMES[index % ADV_BOWLER_NAMES.length];
}

const advanceStates = new Map<string, AdvState>();

function advFixtureBaseId(id: string): string | null {
  for (const baseId of Object.keys(ADV_FIXTURE_NAMES)) {
    if (id === baseId || E2E_PROJECT_NAMES.some((projectName) => id === perProjectAdvancementSeriesId(baseId, projectName))) return baseId;
  }
  return null;
}

function getAdvancementState(id: string): AdvState | null {
  const existing = advanceStates.get(id);
  if (existing) return existing;

  const baseId = advFixtureBaseId(id);
  let state: AdvState | null = null;
  if (baseId) {
    state = advBuildFixture(baseId);
  } else {
    const detail = mockSeriesDetail(id);
    state = detail ? advFallbackState(detail) : null;
  }

  if (state) advanceStates.set(id, state);
  return state;
}

// The Series detail of a fixture. A Series is locked once any Stage leaves Pending.
function mockAdvancementSeriesDetail(id: string, baseId: string): Record<string, unknown> {
  const state = getAdvancementState(id) as AdvState;
  const year = new Date().getFullYear();
  return {
    id,
    name: ADV_FIXTURE_NAMES[baseId],
    seasonId: MOCK_SERIES_SEASON_ID,
    seasonDescription: `${year} Season`,
    seasonStartDate: `${year}-01-01`,
    seasonEndDate: `${year + 1}-12-31`,
    isLocked: state.stages.some((stage) => stage.status !== 'Pending'),
    finale: { ...MOCK_SERIES_FINALE, finalistCount: 2 },
    logoUrl: null,
    stages: state.stages.map((stage) => {
      const [name, start, kind, count, squad] = ADV_STAGE_ROWS[stage.order - 1];
      return {
        id: stage.stageId,
        order: stage.order,
        kind,
        status: stage.status, // the Stage's own advancement status, as the real GetSeries sends it
        advancingCount: count,
        squadDate: squad,
        squadStartTime: squad ? '09:30:00' : null,
        tournamentId: stage.tournamentId,
        tournamentName: name,
        tournamentStartDate: start,
        tournamentEndDate: start,
        tournamentType: 'Singles',
        teamSize: 1,
      };
    }),
  };
}

function mockAdvancementResponse(state: AdvState): object {
  const targetField = state.stages.reduce((sum, s) => sum + (s.kind === 'Qualifying' ? s.slotCount ?? ADV_SLOTS : 0), 0);

  return {
    version: state.version,
    backfillAsOf: state.backfillAsOf,
    targetField,
    stages: state.stages.map((s) => ({ ...s, canMakeOfficial: advCanMakeOfficial(state, s) })),
    positions: state.positions,
  };
}

// GET /series/{id}/advancers: Official Stages only. A Qualifying Stage lists its entries and the bowlers who already advanced,
// a Survivor Stage lists its full standings, and a Placement Stage lists every position.
function mockAdvancersResponse(state: AdvState): object {
  const stages = state.stages
    .filter((s) => s.status === 'Official')
    .map((s) => ({
      stageId: s.stageId,
      order: s.order,
      kind: s.kind,
      advancers: mockAdvancersOf(state, s),
    }))
    .filter((s) => s.advancers.length > 0);

  return { stages };
}

function mockAdvancersOf(state: AdvState, s: AdvStage): object[] {
  if (s.kind === 'Qualifying') {
    const entries = s.entries.map((e) => ({
      bowlerId: e.bowlerId,
      bowlerName: e.bowlerName,
      place: e.place ?? null,
      blockTotal: null,
      isBackfill: e.sourceStageId === undefined,
      advanced: true,
      alreadyAdvancedFromOrder: null,
      backfillPoints: e.backfillPoints ?? null,
    }));
    const already = (s.alreadyAdvanced as { bowlerId: string; bowlerName: string; place: number; earlierStageOrder: number }[]).map((a) => ({
      bowlerId: a.bowlerId,
      bowlerName: a.bowlerName,
      place: a.place,
      blockTotal: null,
      isBackfill: false,
      advanced: false,
      alreadyAdvancedFromOrder: a.earlierStageOrder,
      backfillPoints: null,
    }));
    // Placed bowlers by place, then the Bowler of the Year point list, high to low.
    return [...entries, ...already].sort((a, b) => (a.place ?? 999) - (b.place ?? 999) || (b.backfillPoints ?? 0) - (a.backfillPoints ?? 0));
  }

  if (s.kind === 'Survivor') {
    return s.survivorOutcomes.map((o) => ({
      bowlerId: o.bowlerId,
      bowlerName: o.bowlerName,
      place: null,
      blockTotal: o.blockTotal ?? null,
      isBackfill: false,
      advanced: o.advanced,
      alreadyAdvancedFromOrder: null,
    }));
  }

  return state.positions.map((p) => ({
    bowlerId: p.bowlerId,
    bowlerName: p.bowlerName,
    place: null,
    blockTotal: null,
    isBackfill: false,
    advanced: p.isFinalist === true,
    alreadyAdvancedFromOrder: null,
    position: p.position,
    bowled: p.bowled,
    isFinalist: p.isFinalist ?? null,
  }));
}

const ADV_PATH = /^\/series\/([^/]+)\/(advancement|advancers|stages\/([^/]+)\/(results|squads|recomputations|squad|overrides|official)(?:\/([^/]+))?)$/;

function resolveAdvancementRoute(pathname: string): object | null {
  const match = ADV_PATH.exec(pathname);
  if (!match) return null;

  const state = getAdvancementState(match[1]);
  if (!state) return null;

  if (match[2] === 'advancement') return mockAdvancementResponse(state);
  if (match[2] === 'advancers') return mockAdvancersResponse(state);

  const stage = state.stages.find((s) => s.stageId === match[3]);
  if (!stage) return null;

  if (match[4] === 'results') {
    const items = state.results[stage.stageId] ?? [];
    return { items, totalItems: items.length };
  }

  if (match[4] === 'squads') {
    const items = (state.squads[stage.stageId] ?? []).map((squad) => ({ ...squad, isSelected: squad.squadId === stage.resolvedSquadId }));
    return { items, totalItems: items.length };
  }

  return null;
}

function sendAdvancementError(res: ServerResponse, status: number, name: string, reason: string): void {
  sendJsonResponse(res, { status, title: 'One or more validation errors occurred.', errors: [{ name, reason }] }, status);
}

// Reads a request body property by its camelCase or PascalCase name.
function advBodyProp<T>(body: string, name: string): T | undefined {
  const parsed = JSON.parse(body || '{}') as Record<string, unknown>;
  const pascal = name.charAt(0).toUpperCase() + name.slice(1);
  return (parsed[name] ?? parsed[pascal]) as T | undefined;
}

function advField<T>(wrapper: Record<string, unknown> | undefined, name: string): T | undefined {
  if (!wrapper) return undefined;
  const pascal = name.charAt(0).toUpperCase() + name.slice(1);
  return (wrapper[name] ?? wrapper[pascal]) as T | undefined;
}

// The advancement write routes. Mirrors the real endpoints' codes: a stale Version is 409, a refused
// Recompute is 409 with one failure per Official Stage, a bad request is 422. Returns true when it handled the request.
async function handleAdvancementWrite(req: IncomingMessage, res: ServerResponse, pathname: string, requestUrl: URL): Promise<boolean> {
  const match = ADV_PATH.exec(pathname);
  if (!match || match[2] === 'advancement' || match[2] === 'advancers') return false;

  const action = match[4];
  const overrideId = match[5];
  if (!isAdvancementWriteRoute(req.method, action, !!overrideId)) return false;

  if (sendMockOverrideErrorIfSet(res, pathname)) return true;

  const state = getAdvancementState(match[1]);
  const stage = state?.stages.find((s) => s.stageId === match[3]);
  if (!state || !stage) {
    sendJsonResponse(res, { error: 'Not Found' }, 404);
    return true;
  }

  if (req.method === 'DELETE') {
    lastRequestBodies.set('DELETE ' + pathname, JSON.stringify({ version: Number(requestUrl.searchParams.get('version')) }));
    handleOverrideDelete(res, state, stage, overrideId, Number(requestUrl.searchParams.get('version')));
    return true;
  }

  const body = await readRequestBody(req);
  lastRequestBodies.set(req.method + ' ' + pathname, body);

  if (action === 'overrides') handleOverrideCreate(res, state, stage, body);
  else if (action === 'squad') handleSquadReplace(res, state, stage, body);
  else if (action === 'official') handleMakeOfficial(res, state, stage, body);
  else handleRecompute(res, state, stage, body);
  return true;
}

function isAdvancementWriteRoute(method: string | undefined, action: string, hasOverrideId: boolean): boolean {
  if (method === 'POST') return (action === 'recomputations' || action === 'overrides') && !hasOverrideId;
  if (method === 'PUT') return (action === 'squad' || action === 'official') && !hasOverrideId;
  return method === 'DELETE' && action === 'overrides' && hasOverrideId;
}

function sendAdvancementStale(res: ServerResponse): void {
  sendAdvancementError(res, 409, 'Series.Version', 'The Series changed since you loaded it. Reload and try again.');
}

function sendNoContent(res: ServerResponse): void {
  res.writeHead(204);
  res.end();
}

function handleOverrideDelete(res: ServerResponse, state: AdvState, stage: AdvStage, overrideId: string, version: number): void {
  const index = stage.overrides.findIndex((o) => o.overrideId === overrideId);
  // A missing override is 204 before the Version check (delete is idempotent).
  if (index < 0) return sendNoContent(res);
  if (version !== state.version) return sendAdvancementStale(res);
  if (stage.status !== 'Computed') {
    return sendAdvancementError(res, 409, 'Series.Stage', 'The Stage is not Computed, so its overrides cannot change.');
  }

  const [removed] = stage.overrides.splice(index, 1);
  if (removed.kind === 'Add') {
    stage.entries = stage.entries.filter((e) => !(e.bowlerId === removed.bowlerId && e.viaOverride));
  } else {
    const row = state.results[stage.stageId]?.find((r) => r.bowlerId === removed.bowlerId);
    stage.entries.push({ bowlerId: removed.bowlerId, bowlerName: removed.bowlerName, sourceStageId: stage.stageId, place: row?.place, viaOverride: false });
    stage.entries.sort((a, b) => (a.place ?? 999) - (b.place ?? 999));
    stage.vacantSlotCount = Math.max(0, (stage.vacantSlotCount ?? 0) - 1);
  }
  state.version++;
  sendNoContent(res);
}

// The first rule an override request breaks, or null when it is valid.
function overrideRequestError(stage: AdvStage, reason: string): { status: number; name: string; reason: string } | null {
  if (!reason) return { status: 422, name: 'StageOverride.Reason', reason: 'A reason is required.' };
  if (reason.length > 500) return { status: 422, name: 'StageOverride.Reason', reason: 'The reason must be 500 characters or fewer.' };
  if (stage.kind !== 'Qualifying') return { status: 422, name: 'Series.Stage', reason: 'Only a Qualifying Stage takes an override.' };
  if (stage.status !== 'Computed') return { status: 409, name: 'Series.Stage', reason: 'The Stage is not Computed, so it cannot take an override.' };
  return null;
}

function handleOverrideCreate(res: ServerResponse, state: AdvState, stage: AdvStage, body: string): void {
  const input = advBodyProp<Record<string, unknown>>(body, 'stageOverride');
  const kind = advField<number>(input, 'kind');
  const bowlerId = advField<string>(input, 'bowlerId') ?? '';
  const reason = (advField<string>(input, 'reason') ?? '').trim();

  if (advField<number>(input, 'version') !== state.version) return sendAdvancementStale(res);

  const invalid = overrideRequestError(stage, reason);
  if (invalid) return sendAdvancementError(res, invalid.status, invalid.name, invalid.reason);

  const created = advApplyOverride(state, stage, kind === OVERRIDE_KIND_CLEAR ? 'Clear' : 'Add', bowlerId, reason);
  if ('status' in created) return sendAdvancementError(res, created.status, created.name, created.reason);

  sendJsonResponse(res, { overrideId: created.overrideId, version: state.version }, 201);
}

function handleSquadReplace(res: ServerResponse, state: AdvState, stage: AdvStage, body: string): void {
  const input = advBodyProp<Record<string, unknown>>(body, 'squad');
  const squadId = advField<string>(input, 'squadId');

  if (advField<number>(input, 'version') !== state.version) return sendAdvancementStale(res);
  if (stage.kind !== 'Survivor') {
    return sendAdvancementError(res, 422, 'Series.Stage', 'Only a Survivor Stage takes a squad.');
  }
  if (!state.squads[stage.stageId]?.some((s) => s.squadId === squadId)) {
    return sendAdvancementError(res, 422, 'Squad.SquadId', 'The tournament has no such squad.');
  }

  const previous = state.stages[stage.order - 2];
  if (stage.tournamentStatus === 'Completed' && previous && previous.status !== 'Pending') {
    advComputeSurvivor(state, stage, false, squadId);
  } else {
    // The Stage cannot compute yet: only the squad is chosen.
    stage.resolvedSquadId = squadId;
    stage.needsSquad = false;
  }
  stage.isStale = false;
  state.version++;
  sendNoContent(res);
}

function handleRecompute(res: ServerResponse, state: AdvState, stage: AdvStage, body: string): void {
  const input = advBodyProp<Record<string, unknown>>(body, 'recomputation');
  if (advField<number>(input, 'version') !== state.version) return sendAdvancementStale(res);

  const inRange = state.stages.filter((s) => s.order >= stage.order);
  const official = inRange.filter((s) => s.status === 'Official');
  if (official.length > 0 && advField<boolean>(input, 'confirmResetOfficial') !== true) {
    return sendJsonResponse(res, {
      status: 409,
      title: 'One or more validation errors occurred.',
      errors: official.map((s) => ({ name: `Series.Stages[${s.order - 1}]`, reason: 'Recomputing resets this Official Stage to Computed.' })),
    }, 409);
  }

  let changed = false;
  for (const target of inRange) {
    if (target.status === 'Official') {
      target.status = 'Computed';
      target.officialAt = undefined;
      changed = true;
    }
    if (target.isStale) {
      target.isStale = false;
      changed = true;
    }
  }
  // A no-op recompute writes nothing, so Version stays.
  if (changed) state.version++;
  sendNoContent(res);
}

// PUT /series/{id}/stages/{stageId}/official. A stale Version is 409, a Placement Stage is 422, a Stage that is not ready is 409,
// and a Stage that is already Official is a no-op 204.
function handleMakeOfficial(res: ServerResponse, state: AdvState, stage: AdvStage, body: string): void {
  const input = (JSON.parse(body || '{}') as Record<string, unknown>);
  const wrapper = (input.official ?? input.Official) as Record<string, unknown> | undefined;
  if (advField<number>(wrapper, 'version') !== state.version) return sendAdvancementStale(res);
  if (stage.status === 'Official') return sendNoContent(res);
  if (!advCanMakeOfficial(state, stage)) {
    return sendAdvancementError(res, 409, 'Series.Stage', 'This Stage is not ready to be made official.');
  }

  stage.status = 'Official';
  stage.officialAt = ADV_OFFICIAL_AT;
  state.version++;
  sendNoContent(res);
}

const OVERRIDE_KIND_CLEAR = 2;

// An edit of an advancement fixture drops the Stages the request no longer lists (the cancelled-Stage removal).
// Orders keep their numbers here: the fixture's tournament names are looked up by order.
function removeAdvancementStagesNotSubmitted(seriesId: string, body: string): void {
  const state = advFixtureBaseId(seriesId) ? advanceStates.get(seriesId) : undefined;
  if (!state) return;

  const submitted = (JSON.parse(body || '{}') as { series?: { stages?: { id?: string | null }[] } }).series?.stages;
  if (!submitted) return;

  const keep = new Set(submitted.map((stage) => stage.id));
  const kept = state.stages.filter((stage) => keep.has(stage.stageId));
  if (kept.length !== state.stages.length) {
    state.stages = kept;
    state.version++;
  }
}


const SERIES_TOURNAMENT_QUALIFYING = {
  seriesId: MOCK_SERIES_ID,
  name: MOCK_SERIES_NAME,
  logoUrl: null,
  kind: 'Qualifying',
  advancingCount: 8,
};

const SERIES_TOURNAMENT_PLACEMENT = {
  seriesId: MOCK_SERIES_ID,
  name: MOCK_SERIES_NAME,
  logoUrl: MOCK_SERIES_LOGO_URL,
  kind: 'Placement',
  finaleCount: 4,
  finaleDate: MOCK_SERIES_FINALE_DATE,
  finaleStartTime: '14:00:00',
};

function seriesStageSeasonItem(id: string, name: string, startDate: string, series: object | null): object {
  return {
    id,
    name,
    startDate,
    endDate: startDate,
    tournamentType: 'Singles',
    status: 'Completed',
    titleEligible: true,
    entryFee: 75,
    registrationUrl: null,
    addedMoney: null,
    reservations: null,
    patternLengthCategory: 'Medium',
    patternRatioCategory: null,
    logoUrl: null,
    winners: [],
    bowlingCenter: { name: 'Lucky Strike Lanes', city: 'Boston', state: 'MA' },
    sponsors: [],
    oilPatterns: [],
    ...(series ? { series } : {}),
  };
}

function seriesStageDetail(id: string, name: string, startDate: string, series: object | null): object {
  return {
    id,
    name,
    season: '2025-2026 Season',
    startDate,
    endDate: startDate,
    statsEligible: true,
    status: 'Completed',
    titleEligible: true,
    tournamentType: 'Singles',
    entryFee: 60,
    registrationUrl: null,
    addedMoney: null,
    reservations: null,
    entryCount: null,
    patternLengthCategory: null,
    patternRatioCategory: null,
    logoUrl: null,
    bowlingCenter: { name: 'Lucky Strike Lanes', city: 'Boston', state: 'MA' },
    sponsors: [],
    oilPatterns: [],
    winners: [],
    results: [],
    ...(series ? { series } : {}),
  };
}

export const MOCK_SEASON_TOURNAMENTS = {
  items: [
    {
      id: MOCK_TOURNAMENT_ID,
      name: 'NEBA Spring Classic',
      startDate: '2026-03-15',
      endDate: '2026-03-15',
      tournamentType: 'Singles',
      status: 'Completed',
      titleEligible: true,
      entryFee: 75,
      registrationUrl: null,
      addedMoney: 500,
      reservations: null,
      patternLengthCategory: 'Medium',
      patternRatioCategory: null,
      logoUrl: null,
      winners: ['Current Leader'],
      bowlingCenter: { name: 'Lucky Strike Lanes', city: 'Boston', state: 'MA' },
      sponsors: [],
      oilPatterns: [],
    },
    // Series Stage tournaments (tag on the schedule) and three plain ones the Series editor can pick.
    seriesStageSeasonItem(MOCK_SERIES_QUALIFYING_STAGE_TOURNAMENT_ID, 'NEBA Series Qualifier', '2026-04-04', SERIES_TOURNAMENT_QUALIFYING),
    seriesStageSeasonItem(MOCK_SERIES_PLACEMENT_STAGE_TOURNAMENT_ID, 'NEBA Series Placement', '2026-04-05', SERIES_TOURNAMENT_PLACEMENT),
    seriesStageSeasonItem('01JX0000000000000000000301', 'Series Pick One', '2026-04-11', null),
    seriesStageSeasonItem('01JX0000000000000000000302', 'Series Pick Two', '2026-04-18', null),
    seriesStageSeasonItem('01JX0000000000000000000303', 'Series Pick Three', '2026-04-25', null),
  ],
};

interface MockInduction {
  id: string;
  bowlerId: string;
  year: number;
  bowlerName: string;
  categories: string[];
  photoUri: string | null;
  photoFocalX: number | null;
  photoFocalY: number | null;
  photoZoom: number | null;
}

// Stateful: PUT /hall-of-fame/{id}/photo updates the matching item so the page shows the saved photo after it reloads.
// Bob and Alice start with photos, Jane and Tom do not, so each e2e test has an inductee of its own.
const MOCK_HALL_OF_FAME: { items: MockInduction[]; totalItems: number } = {
  items: [
    { id: '01JX0000000000000000000001', bowlerId: '01JX0000000000000000000201', year: 2024, bowlerName: 'Jane Smith', categories: ['Superior Performance'], photoUri: null, photoFocalX: null, photoFocalY: null, photoZoom: null },
    { id: '01JX0000000000000000000002', bowlerId: '01JX0000000000000000000202', year: 2024, bowlerName: 'Bob Johnson', categories: ['Meritorious Service'], photoUri: 'http://localhost:5151/mock-storage/hall-of-fame/photo/bob.png', photoFocalX: 20, photoFocalY: 50, photoZoom: 100 },
    { id: '01JX0000000000000000000003', bowlerId: '01JX0000000000000000000203', year: 2023, bowlerName: 'Alice Williams', categories: ['Friend of NEBA'], photoUri: 'http://localhost:5151/mock-storage/hall-of-fame/photo/alice.png', photoFocalX: 50, photoFocalY: 50, photoZoom: 100 },
    { id: '01JX0000000000000000000004', bowlerId: '01JX0000000000000000000204', year: 2023, bowlerName: 'Tom Davis', categories: ['Superior Performance', 'Meritorious Service'], photoUri: null, photoFocalX: null, photoFocalY: null, photoZoom: null },
  ],
  totalItems: 4,
};

const INITIAL_HALL_OF_FAME_ITEMS: MockInduction[] = structuredClone(MOCK_HALL_OF_FAME.items);

const MOCK_HALL_OF_FAME_POINTS = {
  rules: [
    { group: 'Bowler of the Year Awards', label: 'Bowler of the Year', points: 12 },
    { group: 'Tournament Titles', label: 'Singles Title', points: 6 },
    { group: 'Legacy Titles', label: 'NEBA High-Roller Title', points: 3 },
  ],
  requiredPoints: 36,
  requiredSeasonsWithPoints: 3,
  requiredMembershipYears: 10,
  membershipDataStartYear: 1999,
  standings: [
    { bowlerId: '01JX0000000000000000000101', bowlerName: 'Ray Dunleavy', totalPoints: 42, seasonsWithPoints: 5, membershipYears: 28, membershipHistoryComplete: false, eligible: true, meetsPoints: true, meetsSeasons: true, inductedCategories: [] },
    { bowlerId: '01JX0000000000000000000102', bowlerName: 'Ken Almeida', totalPoints: 42, seasonsWithPoints: 2, membershipYears: 15, membershipHistoryComplete: true, eligible: false, meetsPoints: true, meetsSeasons: false, inductedCategories: [] },
    { bowlerId: '01JX0000000000000000000103', bowlerName: 'Walt Brennan', totalPoints: 34, seasonsWithPoints: 6, membershipYears: 28, membershipHistoryComplete: false, eligible: false, meetsPoints: false, meetsSeasons: true, inductedCategories: ['Meritorious Service'] },
    { bowlerId: '01JX0000000000000000000104', bowlerName: 'José Marchetti', totalPoints: 12, seasonsWithPoints: 2, membershipYears: 8, membershipHistoryComplete: true, eligible: false, meetsPoints: false, meetsSeasons: false, inductedCategories: [] },
  ],
};

// Detail for inducted bowlers. 203 (Alice Williams) is missing on purpose: the detail route returns 404 for her.
const MOCK_INDUCTEE_DETAILS = new Map<string, object>([
  ['01JX0000000000000000000201', {
    bowlerId: '01JX0000000000000000000201',
    bowlerName: 'Jane Smith',
    inductedCategories: ['Superior Performance'],
    totalPoints: 43,
    seasonsWithPoints: 3,
    membershipHistoryComplete: true,
    breakdown: [
      { source: 'Bowler of the Year', date: '2003-12-31', points: 12, runningTotal: 12, reachedThreshold: false },
      { source: 'Singles Title', date: '2009-03-10', points: 6, runningTotal: 18, reachedThreshold: false },
      { source: 'NEBA Masters Title', date: '2014-09-11', points: 6, runningTotal: 24, reachedThreshold: false },
    ],
    membershipYears: [
      { year: 1999, membershipType: 'New' },
      { year: 2000, membershipType: 'Renewal' },
    ],
  }],
  ['01JX0000000000000000000202', {
    bowlerId: '01JX0000000000000000000202',
    bowlerName: 'Bob Johnson',
    inductedCategories: ['Meritorious Service'],
    totalPoints: 0,
    seasonsWithPoints: 0,
    membershipHistoryComplete: true,
    breakdown: [],
    membershipYears: [],
  }],
]);

const MOCK_HALL_OF_FAME_POINTS_DETAIL = (bowlerId: string) => {
  const standing = MOCK_HALL_OF_FAME_POINTS.standings.find((s) => s.bowlerId === bowlerId);
  if (!standing) return null;
  return {
    bowlerId: standing.bowlerId,
    bowlerName: standing.bowlerName,
    inductedCategories: standing.inductedCategories,
    totalPoints: standing.totalPoints,
    seasonsWithPoints: standing.seasonsWithPoints,
    membershipHistoryComplete: standing.membershipHistoryComplete,
    breakdown: [
      { source: 'Bowler of the Year', date: '2017-12-31', points: 12, runningTotal: 12, reachedThreshold: false },
      { source: 'Singles Title', date: '2019-03-10', points: 6, runningTotal: 18, reachedThreshold: false },
      { source: 'Doubles Title', date: '2021-05-02', points: 6, runningTotal: 24, reachedThreshold: false },
      { source: 'NEBA Masters Title', date: '2022-09-11', points: 6, runningTotal: 30, reachedThreshold: false },
      { source: 'Singles Title', date: '2023-03-05', points: 6, runningTotal: 36, reachedThreshold: true },
      { source: 'Trios Title', date: '2024-04-14', points: 2, runningTotal: 38, reachedThreshold: false },
    ],
    membershipYears: [
      { year: 2024, membershipType: 'Renewal' },
      { year: 2025, membershipType: 'Renewal' },
    ],
  };
};

// An 800x600 picture with a head-and-shoulders figure, so the focal point picker has a real image to measure.
const MOCK_HALL_OF_FAME_PHOTO_SVG = `<svg xmlns="http://www.w3.org/2000/svg" width="800" height="600" viewBox="0 0 800 600">
  <rect width="800" height="600" fill="#3E4FD9"/>
  <ellipse cx="560" cy="640" rx="190" ry="230" fill="#052767"/>
  <circle cx="560" cy="230" r="72" fill="#e8c4a0"/>
</svg>`;

const MOCK_BOWLER_OF_THE_YEAR_AWARDS = {
  items: [
    { season: '2024-2025', bowlerName: 'Current Leader', category: 'Open' },
    { season: '2024-2025', bowlerName: 'Jane Smith', category: 'Women' },
    { season: '2023-2024', bowlerName: 'Legacy Leader', category: 'Open' },
  ],
  totalItems: 3,
};

const MOCK_HIGH_AVERAGE_AWARDS = {
  items: [
    { season: '2024-2025', bowlerName: 'Current Leader', average: 228.42, totalGames: 35, tournamentsParticipated: 7 },
    { season: '2023-2024', bowlerName: 'Legacy Leader', average: 219.35, totalGames: 28, tournamentsParticipated: 6 },
  ],
  totalItems: 2,
};

const MOCK_HIGH_BLOCK_AWARDS = {
  items: [
    { season: '2024-2025', bowlerName: 'Current Leader', score: 1198 },
    { season: '2023-2024', bowlerName: 'Legacy Leader', score: 1120 },
  ],
  totalItems: 2,
};

// Includes 'Open', matching MOCK_TOURNAMENT_DETAIL.tournamentType — otherwise the edit form's
// <select> has no matching <option> and silently blanks the pre-filled value.
const MOCK_TOURNAMENT_TYPES = {
  items: [{ name: 'Singles' }, { name: 'Doubles' }, { name: 'Open' }],
};

const MOCK_OIL_PATTERNS = {
  items: [
    {
      oilPatternId: '01JX0000000000000000000201',
      name: 'Typhoon',
      length: 40,
      volume: 24,
      leftRatio: 5,
      rightRatio: 5,
      kegelId: null,
      lengthCategory: 'Medium',
      ratioCategory: 'Medium',
    },
  ],
};

export const MOCK_TOURNAMENT_DETAIL = {
  id: MOCK_TOURNAMENT_ID,
  name: 'NEBA Spring Classic',
  season: '2024-2025 Season',
  startDate: '2024-09-21',
  endDate: '2024-09-21',
  statsEligible: true,
  status: 'Completed',
  titleEligible: true,
  tournamentType: 'Open',
  entryFee: 75,
  registrationUrl: null,
  addedMoney: 500,
  reservations: null,
  entryCount: 48,
  patternLengthCategory: 'Medium',
  patternRatioCategory: null,
  logoUrl: null,
  bowlingCenter: { name: 'Lucky Strike Lanes', city: 'Boston', state: 'MA' },
  sponsors: [
    {
      sponsorId: '01JX0000000000000000000001',
      name: 'Pro Shop Plus',
      slug: 'pro-shop-plus',
      logoUrl: null,
      websiteUrl: null,
      tagPhrase: null,
      titleSponsor: true,
      sponsorshipAmount: 1000,
    },
    {
      sponsorId: '01JX0000000000000000000102',
      name: 'Regional Lanes',
      slug: 'regional-lanes',
      logoUrl: null,
      websiteUrl: null,
      tagPhrase: null,
      titleSponsor: false,
      sponsorshipAmount: 250,
    },
  ],
  oilPatterns: [{ oilPatternId: '01JX0000000000000000000040', name: 'Scorpion', length: 42, volume: 24.5, leftRatio: 3, rightRatio: 3 }],
  winners: ['Current Leader'],
  results: [],
};

// Fixtures for Oil Pattern Reveal gating E2E coverage (TournamentDetail.spec.ts). Each id
// simulates a different response shape the real API would return for a given viewer/reveal
// state — the mock server has no auth of its own, so the three states are modeled as three
// distinct tournaments rather than one tournament whose response varies by caller.
export const MOCK_TOURNAMENT_OIL_REVEAL_PENDING_ID = '01JX0000000000000000000030';
export const MOCK_TOURNAMENT_OIL_REVEALED_ID = '01JX0000000000000000000031';
export const MOCK_TOURNAMENT_OIL_REVEAL_MGMT_ID = '01JX0000000000000000000032';

// Ordinary viewer, reveal date/time still in the future: API withholds full pattern details and
// returns only the category chips.
const MOCK_TOURNAMENT_OIL_REVEAL_PENDING = {
  id: MOCK_TOURNAMENT_OIL_REVEAL_PENDING_ID,
  name: 'NEBA Pending Reveal Classic',
  season: '2025-2026 Season',
  startDate: '2026-06-01',
  endDate: '2026-06-01',
  statsEligible: true,
  status: 'Completed',
  titleEligible: true,
  tournamentType: 'Singles',
  entryFee: 60,
  registrationUrl: null,
  addedMoney: null,
  reservations: null,
  entryCount: null,
  patternLengthCategory: 'Medium',
  patternRatioCategory: null,
  logoUrl: null,
  bowlingCenter: { name: 'Lucky Strike Lanes', city: 'Boston', state: 'MA' },
  sponsors: [],
  oilPatterns: [],
  oilPatternRevealDateTime: '2030-01-01T00:00:00+00:00',
  winners: [],
  results: [],
};

// Reveal date/time already passed: full pattern details are public for every viewer.
const MOCK_TOURNAMENT_OIL_REVEALED = {
  id: MOCK_TOURNAMENT_OIL_REVEALED_ID,
  name: 'NEBA Revealed Pattern Classic',
  season: '2025-2026 Season',
  startDate: '2026-06-01',
  endDate: '2026-06-01',
  statsEligible: true,
  status: 'Completed',
  titleEligible: true,
  tournamentType: 'Singles',
  entryFee: 60,
  registrationUrl: null,
  addedMoney: null,
  reservations: null,
  entryCount: null,
  patternLengthCategory: 'Medium',
  patternRatioCategory: null,
  logoUrl: null,
  bowlingCenter: { name: 'Lucky Strike Lanes', city: 'Boston', state: 'MA' },
  sponsors: [],
  oilPatterns: [{ oilPatternId: '01JX0000000000000000000041', name: 'Scorpion', length: 42, volume: 24.5, leftRatio: 3, rightRatio: 3 }],
  oilPatternRevealDateTime: '2020-01-01T00:00:00+00:00',
  winners: [],
  results: [],
};

// Reveal date/time still in the future, but the caller has tournament-management permission, so
// the API returns full pattern details early alongside the still-pending reveal date/time.
const MOCK_TOURNAMENT_OIL_REVEAL_MGMT = {
  id: MOCK_TOURNAMENT_OIL_REVEAL_MGMT_ID,
  name: 'NEBA Management Preview Classic',
  season: '2025-2026 Season',
  startDate: '2026-06-01',
  endDate: '2026-06-01',
  statsEligible: true,
  status: 'Completed',
  titleEligible: true,
  tournamentType: 'Singles',
  entryFee: 60,
  registrationUrl: null,
  addedMoney: null,
  reservations: null,
  entryCount: null,
  patternLengthCategory: 'Medium',
  patternRatioCategory: null,
  logoUrl: null,
  bowlingCenter: { name: 'Lucky Strike Lanes', city: 'Boston', state: 'MA' },
  sponsors: [],
  oilPatterns: [{ oilPatternId: '01JX0000000000000000000042', name: 'Scorpion', length: 42, volume: 24.5, leftRatio: 3, rightRatio: 3 }],
  oilPatternRevealDateTime: '2030-01-01T00:00:00+00:00',
  winners: [],
  results: [],
};

// Dedicated tournament for the "Manage Sponsors" E2E flow (add/remove), kept separate from
// MOCK_TOURNAMENT_ID so those mutations never leak into the other tournament-detail tests that
// assert a fixed sponsor count against MOCK_TOURNAMENT_DETAIL.
export const MOCK_TOURNAMENT_SPONSOR_MGMT_ID = '01JX0000000000000000000040';

const MOCK_TOURNAMENT_SPONSOR_MGMT = {
  id: MOCK_TOURNAMENT_SPONSOR_MGMT_ID,
  name: 'NEBA Sponsor Management Classic',
  season: '2025-2026 Season',
  startDate: '2026-06-01',
  endDate: '2026-06-01',
  statsEligible: true,
  status: 'Completed',
  titleEligible: true,
  tournamentType: 'Singles',
  entryFee: 60,
  registrationUrl: null,
  addedMoney: null,
  reservations: null,
  entryCount: null,
  patternLengthCategory: null,
  patternRatioCategory: null,
  logoUrl: null,
  bowlingCenter: { name: 'Lucky Strike Lanes', city: 'Boston', state: 'MA' },
  sponsors: [] as TournamentSponsorFixture[],
  oilPatterns: [],
  winners: [],
  results: [],
};

// Dedicated tournament for the Truncate/Cancel status-action E2E flow, kept separate from
// MOCK_TOURNAMENT_DETAIL so mutating its status via the PATCH handlers below never leaks into
// the other tournament-detail tests that assert a fixed "Completed" status.
export const MOCK_TOURNAMENT_STATUS_ACTIONS_ID = '01JX0000000000000000000050';

const MOCK_TOURNAMENT_STATUS_ACTIONS = {
  id: MOCK_TOURNAMENT_STATUS_ACTIONS_ID,
  name: 'NEBA Status Actions Classic',
  season: '2025-2026 Season',
  startDate: '2026-06-01',
  endDate: '2026-06-01',
  statsEligible: true,
  status: 'Scheduled',
  titleEligible: false,
  tournamentType: 'Singles',
  entryFee: 60,
  registrationUrl: null,
  addedMoney: null,
  reservations: null,
  entryCount: null,
  patternLengthCategory: null,
  patternRatioCategory: null,
  logoUrl: null,
  bowlingCenter: { name: 'Lucky Strike Lanes', city: 'Boston', state: 'MA' },
  sponsors: [] as TournamentSponsorFixture[],
  oilPatterns: [],
  winners: [],
  results: [],
};

// A second, separate tournament for the Truncate happy-path test specifically — Truncate and
// Cancel each finalize their tournament (a one-way transition), so the Truncate happy-path test
// can't share MOCK_TOURNAMENT_STATUS_ACTIONS_ID with the Cancel happy-path test without one of
// them observing the other's finalized status.
export const MOCK_TOURNAMENT_TRUNCATE_ACTIONS_ID = '01JX0000000000000000000051';

const MOCK_TOURNAMENT_TRUNCATE_ACTIONS = {
  ...MOCK_TOURNAMENT_STATUS_ACTIONS,
  id: MOCK_TOURNAMENT_TRUNCATE_ACTIONS_ID,
  name: 'NEBA Truncate Actions Classic',
};

// Playwright runs every spec once per browser project, all against this one mock server. The
// status-action tests mutate their tournament (Cancel/Truncate are one-way) and set failure
// overrides by path, so projects running in parallel would see each other's changes. Each project
// therefore gets its own copy of those tournaments, with the same id except for a "P<n>" marker.
export const E2E_PROJECT_NAMES = ['chrome', 'mobile-chrome', 'mobile-safari'] as const;

export function perProjectTournamentId(baseId: string, projectName: string): string {
  const index = E2E_PROJECT_NAMES.indexOf(projectName as (typeof E2E_PROJECT_NAMES)[number]);
  if (index < 0) {
    throw new Error(`Unknown Playwright project "${projectName}"; add it to E2E_PROJECT_NAMES.`);
  }

  return `${baseId.slice(0, -3)}P${index}${baseId.slice(-1)}`;
}

const PER_PROJECT_STATUS_TOURNAMENTS: [string, object][] = E2E_PROJECT_NAMES.flatMap((projectName) => [
  [
    perProjectTournamentId(MOCK_TOURNAMENT_STATUS_ACTIONS_ID, projectName),
    {
      ...MOCK_TOURNAMENT_STATUS_ACTIONS,
      id: perProjectTournamentId(MOCK_TOURNAMENT_STATUS_ACTIONS_ID, projectName),
    },
  ],
  [
    perProjectTournamentId(MOCK_TOURNAMENT_TRUNCATE_ACTIONS_ID, projectName),
    {
      ...MOCK_TOURNAMENT_TRUNCATE_ACTIONS,
      id: perProjectTournamentId(MOCK_TOURNAMENT_TRUNCATE_ACTIONS_ID, projectName),
    },
  ],
]);

// Team eligibility fixtures (TeamEligibility.spec.ts, TournamentDetail.spec.ts, Tournaments.spec.ts): an
// upcoming doubles tournament with defending champions. The errors copy exists so the spec that makes
// its eligibility check fail does not affect the other specs, and it gets one copy per project like
// the status-action tournaments above.
export const MOCK_TOURNAMENT_TEAM_ID = '01JX0000000000000000000090';
export const MOCK_TOURNAMENT_TEAM_ERRORS_ID = '01JX0000000000000000000091';

export const MOCK_BOWLER_DEFENDER_ONE_ID = '01JX0000000000000000000081';
export const MOCK_BOWLER_DEFENDER_TWO_ID = '01JX0000000000000000000082';
export const MOCK_BOWLER_CHAMPION_ONE_ID = '01JX0000000000000000000083';
export const MOCK_BOWLER_CHAMPION_TWO_ID = '01JX0000000000000000000084';
export const MOCK_BOWLER_NEWCOMER_ID = '01JX0000000000000000000085';

const MOCK_DEFENDING_CHAMPIONS = [
  {
    tournamentName: 'NEBA Team Doubles',
    seasonDescription: '2098 Season',
    sourceTruncated: false,
    undefended: false,
    champions: ['Dana Defender', 'Dave Defender'],
    consecutiveTitles: 2,
  },
];

const MOCK_TOURNAMENT_TEAM = {
  ...MOCK_TOURNAMENT_STATUS_ACTIONS,
  id: MOCK_TOURNAMENT_TEAM_ID,
  name: 'NEBA Team Doubles',
  season: '2099 Season',
  startDate: '2099-03-06',
  endDate: '2099-03-07',
  status: 'Scheduled',
  tournamentType: 'Doubles',
  teamSize: 2,
  previousTournamentId: '01JX0000000000000000000089',
  defendingChampions: MOCK_DEFENDING_CHAMPIONS,
};

const PER_PROJECT_TEAM_ERRORS_TOURNAMENTS: [string, object][] = E2E_PROJECT_NAMES.map((projectName) => [
  perProjectTournamentId(MOCK_TOURNAMENT_TEAM_ERRORS_ID, projectName),
  {
    ...MOCK_TOURNAMENT_TEAM,
    id: perProjectTournamentId(MOCK_TOURNAMENT_TEAM_ERRORS_ID, projectName),
    name: 'NEBA Team Doubles Errors',
  },
]);

const MOCK_BOWLER_SEARCH_RESULTS = [
  { bowlerId: MOCK_BOWLER_DEFENDER_ONE_ID, name: 'Dana Defender', usbcId: '4821-09317' },
  { bowlerId: MOCK_BOWLER_DEFENDER_TWO_ID, name: 'Dave Defender', usbcId: '4821-09318' },
  { bowlerId: MOCK_BOWLER_CHAMPION_ONE_ID, name: 'Chris Champion', usbcId: '1111-00001' },
  { bowlerId: MOCK_BOWLER_CHAMPION_TWO_ID, name: 'Cora Champion', usbcId: '1111-00002' },
  { bowlerId: MOCK_BOWLER_NEWCOMER_ID, name: 'Nina Newcomer' },
];

// Title facts for the mock bowlers: [titles in the last 5 years, in the last 10 years, last title year].
const MOCK_BOWLER_TITLES: Record<string, [number, number, number | null]> = {
  [MOCK_BOWLER_CHAMPION_ONE_ID]: [1, 2, 2024],
  [MOCK_BOWLER_CHAMPION_TWO_ID]: [0, 1, 2019],
};

function mockStanding(titles: number): string {
  if (titles === 0) {
    return 'NonChampion';
  }

  return titles === 1 ? 'OneTimeChampion' : 'MultiTimeChampion';
}

function mockMemberFacts(bowlerIds: string[], unknownBowlerCount: number, championsFail: boolean): object[] {
  const known = bowlerIds.map((id) => {
    const found = MOCK_BOWLER_SEARCH_RESULTS.find((bowler) => bowler.bowlerId === id);
    const [titles5, titles10, lastTitleYear] = MOCK_BOWLER_TITLES[id] ?? [0, 0, null];
    const standing = mockStanding(titles10);

    return {
      name: found?.name ?? 'Unknown Bowler',
      usbcId: found && 'usbcId' in found ? found.usbcId : null,
      notInSystem: false,
      lastTitleYear,
      titlesLast5Years: titles5,
      titlesLast10Years: titles10,
      countsAs: standing,
      ageVerified: null,
      hallOfFameSuperiorPerformance: null,
      titlesBreakRule: championsFail && titles10 > 0,
      ageBreaksRule: false,
      hallOfFameBreaksRule: false,
    };
  });

  const unknown = Array.from({ length: unknownBowlerCount }, () => ({
    name: null, usbcId: null, notInSystem: true, lastTitleYear: null, titlesLast5Years: 0, titlesLast10Years: 0,
    countsAs: 'NonChampion', ageVerified: null, hallOfFameSuperiorPerformance: null,
    titlesBreakRule: false, ageBreaksRule: false, hallOfFameBreaksRule: false,
  }));

  return [...known, ...unknown];
}

const MOCK_FACTS_SUMMARY =
  'For Doubles, a champion is a bowler with a NEBA title in the 10 years before the tournament (5 years once a bowler has turned 60). The dates checked are Mar 6, 2099 and Mar 7, 2099.';

// A simple stand-in for the real rules, so each outcome has a team that reaches it: the two
// defenders together are defending champions, the two champions together are over the champion
// limit, and anyone else is eligible.
function resolveTeamEligibilityRoute(pathname: string, searchParams: URLSearchParams): object | null {
  const match = /^\/tournaments\/([^/]+)\/team-eligibility$/.exec(pathname);
  if (!match) return null;

  const bowlerIds = searchParams.getAll('bowlerIds');
  const unknownBowlerCount = Number.parseInt(searchParams.get('unknownBowlerCount') ?? '0', 10) || 0;
  const has = (id: string) => bowlerIds.includes(id);

  if (has(MOCK_BOWLER_DEFENDER_ONE_ID) && has(MOCK_BOWLER_DEFENDER_TWO_ID)) {
    const source = MOCK_DEFENDING_CHAMPIONS[0];
    return {
      outcome: 'DefendingChampions',
      reasons: [],
      dateChecks: [],
      professionalTitlesVerified: false,
      defendingSource: {
        tournamentName: source.tournamentName,
        seasonDescription: source.seasonDescription,
        sourceTruncated: source.sourceTruncated,
        undefended: source.undefended,
      },
    };
  }

  if (has(MOCK_BOWLER_CHAMPION_ONE_ID) && has(MOCK_BOWLER_CHAMPION_TWO_ID)) {
    return {
      outcome: 'Ineligible',
      reasons: [{ code: 'Champions.TooMany', message: 'A team may have at most 1 previous champion.' }],
      dateChecks: [],
      professionalTitlesVerified: false,
      defendingSource: null,
      summary: MOCK_FACTS_SUMMARY,
      members: mockMemberFacts(bowlerIds, unknownBowlerCount, true),
      checks: [{ label: 'Champions: 2 (at most 1)', passed: false }],
    };
  }

  return {
    outcome: 'Eligible',
    reasons: [],
    dateChecks: [],
    professionalTitlesVerified: false,
    defendingSource: null,
    summary: MOCK_FACTS_SUMMARY,
    members: mockMemberFacts(bowlerIds, unknownBowlerCount, false),
    checks: [{ label: 'Champions: 0 (at most 1)', passed: true }],
  };
}

function resolveBowlerSearchRoute(pathname: string, searchParams: URLSearchParams): object | null {
  if (pathname !== '/bowlers') return null;

  const name = (searchParams.get('name') ?? '').toLowerCase();
  const items = MOCK_BOWLER_SEARCH_RESULTS.filter((bowler) => bowler.name.toLowerCase().includes(name));
  return { items, totalItems: items.length };
}

// Added money breakdown fixtures. Separate from MOCK_TOURNAMENT_DETAIL so its fixed sponsor list
// stays valid for the other tournament-detail tests.
export const MOCK_TOURNAMENT_ADDED_MONEY_UPCOMING_ID = '01JX0000000000000000000060';
export const MOCK_TOURNAMENT_ADDED_MONEY_PAST_ID = '01JX0000000000000000000061';

const MOCK_TOURNAMENT_ADDED_MONEY_UPCOMING = {
  ...MOCK_TOURNAMENT_SPONSOR_MGMT,
  id: MOCK_TOURNAMENT_ADDED_MONEY_UPCOMING_ID,
  name: 'NEBA Added Money Upcoming Open',
  startDate: '2099-01-15',
  endDate: '2099-01-15',
  status: 'Scheduled',
  addedMoney: 1550,
  nebaAddedMoney: 300,
  sponsors: [
    {
      sponsorId: '01JX0000000000000000000201',
      name: 'Pro Shop Plus',
      slug: 'pro-shop-plus',
      logoUrl: null,
      websiteUrl: null,
      tagPhrase: null,
      titleSponsor: false,
      sponsorshipAmount: 1000,
    },
    {
      sponsorId: '01JX0000000000000000000202',
      name: 'Greater Boston Area Bowling Proprietors Association & Friends of NEBA',
      slug: 'greater-boston-proprietors',
      logoUrl: null,
      websiteUrl: null,
      tagPhrase: null,
      titleSponsor: false,
      sponsorshipAmount: 250,
    },
  ],
  results: [],
};

const MOCK_TOURNAMENT_ADDED_MONEY_PAST = {
  ...MOCK_TOURNAMENT_SPONSOR_MGMT,
  id: MOCK_TOURNAMENT_ADDED_MONEY_PAST_ID,
  name: 'NEBA Added Money Past Open',
  startDate: '2024-03-02',
  endDate: '2024-03-02',
  status: 'Completed',
  addedMoney: 1500,
  nebaAddedMoney: 100,
  sponsors: [
    {
      sponsorId: '01JX0000000000000000000211',
      name: 'Presenting Co',
      slug: 'presenting-co',
      logoUrl: null,
      websiteUrl: null,
      tagPhrase: null,
      titleSponsor: true,
      sponsorshipAmount: 0,
    },
    {
      sponsorId: '01JX0000000000000000000212',
      name: 'Zenith Lanes',
      slug: 'zenith-lanes',
      logoUrl: null,
      websiteUrl: null,
      tagPhrase: null,
      titleSponsor: false,
      sponsorshipAmount: 700,
    },
    {
      sponsorId: '01JX0000000000000000000213',
      name: 'Regional Lanes',
      slug: 'regional-lanes',
      logoUrl: null,
      websiteUrl: null,
      tagPhrase: null,
      titleSponsor: false,
      sponsorshipAmount: 700,
    },
  ],
  results: [],
};

// Invitational fixtures for the rules link/slide-out tests: one upcoming (link shows) and one
// completed (link hidden, because the rules may have changed since it ran).
export const MOCK_TOURNAMENT_INVITATIONAL_UPCOMING_ID = '01JX0000000000000000000070';
export const MOCK_TOURNAMENT_INVITATIONAL_PAST_ID = '01JX0000000000000000000071';

const MOCK_TOURNAMENT_INVITATIONAL_UPCOMING = {
  ...MOCK_TOURNAMENT_STATUS_ACTIONS,
  id: MOCK_TOURNAMENT_INVITATIONAL_UPCOMING_ID,
  name: 'NEBA Upcoming Invitational',
  startDate: '2099-02-15',
  endDate: '2099-02-15',
  status: 'Scheduled',
  tournamentType: 'Invitational',
};

const MOCK_TOURNAMENT_INVITATIONAL_PAST = {
  ...MOCK_TOURNAMENT_STATUS_ACTIONS,
  id: MOCK_TOURNAMENT_INVITATIONAL_PAST_ID,
  name: 'NEBA Past Invitational',
  startDate: '2024-02-15',
  endDate: '2024-02-15',
  status: 'Completed',
  tournamentType: 'Invitational',
};

const EXTRA_TOURNAMENT_DETAILS = new Map<string, object>([
  [MOCK_TOURNAMENT_OIL_REVEAL_PENDING_ID, MOCK_TOURNAMENT_OIL_REVEAL_PENDING],
  [MOCK_TOURNAMENT_OIL_REVEALED_ID, MOCK_TOURNAMENT_OIL_REVEALED],
  [MOCK_TOURNAMENT_OIL_REVEAL_MGMT_ID, MOCK_TOURNAMENT_OIL_REVEAL_MGMT],
  [MOCK_TOURNAMENT_SPONSOR_MGMT_ID, MOCK_TOURNAMENT_SPONSOR_MGMT],
  [MOCK_TOURNAMENT_STATUS_ACTIONS_ID, MOCK_TOURNAMENT_STATUS_ACTIONS],
  [MOCK_TOURNAMENT_TRUNCATE_ACTIONS_ID, MOCK_TOURNAMENT_TRUNCATE_ACTIONS],
  [MOCK_TOURNAMENT_ADDED_MONEY_UPCOMING_ID, MOCK_TOURNAMENT_ADDED_MONEY_UPCOMING],
  [MOCK_TOURNAMENT_ADDED_MONEY_PAST_ID, MOCK_TOURNAMENT_ADDED_MONEY_PAST],
  [MOCK_TOURNAMENT_INVITATIONAL_UPCOMING_ID, MOCK_TOURNAMENT_INVITATIONAL_UPCOMING],
  [MOCK_TOURNAMENT_INVITATIONAL_PAST_ID, MOCK_TOURNAMENT_INVITATIONAL_PAST],
  [MOCK_TOURNAMENT_TEAM_ID, MOCK_TOURNAMENT_TEAM],
  [
    MOCK_SERIES_QUALIFYING_STAGE_TOURNAMENT_ID,
    seriesStageDetail(MOCK_SERIES_QUALIFYING_STAGE_TOURNAMENT_ID, 'NEBA Series Qualifier', '2026-04-04', SERIES_TOURNAMENT_QUALIFYING),
  ],
  [
    MOCK_SERIES_PLACEMENT_STAGE_TOURNAMENT_ID,
    seriesStageDetail(MOCK_SERIES_PLACEMENT_STAGE_TOURNAMENT_ID, 'NEBA Series Placement', '2026-04-05', SERIES_TOURNAMENT_PLACEMENT),
  ],
  ...PER_PROJECT_STATUS_TOURNAMENTS,
  ...PER_PROJECT_TEAM_ERRORS_TOURNAMENTS,
]);

// Invitational eligible bowlers (TournamentDetail.spec.ts): only the upcoming Invitational has a
// list. One bowler has no USBC ID.
function resolveEligibleBowlersRoute(pathname: string): object | null {
  const match = /^\/tournaments\/([^/]+)\/eligible-bowlers$/.exec(pathname);
  if (!match) return null;

  if (match[1] !== MOCK_TOURNAMENT_INVITATIONAL_UPCOMING_ID) {
    return { requirements: null, bowlers: [] };
  }

  return {
    requirements: { minTournamentsCount: 3 },
    bowlers: [
      { name: 'Alice Anders', usbcId: '2222-00001' },
      { name: 'Ben Baxter', usbcId: null },
      { name: 'Carla Cole', usbcId: '2222-00003' },
    ],
  };
}

type SeasonVariants = {
  label: string;
  leaderName: string;
  rivalName: string;
  points: number;
  average: number;
  winnings: number;
  highBlock: number;
  highGame: number;
  matchPlayAverage: number;
  pointsPerEntry: number;
  pointsPerTournament: number;
  totalEntries: number;
  totalPrizeMoney: number;
  seasonHighGame: number;
  seasonHighBlock: number;
  openerDate: string;
  openerPoints: number;
  winterDate: string;
};

const LEGACY_SEASON: SeasonVariants = {
  label: '2020-2021 Season',
  leaderName: 'Legacy Leader',
  rivalName: 'Legacy Rival',
  points: 184,
  average: 219.35,
  winnings: 1320,
  highBlock: 1120,
  highGame: 279,
  matchPlayAverage: 218.5,
  pointsPerEntry: 23,
  pointsPerTournament: 26.29,
  totalEntries: 152,
  totalPrizeMoney: 18800,
  seasonHighGame: 289,
  seasonHighBlock: 1189,
  openerDate: '2020-09-19',
  openerPoints: 32,
  winterDate: '2021-01-16',
};

const CURRENT_SEASON: SeasonVariants = {
  label: '2024-2025 Season',
  leaderName: 'Current Leader',
  rivalName: 'Current Rival',
  points: 267,
  average: 228.42,
  winnings: 2430,
  highBlock: 1198,
  highGame: 289,
  matchPlayAverage: 226.8,
  pointsPerEntry: 33.38,
  pointsPerTournament: 38.14,
  totalEntries: 194,
  totalPrizeMoney: 25400,
  seasonHighGame: 300,
  seasonHighBlock: 1264,
  openerDate: '2024-09-21',
  openerPoints: 45,
  winterDate: '2025-01-18',
};

// A long standings list for the current season, so the full-standings modal has to virtualize its rows.
function createExtraBowlerOfTheYearRows(count: number): object[] {
  return Array.from({ length: count }, (_, i) => ({
    bowlerId: `01JXBOTY${String(i + 2).padStart(18, '0')}`,
    bowlerName: `Bowler ${i + 2}`,
    points: Math.max(1, 5000 - (i + 1) * 8),
    tournaments: 6,
    entries: 7,
    finals: 3,
    averageFinish: 12.5,
    winnings: 1000,
  }));
}

function createStatsResponse(selectedYear: number): object {
  const s = selectedYear === 2021 ? LEGACY_SEASON : CURRENT_SEASON;

  return {
    selectedSeason: s.label,
    availableSeasons: {
      2025: '2024-2025 Season',
      2021: '2020-2021 Season',
    },
    bowlerSearchList: {
      [PRIMARY_BOWLER_ID]: s.leaderName,
      [SECONDARY_BOWLER_ID]: s.rivalName,
    },
    minimumNumberOfGames: 20,
    minimumNumberOfTournaments: 4,
    minimumNumberOfEntries: 6,
    bowlerOfTheYear: [
      { bowlerId: PRIMARY_BOWLER_ID, bowlerName: s.leaderName, points: s.points, tournaments: 7, entries: 8, finals: 5, averageFinish: 6.4, winnings: s.winnings },
      ...(selectedYear === 2021 ? [] : createExtraBowlerOfTheYearRows(559)),
    ],
    seniorOfTheYear: [],
    superSeniorOfTheYear: [],
    womanOfTheYear: [],
    rookieOfTheYear: [],
    youthOfTheYear: [],
    highAverage: [
      { bowlerId: PRIMARY_BOWLER_ID, bowlerName: s.leaderName, average: s.average, games: 35, tournaments: 7, fieldAverage: 9.2 },
    ],
    highBlock: [
      { bowlerId: PRIMARY_BOWLER_ID, bowlerName: s.leaderName, highBlock: s.highBlock, highGame: s.highGame },
    ],
    matchPlayAverage: [
      { bowlerId: PRIMARY_BOWLER_ID, bowlerName: s.leaderName, matchPlayAverage: s.matchPlayAverage, games: 18, wins: 12, losses: 6, winPercentage: 66.7, winnings: s.winnings },
    ],
    matchPlayRecord: [
      { bowlerId: PRIMARY_BOWLER_ID, bowlerName: s.leaderName, wins: 12, losses: 6, winPercentage: 66.7, finals: 5, matchPlayAverage: s.matchPlayAverage, winnings: s.winnings },
    ],
    matchPlayAppearances: [
      { bowlerId: PRIMARY_BOWLER_ID, bowlerName: s.leaderName, finals: 5, tournaments: 7, entries: 8 },
    ],
    pointsPerEntry: [
      { bowlerId: PRIMARY_BOWLER_ID, bowlerName: s.leaderName, pointsPerEntry: s.pointsPerEntry, points: s.points, entries: 8 },
    ],
    pointsPerTournament: [
      { bowlerId: PRIMARY_BOWLER_ID, bowlerName: s.leaderName, points: s.points, tournaments: 7, pointsPerTournament: s.pointsPerTournament },
    ],
    finalsPerEntry: [
      { bowlerId: PRIMARY_BOWLER_ID, bowlerName: s.leaderName, finals: 5, entries: 8, finalsPerEntry: 0.63 },
    ],
    averageFinishes: [
      { bowlerId: PRIMARY_BOWLER_ID, bowlerName: s.leaderName, averageFinish: 6.4, finals: 5, winnings: s.winnings },
    ],
    seasonAtAGlance: { totalEntries: s.totalEntries, totalPrizeMoney: s.totalPrizeMoney },
    seasonsBests: {
      highGame: s.seasonHighGame,
      highGameBowlers: { [PRIMARY_BOWLER_ID]: s.leaderName },
      highBlock: s.seasonHighBlock,
      highBlockBowlers: { [PRIMARY_BOWLER_ID]: s.leaderName },
      highAverage: s.average,
      highAverageBowlers: { [PRIMARY_BOWLER_ID]: s.leaderName },
    },
    fieldMatchPlaySummary: {
      highestWinPercentage: 66.7,
      highestWinPercentageBowlers: { [PRIMARY_BOWLER_ID]: s.leaderName },
      mostFinals: 5,
      mostFinalsBowlers: { [PRIMARY_BOWLER_ID]: s.leaderName },
    },
    openPointsRace: [
      {
        bowlerId: PRIMARY_BOWLER_ID,
        bowlerName: s.leaderName,
        results: [
          { tournamentName: 'Season Opener', tournamentDate: s.openerDate, cumulativePoints: s.openerPoints },
          { tournamentName: 'Winter Classic', tournamentDate: s.winterDate, cumulativePoints: s.points },
        ],
      },
    ],
    seniorPointsRace: [],
    superSeniorPointsRace: [],
    womenPointsRace: [],
    youthPointsRace: [],
    rookiePointsRace: [],
    allBowlers: [
      {
        bowlerId: PRIMARY_BOWLER_ID,
        bowlerName: s.leaderName,
        points: s.points,
        average: s.average,
        games: 35,
        finals: 5,
        wins: 12,
        losses: 6,
        winPercentage: 66.7,
        matchPlayAverage: s.matchPlayAverage,
        winnings: s.winnings,
        fieldAverage: 9.2,
        tournaments: 7,
      },
      {
        bowlerId: SECONDARY_BOWLER_ID,
        bowlerName: s.rivalName,
        points: s.points - 40,
        average: s.average - 4,
        games: 31,
        finals: 3,
        wins: 8,
        losses: 7,
        winPercentage: 53.3,
        matchPlayAverage: s.matchPlayAverage - 3,
        winnings: s.winnings - 600,
        fieldAverage: 3.4,
        tournaments: 6,
      },
    ],
  };
}

const MOCK_TOURNAMENT_CHAMPIONS = {
  items: [
    {
      tournamentId: '01JX0000000000000000000010',
      tournamentName: 'NEBA Spring Classic',
      tournamentDate: '2024-04-20',
      tournamentType: 'Singles',
      champions: [
        { bowlerId: PRIMARY_BOWLER_ID, bowlerName: 'Current Leader', bowlerLastName: 'Leader', bowlerFirstName: 'Current', hallOfFame: true },
      ],
    },
    {
      tournamentId: '01JX0000000000000000000011',
      tournamentName: 'NEBA Fall Doubles',
      tournamentDate: '2024-10-15',
      tournamentType: 'Doubles',
      champions: [
        { bowlerId: PRIMARY_BOWLER_ID, bowlerName: 'Current Leader', bowlerLastName: 'Leader', bowlerFirstName: 'Current', hallOfFame: true },
        { bowlerId: SECONDARY_BOWLER_ID, bowlerName: 'Current Rival', bowlerLastName: 'Rival', bowlerFirstName: 'Current', hallOfFame: false },
      ],
    },
    {
      tournamentId: '01JX0000000000000000000012',
      tournamentName: 'NEBA Winter Classic',
      tournamentDate: '2023-01-21',
      tournamentType: 'Singles',
      champions: [
        { bowlerId: SECONDARY_BOWLER_ID, bowlerName: 'Current Rival', bowlerLastName: 'Rival', bowlerFirstName: 'Current', hallOfFame: false },
      ],
    },
  ],
};

const MOCK_BOWLER_TITLES_CURRENT_LEADER = {
  bowlerName: 'Current Leader',
  hallOfFame: true,
  titles: [
    { tournamentId: '01JX0000000000000000000010', tournamentName: 'NEBA Spring Classic', tournamentDate: '2024-04-20', tournamentType: 'Singles' },
    { tournamentId: '01JX0000000000000000000011', tournamentName: 'NEBA Fall Doubles', tournamentDate: '2024-10-15', tournamentType: 'Doubles' },
  ],
};

const MOCK_BOWLER_TITLES_CURRENT_RIVAL = {
  bowlerName: 'Current Rival',
  hallOfFame: false,
  titles: [
    { tournamentId: '01JX0000000000000000000011', tournamentName: 'NEBA Fall Doubles', tournamentDate: '2024-10-15', tournamentType: 'Doubles' },
    { tournamentId: '01JX0000000000000000000012', tournamentName: 'NEBA Winter Classic', tournamentDate: '2023-01-21', tournamentType: 'Singles' },
  ],
};

// Every mock tournament is in the past, so nothing is upcoming. Must stay ahead of the
// /tournaments/{id} detail route, which would read "upcoming" as a tournament ID. (An upcoming
// tournament here would also replace the Tournaments page hero, which Tournaments.spec expects to
// be the plain page title.)
export const MOCK_UPCOMING_TOURNAMENTS = { items: [], totalItems: 0 };

export const MOCK_SEASONS = {
  items: [
    {
      id: MOCK_SEASON_ID,
      description: '2025-2026 Season',
      startDate: '2025-09-01',
      endDate: '2026-05-31',
    },
  ],
};

export const ARTICLE_ID_SEASON_CHAMPIONS = '01JX0000000000000000000101';
export const ARTICLE_ID_JUNE_LANE_PATTERN = '01JX0000000000000000000102';
export const ARTICLE_ID_POINTS_RACE = '01JX0000000000000000000103';

const MOCK_NEWS_PAGE_1 = {
  items: [
    {
      articleId: ARTICLE_ID_SEASON_CHAMPIONS,
      slug: 'season-champions-2026',
      publicationStatus: 'Published',
      title: '2025–26 Season Champions Crowned at Tournament of Champions',
      excerpt: 'After a dominant season, the finals came down to two of NEBA\'s most decorated veterans. Find out who took home the title and how the points race shook out heading into next year.',
      headerImageUrl: null,
      publishDateUtc: '2026-05-15T12:00:00+00:00',
    },
    {
      articleId: ARTICLE_ID_JUNE_LANE_PATTERN,
      slug: 'june-lane-pattern',
      publicationStatus: 'Published',
      title: 'Lane Pattern Announced for June Southside Classic',
      excerpt: 'The June monthly at Southside Bowl will feature the WTBA London sport pattern. Download the PDF and check qualifying details.',
      headerImageUrl: null,
      publishDateUtc: '2026-05-01T12:00:00+00:00',
    },
    {
      articleId: ARTICLE_ID_POINTS_RACE,
      slug: 'points-race-update',
      publicationStatus: 'Published',
      title: 'Points Race Update: Three Bowlers Separated by Eight Points',
      excerpt: 'With two tournaments left, the Bowler of the Year race is razor-thin. Here\'s the current standings and what each contender needs.',
      headerImageUrl: null,
      publishDateUtc: '2026-04-18T12:00:00+00:00',
    },
  ],
  totalItems: 3,
  pageNumber: 1,
  pageSize: 10,
};

const MOCK_ARTICLE_SEASON_CHAMPIONS: object = {
  articleId: ARTICLE_ID_SEASON_CHAMPIONS,
  slug: 'season-champions-2026',
  publicationStatus: 'Published',
  title: '2025–26 Season Champions Crowned at Tournament of Champions',
  content: '<p>After a dominant regular season, the 2025–26 NEBA Tournament of Champions brought together the top performers from across New England for a single-elimination finale at Baxter Bowl in Springfield.</p><p>The field was deep. Twelve qualifiers entered match play, but it was two bowlers who had been trading the points lead all season who ultimately met in the final: defending champion Marcus Roark and two-time high-average winner Diane Pellerin.</p><p>Pellerin answered with a strike in the 10th to post a 267 and claim her first Tournament of Champions title.</p>',
  headerImageUrl: null,
  publishDateUtc: '2026-05-15T12:00:00+00:00',
  tournamentId: MOCK_TOURNAMENT_ID,
  attachments: [
    { displayName: 'Tournament Results & Bracket', contentType: 'application/pdf', url: 'https://files.bowlneba.com/news/season-champions-2026/bracket.pdf', isInline: false, container: 'news', path: 'season-champions-2026/bracket.pdf', sizeInBytes: 245760 },
    { displayName: 'Lane Pattern (WTBA London)', contentType: 'application/pdf', url: 'https://files.bowlneba.com/news/season-champions-2026/lane-pattern.pdf', isInline: false, container: 'news', path: 'season-champions-2026/lane-pattern.pdf', sizeInBytes: 189440 },
  ],
};

const MOCK_ARTICLE_JUNE_LANE_PATTERN: object = {
  articleId: ARTICLE_ID_JUNE_LANE_PATTERN,
  slug: 'june-lane-pattern',
  publicationStatus: 'Published',
  title: 'Lane Pattern Announced for June Southside Classic',
  content: '<p>The June monthly at Southside Bowl will feature the WTBA London sport pattern. Download the PDF below and check qualifying details.</p><p>Registration opens May 20th. Entry fee is $75 per bowler.</p>',
  headerImageUrl: null,
  publishDateUtc: '2026-05-01T12:00:00+00:00',
  announcedAtUtc: '2026-05-01T12:02:00+00:00',
  tournamentId: null,
  attachments: [
    { displayName: 'Lane Pattern PDF', contentType: 'application/pdf', url: 'https://files.bowlneba.com/news/june-lane-pattern/pattern.pdf', isInline: false, container: 'news', path: 'june-lane-pattern/pattern.pdf', sizeInBytes: 156672 },
  ],
};

const MOCK_ARTICLE_POINTS_RACE: object = {
  articleId: ARTICLE_ID_POINTS_RACE,
  slug: 'points-race-update',
  publicationStatus: 'Published',
  title: 'Points Race Update: Three Bowlers Separated by Eight Points',
  content: '<p>With two tournaments left, the Bowler of the Year race is razor-thin. Here\'s the current standings and what each contender needs to clinch the title heading into the final stretch.</p>',
  headerImageUrl: null,
  publishDateUtc: '2026-04-18T12:00:00+00:00',
  tournamentId: null,
  attachments: [],
};

const MOCK_US_STATES = {
  items: [
    { name: 'Massachusetts', code: 'MA' },
    { name: 'New Hampshire', code: 'NH' },
    { name: 'Rhode Island', code: 'RI' },
  ],
};

const MOCK_PHONE_NUMBER_TYPES = {
  items: [
    { name: 'Home', code: 'H' },
    { name: 'Mobile', code: 'M' },
    { name: 'Work', code: 'W' },
    { name: 'Fax', code: 'F' },
  ],
};

const MOCK_USERS = {
  items: [
    {
      userId: '01JX0000000000000000000499',
      email: 'webmaster@bowlneba.com',
      emailConfirmed: true,
      roles: ['Webmaster'],
    },
    {
      userId: '01JX0000000000000000000500',
      email: 'invited.staff@bowlneba.com',
      emailConfirmed: false,
      roles: ['Journalist'],
    },
    {
      userId: '01JX0000000000000000000498',
      email: 'admin@bowlneba.com',
      emailConfirmed: true,
      roles: ['Admin'],
    },
  ],
  totalItems: 3,
  pageNumber: 1,
  pageSize: 20,
};

const ALL_PERMISSION_VALUES = [
  'BackgroundJobs.ViewDashboard',
  'Cache.Clear',
  'Documents.RefreshDocument',
  'News.CreateArticle',
  'News.DeleteArticle',
  'News.EditArticle',
  'Sponsors.CreateSponsor',
  'Sponsors.EditSponsor',
  'System.CreateUser',
  'System.EditUser',
  'System.GetUsers',
  'System.ResetUserPassword',
  'Tournaments.CreateTournament',
  'Tournaments.DeleteTournament',
  'Tournaments.EditTournament',
  'Tournaments.EditTournamentLogo',
  'Tournaments.ManageSponsors',
  'Tournaments.ManageTournamentStatus',
];

const MOCK_ROLES = {
  roles: [
    { name: 'Admin', permissions: ALL_PERMISSION_VALUES },
    {
      name: 'Journalist',
      permissions: ['News.CreateArticle', 'News.DeleteArticle', 'News.EditArticle'],
    },
    {
      name: 'Manager',
      permissions: [
        'News.CreateArticle',
        'News.DeleteArticle',
        'News.EditArticle',
        'Sponsors.CreateSponsor',
        'Sponsors.EditSponsor',
        'Tournaments.CreateTournament',
        'Tournaments.EditTournament',
        'Tournaments.EditTournamentLogo',
        'Tournaments.ManageSponsors',
        'Tournaments.ManageTournamentStatus',
      ],
    },
    { name: 'Member', permissions: [] },
    {
      name: 'Tournament Director',
      permissions: [
        'Tournaments.CreateTournament',
        'Tournaments.EditTournament',
        'Tournaments.EditTournamentLogo',
        'Tournaments.ManageSponsors',
        'Tournaments.ManageTournamentStatus',
      ],
    },
    {
      name: 'Webmaster',
      permissions: [
        'BackgroundJobs.ViewDashboard',
        'Documents.RefreshDocument',
        'News.CreateArticle',
        'News.DeleteArticle',
        'News.EditArticle',
        'Sponsors.CreateSponsor',
        'Sponsors.EditSponsor',
        'System.CreateUser',
        'System.EditUser',
        'System.GetUsers',
        'System.ResetUserPassword',
        'Tournaments.CreateTournament',
        'Tournaments.DeleteTournament',
        'Tournaments.EditTournament',
        'Tournaments.ManageSponsors',
        'Tournaments.ManageTournamentStatus',
      ],
    },
  ],
};

// Bodies of the last non-GET request per "METHOD /path", so specs can assert what the UI sent.
const lastRequestBodies = new Map<string, string>();

const routes: Record<string, unknown> = {
  '/health': { status: 'healthy' },
  '/security/users': MOCK_USERS,
  '/security/roles': MOCK_ROLES,
  '/documents/tournament-rules': { html: MOCK_TOURNAMENT_RULES_HTML },
  '/documents/bylaws': { html: MOCK_BYLAWS_HTML },
  '/reference-data/us-states': MOCK_US_STATES,
  '/reference-data/phone-number-types': MOCK_PHONE_NUMBER_TYPES,
  '/bowling-centers': MOCK_BOWLING_CENTERS,
  '/oil-patterns': MOCK_OIL_PATTERNS,
  '/seasons': MOCK_SEASONS,
  '/sponsors': MOCK_SPONSORS_ACTIVE,
  '/sponsors/pro-shop-plus': MOCK_SPONSOR_PRO_SHOP_PLUS,
  '/news': MOCK_NEWS_PAGE_1,
  '/news/season-champions-2026': MOCK_ARTICLE_SEASON_CHAMPIONS,
  '/news/june-lane-pattern': MOCK_ARTICLE_JUNE_LANE_PATTERN,
  '/news/points-race-update': MOCK_ARTICLE_POINTS_RACE,
  '/hall-of-fame/inductions': MOCK_HALL_OF_FAME,
  '/hall-of-fame/points': MOCK_HALL_OF_FAME_POINTS,
  '/awards/bowler-of-the-year': MOCK_BOWLER_OF_THE_YEAR_AWARDS,
  '/awards/high-average': MOCK_HIGH_AVERAGE_AWARDS,
  '/awards/high-block': MOCK_HIGH_BLOCK_AWARDS,
};

function resolveStatsRoute(pathname: string, searchParams: URLSearchParams): object | null {
  if (pathname !== '/stats') return null;

  const requestedYear = Number.parseInt(searchParams.get('year') ?? '2025', 10);
  const selectedYear = Number.isFinite(requestedYear) ? requestedYear : 2025;
  return createStatsResponse(selectedYear);
}

function resolveSeasonTournamentsRoute(pathname: string): object | null {
  if (!pathname.startsWith('/seasons/') || !pathname.endsWith('/tournaments')) return null;

  const seasonId = pathname.slice('/seasons/'.length, -'/tournaments'.length);
  if (seasonId === MOCK_SERIES_SEASON_ID) return mockSeriesSeasonTournaments();
  return seasonId === MOCK_SEASON_ID ? MOCK_SEASON_TOURNAMENTS : null;
}

function resolveStaticGetRoute(pathname: string): object | null {
  if (pathname === '/tournaments/champions') return MOCK_TOURNAMENT_CHAMPIONS;
  if (pathname === '/tournaments/types') return MOCK_TOURNAMENT_TYPES;
  if (pathname === '/tournaments/upcoming') return MOCK_UPCOMING_TOURNAMENTS;
  if (pathname === `/bowlers/${PRIMARY_BOWLER_ID}/titles`) return MOCK_BOWLER_TITLES_CURRENT_LEADER;
  if (pathname === `/bowlers/${SECONDARY_BOWLER_ID}/titles`) return MOCK_BOWLER_TITLES_CURRENT_RIVAL;
  if (pathname.startsWith('/hall-of-fame/points/')) {
    const bowlerId = pathname.slice('/hall-of-fame/points/'.length);
    return MOCK_HALL_OF_FAME_POINTS_DETAIL(bowlerId) ?? MOCK_INDUCTEE_DETAILS.get(bowlerId) ?? null;
  }
  return null;
}

function resolveTournamentDetailRoute(pathname: string): object | null {
  if (!pathname.startsWith('/tournaments/')) return null;

  const tournamentId = pathname.slice('/tournaments/'.length);
  if (tournamentId === MOCK_TOURNAMENT_ID) return MOCK_TOURNAMENT_DETAIL;
  if (EXTRA_TOURNAMENT_DETAILS.has(tournamentId)) return EXTRA_TOURNAMENT_DETAILS.get(tournamentId) ?? null;
  const created = createdTournaments.get(tournamentId);
  if (created) return created;
  // Generated Series Stage tournaments (see mockSeriesStages) link to a detail page that must resolve.
  const stage = mockSeriesStages().find((candidate) => (candidate as { tournamentId: string }).tournamentId === tournamentId) as
    | { tournamentName: string; tournamentStartDate: string }
    | undefined;
  return stage ? seriesStageDetail(tournamentId, stage.tournamentName, stage.tournamentStartDate, null) : null;
}

function resolveSponsorDetailRoute(pathname: string): object | null {
  if (!pathname.startsWith('/sponsors/')) return null;

  const slug = pathname.slice('/sponsors/'.length);
  return createdSponsors.get(slug) ?? null;
}

function resolveUserDetailRoute(pathname: string): object | null {
  if (!pathname.startsWith('/security/users/')) return null;

  const userId = pathname.slice('/security/users/'.length);
  const user = MOCK_USERS.items.find((candidate) => candidate.userId === userId);

  return user ? { ...user, addedPermissions: [], removedPermissions: [] } : null;
}

function resolveGetRoute(pathname: string, searchParams: URLSearchParams): object | null {
  return (
    resolveStatsRoute(pathname, searchParams) ??
    resolveSeasonTournamentsRoute(pathname) ??
    resolveTeamEligibilityRoute(pathname, searchParams) ??
    resolveEligibleBowlersRoute(pathname) ??
    resolveBowlerSearchRoute(pathname, searchParams) ??
    resolveStaticGetRoute(pathname) ??
    resolveAdvancementRoute(pathname) ??
    resolveSeriesRoute(pathname) ??
    resolveTournamentDetailRoute(pathname) ??
    resolveSponsorDetailRoute(pathname) ??
    resolveUserDetailRoute(pathname) ??
    (routes[pathname] as object | undefined) ??
    null
  );
}

interface MockOverride {
  status?: number;
  detail?: string;
  delayMs?: number;
}

const mockOverrides = new Map<string, MockOverride>();

// Sponsors created via POST /sponsors during a test run, keyed by slug, so a subsequent
// GET /sponsors/{slug} (e.g. after the create form navigates to the detail page) resolves.
const createdSponsors = new Map<string, object>();

// Tournaments created via POST /tournaments during a test run, keyed by generated ID, so a
// subsequent GET /tournaments/{id} (after the create form navigates to the detail page) resolves.
const createdTournaments = new Map<string, object>();
let nextCreatedTournamentSuffix = 200;

// Looks up display fields for a sponsor by id, checking the seeded active-sponsors list first,
// then sponsors created via POST /sponsors during this test run — used by
// POST /tournaments/{id}/sponsors to fill in the sponsor's name/slug/logo on the tournament's
// sponsor list, since the request body only carries the sponsor id and amount.
function findSponsorMetaById(sponsorId: string | undefined): SponsorMetaFixture | undefined {
  if (!sponsorId) {
    return undefined;
  }

  const activeMatch = MOCK_SPONSORS_ACTIVE.items.find((s) => s.sponsorId === sponsorId);
  if (activeMatch) {
    return activeMatch;
  }

  for (const created of createdSponsors.values()) {
    const candidate = created as { id?: string } & Partial<SponsorMetaFixture>;
    if (candidate.id === sponsorId) {
      return {
        name: candidate.name ?? '',
        slug: candidate.slug ?? '',
        logoUrl: candidate.logoUrl ?? null,
        websiteUrl: candidate.websiteUrl ?? null,
        tagPhrase: candidate.tagPhrase ?? null,
      };
    }
  }

  return undefined;
}

async function handleRequest(req: IncomingMessage, res: ServerResponse): Promise<void> {
  setCorsHeaders(res);

  if (req.method === 'OPTIONS') {
    res.writeHead(200);
    res.end();
    return;
  }

  const requestUrl = new URL(req.url ?? '/', 'http://localhost');
  const pathname = requestUrl.pathname;

  if (req.method === 'POST' && pathname === '/news') {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    const body = await readRequestBody(req);
    const parsed = JSON.parse(body) as { article?: { title?: string; slug?: string } };
    const slug = parsed.article?.slug || slugify(parsed.article?.title ?? '');

    sendJsonResponse(res, { articleId: '01JX0000000000000000000199', slug }, 201);
    return;
  }

  if (req.method === 'POST' && pathname === '/sponsors') {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    const body = await readRequestBody(req);
    const parsed = JSON.parse(body) as {
      sponsor?: {
        name?: string;
        slug?: string;
        isCurrentSponsor?: boolean;
        priority?: number;
        tier?: string;
        category?: string;
        websiteUrl?: string;
        tagPhrase?: string;
        description?: string;
        facebookUrl?: string;
        instagramUrl?: string;
        businessStreet?: string;
        businessUnit?: string;
        businessCity?: string;
        businessState?: string;
        businessPostalCode?: string;
        businessEmailAddress?: string;
        phoneNumbers?: { phoneNumberType: string; phoneNumber: string }[];
      };
    };
    const sponsor = parsed.sponsor ?? {};
    const slug = sponsor.slug || slugify(sponsor.name ?? '');
    const sponsorId = '01JX0000000000000000000199';

    createdSponsors.set(slug, {
      id: sponsorId,
      name: sponsor.name ?? '',
      slug,
      isCurrentSponsor: sponsor.isCurrentSponsor ?? true,
      priority: sponsor.priority ?? 0,
      tier: sponsor.tier ?? 'Standard',
      category: sponsor.category ?? 'Other',
      logoUrl: null,
      websiteUrl: sponsor.websiteUrl ?? null,
      tagPhrase: sponsor.tagPhrase ?? null,
      description: sponsor.description ?? null,
      facebookUrl: sponsor.facebookUrl ?? null,
      instagramUrl: sponsor.instagramUrl ?? null,
      businessStreet: sponsor.businessStreet ?? null,
      businessUnit: sponsor.businessUnit ?? null,
      businessCity: sponsor.businessCity ?? null,
      businessState: sponsor.businessState ?? null,
      businessPostalCode: sponsor.businessPostalCode ?? null,
      businessCountry: null,
      businessEmailAddress: sponsor.businessEmailAddress ?? null,
      phoneNumbers: sponsor.phoneNumbers ?? [],
      tournamentsSponsored: [],
    });

    sendJsonResponse(res, { sponsorId, slug }, 201);
    return;
  }

  if (req.method === 'POST' && pathname === '/oil-patterns') {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    const body = await readRequestBody(req);
    const parsed = JSON.parse(body) as { oilPattern?: { name?: string; length?: number } };
    const oilPattern = parsed.oilPattern ?? {};

    sendJsonResponse(res, {
      oilPatternId: '01JX0000000000000000000299',
      name: oilPattern.name ?? '',
      length: oilPattern.length ?? 0,
      lengthCategory: 'Medium',
      ratioCategory: 'Medium',
    }, 201);
    return;
  }

  if (req.method === 'POST' && pathname === '/security/users') {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    lastRequestBodies.set('POST /security/users', await readRequestBody(req));

    sendJsonResponse(res, { userId: '01JX0000000000000000000399', rolesAssigned: true, permissionOverridesApplied: true }, 201);
    return;
  }

  if (req.method === 'POST' && pathname === '/security/password/reset') {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    await readRequestBody(req);

    res.writeHead(204);
    res.end();
    return;
  }

  if (req.method === 'POST' && pathname === '/security/password/forgot') {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    lastRequestBodies.set('POST /security/password/forgot', await readRequestBody(req));

    // Always 204, whether or not the email "exists", like the real endpoint.
    res.writeHead(204);
    res.end();
    return;
  }

  if (req.method === 'POST' && pathname === '/security/password/change') {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    await readRequestBody(req);

    // Every required ChangePasswordResponse field, or the page fails deterministically.
    sendJsonResponse(res, {
      accessToken: mockJwt(),
      refreshToken: 'mock-refresh-token-after-change',
      expiresAt: '2099-01-01T00:00:00+00:00',
      userId: '01JX0000000000000000000001',
      email: 'e2e-test-user@bowlneba.com',
    });
    return;
  }

  if (req.method === 'POST' && pathname === '/security/password/set-from-token') {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    await readRequestBody(req);

    res.writeHead(204);
    res.end();
    return;
  }

  if (req.method === 'POST' && pathname === '/sponsors/logo') {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    // Body content is discarded — the mock only needs to acknowledge the multipart upload and
    // hand back a StoredFile pointer, same shape as the real UploadSponsorLogo endpoint.
    await readRequestBody(req);

    sendJsonResponse(res, {
      container: 'bowlneba-public',
      path: 'sponsors/logo/e2e-test-logo.png',
      fileName: 'e2e-test-logo.png',
      contentType: 'image/png',
      sizeInBytes: 4,
      url: 'http://localhost:5151/mock-storage/sponsors/logo/e2e-test-logo.png',
    }, 200);
    return;
  }

  if (req.method === 'POST' && pathname === '/tournaments') {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    const body = await readRequestBody(req);
    const parsed = JSON.parse(body) as {
      tournament?: {
        name?: string;
        tournamentType?: string;
        startDate?: string;
        endDate?: string;
        statsEligible?: boolean;
        entryFee?: number;
        externalRegistrationUrl?: string;
        logo?: { container: string; path: string; contentType: string; sizeInBytes: number };
        oilPatternId?: string;
        patternLengthCategory?: string;
        patternRatioCategory?: string;
        previousTournamentId?: string | null;
      };
    };
    const tournament = parsed.tournament ?? {};
    const tournamentId = `01JX000000000000000000${nextCreatedTournamentSuffix++}`;

    // Recorded per tournament name, so specs running in parallel on different projects (each with
    // its own tournament name) can read back the request they sent.
    lastRequestBodies.set('POST /tournaments:' + (tournament.name ?? ''), body);

    createdTournaments.set(tournamentId, {
      id: tournamentId,
      name: tournament.name ?? '',
      season: '2025-2026 Season',
      startDate: tournament.startDate ?? null,
      endDate: tournament.endDate ?? null,
      statsEligible: tournament.statsEligible ?? true,
      status: 'Scheduled',
      titleEligible: false,
      tournamentType: tournament.tournamentType ?? 'Singles',
      entryFee: tournament.entryFee ?? null,
      registrationUrl: tournament.externalRegistrationUrl ?? null,
      addedMoney: null,
      reservations: null,
      entryCount: null,
      patternLengthCategory: tournament.patternLengthCategory ?? null,
      patternRatioCategory: tournament.patternRatioCategory ?? null,
      previousTournamentId: tournament.previousTournamentId ?? null,
      logoUrl: tournament.logo ? `http://localhost:5151/mock-storage/${tournament.logo.path}` : null,
      bowlingCenter: null,
      sponsors: [],
      oilPatterns: [],
      winners: [],
      results: [],
      articles: [],
    });

    sendJsonResponse(res, { tournamentId }, 201);
    return;
  }

  if (req.method === 'POST' && pathname === '/hall-of-fame/photos') {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    // Body content is discarded — the mock only needs to acknowledge the multipart upload and
    // hand back a StoredFile pointer, same shape as the real UploadHallOfFamePhoto endpoint.
    await readRequestBody(req);

    sendJsonResponse(res, {
      container: 'bowlneba-public',
      path: 'hall-of-fame/photo/e2e-test-photo.png',
      fileName: 'e2e-test-photo.png',
      contentType: 'image/png',
      sizeInBytes: 4,
      url: 'http://localhost:5151/mock-storage/hall-of-fame/photo/e2e-test-photo.png',
    }, 200);
    return;
  }

  if (req.method === 'POST' && pathname === '/tournaments/logo') {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    // Body content is discarded — the mock only needs to acknowledge the multipart upload and
    // hand back a StoredFile pointer, same shape as the real UploadTournamentLogo endpoint.
    await readRequestBody(req);

    sendJsonResponse(res, {
      container: 'bowlneba-public',
      path: 'tournaments/logo/e2e-test-logo.png',
      fileName: 'e2e-test-logo.png',
      contentType: 'image/png',
      sizeInBytes: 4,
      url: 'http://localhost:5151/mock-storage/tournaments/logo/e2e-test-logo.png',
    }, 200);
    return;
  }

  if (req.method === 'POST' && pathname.startsWith('/tournaments/') && pathname.endsWith('/sponsors')) {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    const tournamentId = pathname.slice('/tournaments/'.length, -'/sponsors'.length);
    const tournament = resolveGetRoute(`/tournaments/${tournamentId}`, new URLSearchParams()) as
      | { sponsors: TournamentSponsorFixture[] }
      | null;

    if (tournament === null) {
      sendJsonResponse(res, { error: 'Not Found' }, 404);
      return;
    }

    const body = await readRequestBody(req);
    const parsed = JSON.parse(body) as {
      sponsor?: { sponsorId?: string; titleSponsor?: boolean; sponsorshipAmount?: number };
    };
    const sponsorInput = parsed.sponsor ?? {};
    const sponsorMeta = findSponsorMetaById(sponsorInput.sponsorId);

    if (sponsorInput.titleSponsor) {
      tournament.sponsors.forEach((s) => { s.titleSponsor = false; });
    }

    tournament.sponsors.push({
      sponsorId: sponsorInput.sponsorId ?? '',
      name: sponsorMeta?.name ?? 'Unknown Sponsor',
      slug: sponsorMeta?.slug ?? '',
      logoUrl: sponsorMeta?.logoUrl ?? null,
      websiteUrl: sponsorMeta?.websiteUrl ?? null,
      tagPhrase: sponsorMeta?.tagPhrase ?? null,
      titleSponsor: sponsorInput.titleSponsor ?? false,
      sponsorshipAmount: sponsorInput.sponsorshipAmount ?? 0,
    });

    res.writeHead(204);
    res.end();
    return;
  }

  if (req.method === 'PUT' && pathname.startsWith('/tournaments/') && pathname.includes('/sponsors/')) {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    const [tournamentId, , sponsorId] = pathname.slice('/tournaments/'.length).split('/');
    const tournament = resolveGetRoute(`/tournaments/${tournamentId}`, new URLSearchParams()) as
      | { sponsors: TournamentSponsorFixture[] }
      | null;

    const sponsor = tournament?.sponsors.find((s) => s.sponsorId === sponsorId);

    if (tournament === null || sponsor === undefined) {
      sendJsonResponse(res, { error: 'Not Found' }, 404);
      return;
    }

    const body = await readRequestBody(req);
    const parsed = JSON.parse(body) as { sponsor?: { sponsorshipAmount?: number } };
    sponsor.sponsorshipAmount = parsed.sponsor?.sponsorshipAmount ?? sponsor.sponsorshipAmount;

    res.writeHead(204);
    res.end();
    return;
  }

  if (req.method === 'DELETE' && pathname.startsWith('/tournaments/') && pathname.includes('/sponsors/')) {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    const [tournamentId, , sponsorId] = pathname.slice('/tournaments/'.length).split('/');
    const tournament = resolveGetRoute(`/tournaments/${tournamentId}`, new URLSearchParams()) as
      | { sponsors: TournamentSponsorFixture[] }
      | null;

    if (tournament !== null) {
      tournament.sponsors = tournament.sponsors.filter((s) => s.sponsorId !== sponsorId);
    }

    res.writeHead(204);
    res.end();
    return;
  }

  if (req.method === 'DELETE' && pathname.startsWith('/tournaments/') && !pathname.includes('/sponsors/')) {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    res.writeHead(204);
    res.end();
    return;
  }

  if (req.method === 'PATCH' && pathname.startsWith('/tournaments/') && pathname.endsWith('/truncate')) {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    const tournamentId = pathname.slice('/tournaments/'.length, -'/truncate'.length);
    const tournament = resolveGetRoute(`/tournaments/${tournamentId}`, new URLSearchParams()) as
      | { status: string; titleEligible: boolean }
      | null;

    if (tournament !== null) {
      tournament.status = 'Truncated';
      tournament.titleEligible = false;
    }

    res.writeHead(204);
    res.end();
    return;
  }

  if (req.method === 'PATCH' && pathname.startsWith('/tournaments/') && pathname.endsWith('/cancel')) {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    const tournamentId = pathname.slice('/tournaments/'.length, -'/cancel'.length);
    const tournament = resolveGetRoute(`/tournaments/${tournamentId}`, new URLSearchParams()) as
      | { status: string; titleEligible: boolean }
      | null;

    if (tournament !== null) {
      tournament.status = 'Cancelled';
      tournament.titleEligible = false;
    }

    res.writeHead(204);
    res.end();
    return;
  }

  if (req.method === 'DELETE' && pathname.startsWith('/documents/') && pathname.endsWith('/cache')) {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    res.writeHead(204);
    res.end();
    return;
  }

  if (req.method === 'DELETE' && pathname === '/cache') {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    res.writeHead(204);
    res.end();
    return;
  }

  if (req.method === 'POST') {
    if (pathname === '/__mock/fail') {
      const path = requestUrl.searchParams.get('path') ?? '';
      const status = Number.parseInt(requestUrl.searchParams.get('status') ?? '500', 10);
      const detail = requestUrl.searchParams.get('detail') ?? undefined;
      mockOverrides.set(path, { ...mockOverrides.get(path), status, detail });
      res.writeHead(200);
      res.end();
      return;
    }

    if (pathname === '/__mock/delay') {
      const path = requestUrl.searchParams.get('path') ?? '';
      const ms = Number.parseInt(requestUrl.searchParams.get('ms') ?? '0', 10);
      mockOverrides.set(path, { ...mockOverrides.get(path), delayMs: ms });
      res.writeHead(200);
      res.end();
      return;
    }

    if (pathname === '/__mock/reset') {
      const path = requestUrl.searchParams.get('path');
      if (path) {
        mockOverrides.delete(path);
      } else {
        mockOverrides.clear();
      }
      res.writeHead(200);
      res.end();
      return;
    }
  }

  if (req.method === 'POST' && pathname === '/__mock/series-advancement/reset') {
    const seriesId = requestUrl.searchParams.get('seriesId');
    if (seriesId) {
      advanceStates.delete(seriesId);
    } else {
      advanceStates.clear();
    }
    res.writeHead(204);
    res.end();
    return;
  }

  if (req.method === 'POST' && pathname === '/__mock/hall-of-fame/reset') {
    MOCK_HALL_OF_FAME.items = structuredClone(INITIAL_HALL_OF_FAME_ITEMS);
    res.writeHead(204);
    res.end();
    return;
  }

  if (req.method === 'GET' && pathname === '/__mock/last-request') {
    const method = requestUrl.searchParams.get('method') ?? '';
    const path = requestUrl.searchParams.get('path') ?? '';
    const body = lastRequestBodies.get(method + ' ' + path);

    if (body === undefined) {
      sendJsonResponse(res, { error: 'No request recorded' }, 404);
    } else {
      sendJsonResponse(res, JSON.parse(body));
    }
    return;
  }

  if (req.method === 'GET' && pathname.startsWith('/mock-storage/series/')) {
    res.writeHead(200, { 'Content-Type': 'image/svg+xml' });
    res.end(MOCK_HALL_OF_FAME_PHOTO_SVG);
    return;
  }

  if (req.method === 'GET' && pathname.startsWith('/mock-storage/hall-of-fame/')) {
    res.writeHead(200, { 'Content-Type': 'image/svg+xml' });
    res.end(MOCK_HALL_OF_FAME_PHOTO_SVG);
    return;
  }

  if (req.method === 'GET') {
    const delayMs = mockOverrides.get(pathname)?.delayMs;
    if (delayMs) {
      await new Promise<void>((resolve) => setTimeout(resolve, delayMs));
    }

    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    const data = resolveGetRoute(pathname, requestUrl.searchParams);
    if (data !== null) {
      sendJsonResponse(res, data);
      return;
    }
  }

  if (req.method === 'DELETE' && pathname.startsWith('/news/')) {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    res.writeHead(204);
    res.end();
    return;
  }

  if (req.method === 'PUT' && pathname.startsWith('/security/users/')) {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    lastRequestBodies.set('PUT ' + pathname, await readRequestBody(req));

    res.writeHead(204);
    res.end();
    return;
  }

  const hallOfFamePhotoMatch = /^\/hall-of-fame\/([^/]+)\/photo$/.exec(pathname);
  if (req.method === 'PUT' && hallOfFamePhotoMatch) {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    const body = await readRequestBody(req);
    // Recorded so a test can assert on the submitted photo and framing.
    lastRequestBodies.set('PUT ' + pathname, body);

    const induction = MOCK_HALL_OF_FAME.items.find((item) => item.id === hallOfFamePhotoMatch[1]);
    if (!induction) {
      sendJsonResponse(res, { error: 'Not Found' }, 404);
      return;
    }

    // Mirrors EditHallOfFamePhotoCommandHandler: a photo sets or replaces it; no photo and no framing removes it;
    // no photo with framing keeps the current photo. Keep the two in step when the handler's rules change.
    const input = (JSON.parse(body) as {
      input: { photo: { path: string } | null; focalX: number | null; focalY: number | null; zoom: number | null };
    }).input;

    if (input.photo) {
      induction.photoUri = `http://localhost:5151/mock-storage/${input.photo.path}`;
    } else if (input.focalX === null || input.focalX === undefined) {
      induction.photoUri = null;
    }

    induction.photoFocalX = induction.photoUri ? input.focalX ?? induction.photoFocalX : null;
    induction.photoFocalY = induction.photoUri ? input.focalY ?? induction.photoFocalY : null;
    induction.photoZoom = induction.photoUri ? input.zoom ?? induction.photoZoom : null;

    res.writeHead(204);
    res.end();
    return;
  }

  if (req.method === 'PUT' && pathname.startsWith('/tournaments/') && pathname.endsWith('/logo')) {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    // Recorded so a test can assert on the submitted logo (null when the logo was removed).
    lastRequestBodies.set('PUT ' + pathname, await readRequestBody(req));

    res.writeHead(204);
    res.end();
    return;
  }

  if (await handleAdvancementWrite(req, res, pathname, requestUrl)) return;

  if (req.method === 'POST' && pathname === '/series/logo') {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    await readRequestBody(req);

    sendJsonResponse(res, {
      container: 'bowlneba-public',
      path: 'series/logo/e2e-test-logo.png',
      fileName: 'e2e-test-logo.png',
      contentType: 'image/png',
      sizeInBytes: 4,
      url: 'http://localhost:5151/mock-storage/series/logo/e2e-test-logo.png',
    }, 200);
    return;
  }

  if (req.method === 'POST' && pathname === '/series') {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    const body = await readRequestBody(req);
    const parsed = JSON.parse(body) as {
      series?: { name?: string; finale?: { finalistCount?: number } | null };
    };
    const series = parsed.series ?? {};
    // Recorded per Series name, so specs running in parallel on different projects do not overwrite each other.
    lastRequestBodies.set('POST /series:' + (series.name ?? ''), body);

    // The name "fail" stands in for a Stage the server rejects (a 4xx, so the page does not retry).
    if (series.name === 'fail') {
      sendJsonResponse(res, {
        status: 422,
        title: 'One or more validation errors occurred.',
        errors: [{ name: 'Series.Stages[1]', reason: 'This Stage must follow the previous Stage by date.' }],
      }, 422);
      return;
    }

    if (series.finale && !series.finale.finalistCount) {
      sendJsonResponse(res, {
        status: 422,
        title: 'One or more validation errors occurred.',
        errors: [{ name: 'Series.Finale.FinalistCount', reason: 'The Finale needs at least one finalist.' }],
      }, 422);
      return;
    }

    sendJsonResponse(res, { seriesId: MOCK_SERIES_ID }, 201);
    return;
  }

  if (req.method === 'PUT' && pathname.startsWith('/series/') && pathname.endsWith('/logo')) {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    const body = await readRequestBody(req);
    lastRequestBodies.set('PUT ' + pathname, body);

    const seriesId = pathname.slice('/series/'.length, -'/logo'.length);
    const logo = (JSON.parse(body) as { input?: { logo?: { path: string } | null } }).input?.logo;
    seriesLogos.set(seriesId, logo ? `http://localhost:5151/mock-storage/${logo.path}` : null);

    res.writeHead(204);
    res.end();
    return;
  }

  if (req.method === 'PUT' && pathname.startsWith('/series/')) {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    const editBody = await readRequestBody(req);
    lastRequestBodies.set('PUT ' + pathname, editBody);
    removeAdvancementStagesNotSubmitted(pathname.slice('/series/'.length), editBody);

    res.writeHead(204);
    res.end();
    return;
  }

  if (req.method === 'DELETE' && pathname.startsWith('/series/')) {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    res.writeHead(204);
    res.end();
    return;
  }

  if (
    req.method === 'PUT' &&
    (pathname.startsWith('/news/') ||
      pathname.startsWith('/sponsors/') ||
      pathname.startsWith('/tournaments/'))
  ) {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    res.writeHead(204);
    res.end();
    return;
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
