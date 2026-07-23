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

function sendJsonResponse(res: ServerResponse, data: unknown, statusCode = 200): void {
  res.writeHead(statusCode, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify(data));
}

// Sends the mocked error response for a route registered via /__mock/fail and returns
// true if it did, so the caller can bail out of its own handling.
function sendMockOverrideErrorIfSet(res: ServerResponse, pathname: string): boolean {
  const override = mockOverrides.get(pathname);

  if (override?.status != null && override.status >= 400) {
    sendJsonResponse(res, { error: 'Mock error' }, override.status);
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

export const MOCK_SEASON_TOURNAMENTS = {
  items: [
    {
      id: MOCK_TOURNAMENT_ID,
      name: 'NEBA Spring Classic',
      startDate: '2026-03-15',
      endDate: '2026-03-15',
      tournamentType: 'Singles',
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
  ],
};

const MOCK_HALL_OF_FAME = {
  items: [
    { year: 2024, bowlerName: 'Jane Smith', categories: ['Superior Performance'], photoUri: null },
    { year: 2024, bowlerName: 'Bob Johnson', categories: ['Meritorious Service'], photoUri: null },
    { year: 2023, bowlerName: 'Alice Williams', categories: ['Friend of NEBA'], photoUri: null },
    { year: 2023, bowlerName: 'Tom Davis', categories: ['Superior Performance', 'Meritorious Service'], photoUri: null },
  ],
  totalItems: 4,
};

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

const MOCK_TOURNAMENT_TYPES = {
  items: [{ name: 'Singles' }, { name: 'Doubles' }],
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
  oilPatterns: [{ name: 'Scorpion', length: 42, volume: 24.5, leftRatio: 3, rightRatio: 3 }],
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
  oilPatterns: [{ name: 'Scorpion', length: 42, volume: 24.5, leftRatio: 3, rightRatio: 3 }],
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
  oilPatterns: [{ name: 'Scorpion', length: 42, volume: 24.5, leftRatio: 3, rightRatio: 3 }],
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

const EXTRA_TOURNAMENT_DETAILS = new Map<string, object>([
  [MOCK_TOURNAMENT_OIL_REVEAL_PENDING_ID, MOCK_TOURNAMENT_OIL_REVEAL_PENDING],
  [MOCK_TOURNAMENT_OIL_REVEALED_ID, MOCK_TOURNAMENT_OIL_REVEALED],
  [MOCK_TOURNAMENT_OIL_REVEAL_MGMT_ID, MOCK_TOURNAMENT_OIL_REVEAL_MGMT],
  [MOCK_TOURNAMENT_SPONSOR_MGMT_ID, MOCK_TOURNAMENT_SPONSOR_MGMT],
]);

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
        { bowlerId: PRIMARY_BOWLER_ID, bowlerName: 'Current Leader', hallOfFame: true },
      ],
    },
    {
      tournamentId: '01JX0000000000000000000011',
      tournamentName: 'NEBA Fall Doubles',
      tournamentDate: '2024-10-15',
      tournamentType: 'Doubles',
      champions: [
        { bowlerId: PRIMARY_BOWLER_ID, bowlerName: 'Current Leader', hallOfFame: true },
        { bowlerId: SECONDARY_BOWLER_ID, bowlerName: 'Current Rival', hallOfFame: false },
      ],
    },
    {
      tournamentId: '01JX0000000000000000000012',
      tournamentName: 'NEBA Winter Classic',
      tournamentDate: '2023-01-21',
      tournamentType: 'Singles',
      champions: [
        { bowlerId: SECONDARY_BOWLER_ID, bowlerName: 'Current Rival', hallOfFame: false },
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

const routes: Record<string, unknown> = {
  '/health': { status: 'healthy' },
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
  '/awards/bowler-of-the-year': MOCK_BOWLER_OF_THE_YEAR_AWARDS,
  '/awards/high-average': MOCK_HIGH_AVERAGE_AWARDS,
  '/awards/high-block': MOCK_HIGH_BLOCK_AWARDS,
};

function resolveGetRoute(pathname: string, searchParams: URLSearchParams): object | null {
  if (pathname === '/stats') {
    const requestedYear = Number.parseInt(searchParams.get('year') ?? '2025', 10);
    const selectedYear = Number.isFinite(requestedYear) ? requestedYear : 2025;
    return createStatsResponse(selectedYear);
  }

  if (pathname.startsWith('/seasons/') && pathname.endsWith('/tournaments')) {
    const seasonId = pathname.slice('/seasons/'.length, -'/tournaments'.length);
    return seasonId === MOCK_SEASON_ID ? MOCK_SEASON_TOURNAMENTS : null;
  }

  if (pathname === '/tournaments/champions') return MOCK_TOURNAMENT_CHAMPIONS;
  if (pathname === '/tournaments/types') return MOCK_TOURNAMENT_TYPES;
  if (pathname === `/bowlers/${PRIMARY_BOWLER_ID}/titles`) return MOCK_BOWLER_TITLES_CURRENT_LEADER;
  if (pathname === `/bowlers/${SECONDARY_BOWLER_ID}/titles`) return MOCK_BOWLER_TITLES_CURRENT_RIVAL;

  if (pathname.startsWith('/tournaments/')) {
    const tournamentId = pathname.slice('/tournaments/'.length);
    if (tournamentId === MOCK_TOURNAMENT_ID) return MOCK_TOURNAMENT_DETAIL;
    if (EXTRA_TOURNAMENT_DETAILS.has(tournamentId)) return EXTRA_TOURNAMENT_DETAILS.get(tournamentId) ?? null;
    return createdTournaments.get(tournamentId) ?? null;
  }

  if (pathname.startsWith('/sponsors/') && createdSponsors.has(pathname.slice('/sponsors/'.length))) {
    return createdSponsors.get(pathname.slice('/sponsors/'.length)) ?? null;
  }

  return routes[pathname] ?? null;
}

interface MockOverride {
  status?: number;
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
    });

    sendJsonResponse(res, { sponsorId, slug }, 201);
    return;
  }

  if (req.method === 'POST' && pathname === '/oil-patterns') {
    if (sendMockOverrideErrorIfSet(res, pathname)) return;

    const body = await readRequestBody(req);
    const parsed = JSON.parse(body) as { name?: string; length?: number };

    sendJsonResponse(res, {
      oilPatternId: '01JX0000000000000000000299',
      name: parsed.name ?? '',
      length: parsed.length ?? 0,
      lengthCategory: 'Medium',
      ratioCategory: 'Medium',
    }, 201);
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
      };
    };
    const tournament = parsed.tournament ?? {};
    const tournamentId = `01JX000000000000000000${nextCreatedTournamentSuffix++}`;

    createdTournaments.set(tournamentId, {
      id: tournamentId,
      name: tournament.name ?? '',
      season: '2025-2026 Season',
      startDate: tournament.startDate ?? null,
      endDate: tournament.endDate ?? null,
      statsEligible: tournament.statsEligible ?? true,
      tournamentType: tournament.tournamentType ?? 'Singles',
      entryFee: tournament.entryFee ?? null,
      registrationUrl: tournament.externalRegistrationUrl ?? null,
      addedMoney: null,
      reservations: null,
      entryCount: null,
      patternLengthCategory: tournament.patternLengthCategory ?? null,
      patternRatioCategory: tournament.patternRatioCategory ?? null,
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

  if (req.method === 'POST') {
    if (pathname === '/__mock/fail') {
      const path = requestUrl.searchParams.get('path') ?? '';
      const status = Number.parseInt(requestUrl.searchParams.get('status') ?? '500', 10);
      mockOverrides.set(path, { ...mockOverrides.get(path), status });
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

  if (req.method === 'PUT' && (pathname.startsWith('/news/') || pathname.startsWith('/sponsors/'))) {
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
