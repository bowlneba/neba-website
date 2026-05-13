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
};

const MOCK_SPONSOR_OLD_SPONSOR = {
  id: '01JX0000000000000000000002',
  name: 'Old Sponsor',
  slug: 'old-sponsor',
  isCurrentSponsor: false,
  priority: 99,
  tier: 'Standard',
  category: 'Other',
  logoUrl: null,
  websiteUrl: null,
  tagPhrase: null,
  description: null,
  promotionalNotes: null,
  liveReadText: null,
  facebookUrl: null,
  instagramUrl: null,
  businessStreet: null,
  businessUnit: null,
  businessCity: null,
  businessState: null,
  businessPostalCode: null,
  businessCountry: null,
  businessEmailAddress: null,
  phoneNumbers: [],
  sponsorContactName: null,
  sponsorContactEmailAddress: null,
  sponsorContactPhoneNumber: null,
  sponsorContactPhoneNumberType: null,
};

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

export const MOCK_TOURNAMENT_DETAIL = {
  id: MOCK_TOURNAMENT_ID,
  name: 'NEBA Spring Classic',
  season: {
    id: '01JX0000000000000000000011',
    description: '2024-2025 Season',
    startDate: '2024-09-01',
    endDate: '2025-05-31',
  },
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
  sponsors: [],
  oilPatterns: [{ name: 'Scorpion', length: 42, volume: 24.5, leftRatio: 3, rightRatio: 3 }],
  winners: ['Current Leader'],
  results: [],
};

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

const routes: Record<string, unknown> = {
  '/health': { status: 'healthy' },
  '/documents/tournament-rules': { html: MOCK_TOURNAMENT_RULES_HTML },
  '/documents/bylaws': { html: MOCK_BYLAWS_HTML },
  '/bowling-centers': MOCK_BOWLING_CENTERS,
  '/seasons': MOCK_SEASONS,
  '/sponsors': MOCK_SPONSORS_ACTIVE,
  '/sponsors/pro-shop-plus': MOCK_SPONSOR_PRO_SHOP_PLUS,
  '/sponsors/old-sponsor': MOCK_SPONSOR_OLD_SPONSOR,
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

  if (pathname.startsWith('/tournaments/')) {
    const tournamentId = pathname.slice('/tournaments/'.length);
    return tournamentId === MOCK_TOURNAMENT_ID ? MOCK_TOURNAMENT_DETAIL : null;
  }

  return routes[pathname] ?? null;
}

function handleRequest(req: IncomingMessage, res: ServerResponse): void {
  setCorsHeaders(res);

  if (req.method === 'OPTIONS') {
    res.writeHead(200);
    res.end();
    return;
  }

  if (req.method === 'GET') {
    const requestUrl = new URL(req.url ?? '/', 'http://localhost');
    const data = resolveGetRoute(requestUrl.pathname, requestUrl.searchParams);
    if (data !== null) {
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
