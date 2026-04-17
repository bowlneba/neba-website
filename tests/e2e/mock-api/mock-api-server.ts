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
  id: '01JXXXXXXXXXXXXXXXXXXXXXXXXX',
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
  id: '01JXXXXXXXXXXXXXXXXXXXXXXXXY',
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

const PRIMARY_BOWLER_ID = '01JX1111111111111111111111';
const SECONDARY_BOWLER_ID = '01JX2222222222222222222222';

function createStatsResponse(selectedYear: number): unknown {
  const isLegacySeason = selectedYear === 2021;
  const selectedSeasonLabel = isLegacySeason ? '2020-2021 Season' : '2024-2025 Season';
  const leaderName = isLegacySeason ? 'Legacy Leader' : 'Current Leader';
  const rivalName = isLegacySeason ? 'Legacy Rival' : 'Current Rival';
  const points = isLegacySeason ? 184 : 267;
  const average = isLegacySeason ? 219.35 : 228.42;
  const winnings = isLegacySeason ? 1320 : 2430;

  return {
    selectedSeason: selectedSeasonLabel,
    availableSeasons: {
      2025: '2024-2025 Season',
      2021: '2020-2021 Season',
    },
    bowlerSearchList: {
      [PRIMARY_BOWLER_ID]: leaderName,
      [SECONDARY_BOWLER_ID]: rivalName,
    },
    minimumNumberOfGames: 20,
    minimumNumberOfTournaments: 4,
    minimumNumberOfEntries: 6,
    bowlerOfTheYear: [
      {
        bowlerId: PRIMARY_BOWLER_ID,
        bowlerName: leaderName,
        points,
        tournaments: 7,
        entries: 8,
        finals: 5,
        averageFinish: 6.4,
        winnings,
      },
    ],
    seniorOfTheYear: [],
    superSeniorOfTheYear: [],
    womanOfTheYear: [],
    rookieOfTheYear: [],
    youthOfTheYear: [],
    highAverage: [
      {
        bowlerId: PRIMARY_BOWLER_ID,
        bowlerName: leaderName,
        average,
        games: 35,
        tournaments: 7,
        fieldAverage: 9.2,
      },
    ],
    highBlock: [
      {
        bowlerId: PRIMARY_BOWLER_ID,
        bowlerName: leaderName,
        highBlock: isLegacySeason ? 1120 : 1198,
        highGame: isLegacySeason ? 279 : 289,
      },
    ],
    matchPlayAverage: [
      {
        bowlerId: PRIMARY_BOWLER_ID,
        bowlerName: leaderName,
        matchPlayAverage: isLegacySeason ? 218.5 : 226.8,
        games: 18,
        wins: 12,
        losses: 6,
        winPercentage: 66.7,
        winnings,
      },
    ],
    matchPlayRecord: [
      {
        bowlerId: PRIMARY_BOWLER_ID,
        bowlerName: leaderName,
        wins: 12,
        losses: 6,
        winPercentage: 66.7,
        finals: 5,
        matchPlayAverage: isLegacySeason ? 218.5 : 226.8,
        winnings,
      },
    ],
    matchPlayAppearances: [
      {
        bowlerId: PRIMARY_BOWLER_ID,
        bowlerName: leaderName,
        finals: 5,
        tournaments: 7,
        entries: 8,
      },
    ],
    pointsPerEntry: [
      {
        bowlerId: PRIMARY_BOWLER_ID,
        bowlerName: leaderName,
        pointsPerEntry: isLegacySeason ? 23 : 33.38,
        points,
        entries: 8,
      },
    ],
    pointsPerTournament: [
      {
        bowlerId: PRIMARY_BOWLER_ID,
        bowlerName: leaderName,
        points,
        tournaments: 7,
        pointsPerTournament: isLegacySeason ? 26.29 : 38.14,
      },
    ],
    finalsPerEntry: [
      {
        bowlerId: PRIMARY_BOWLER_ID,
        bowlerName: leaderName,
        finals: 5,
        entries: 8,
        finalsPerEntry: 0.63,
      },
    ],
    averageFinishes: [
      {
        bowlerId: PRIMARY_BOWLER_ID,
        bowlerName: leaderName,
        averageFinish: 6.4,
        finals: 5,
        winnings,
      },
    ],
    seasonAtAGlance: {
      totalEntries: isLegacySeason ? 152 : 194,
      totalPrizeMoney: isLegacySeason ? 18800 : 25400,
    },
    seasonsBests: {
      highGame: isLegacySeason ? 289 : 300,
      highGameBowlers: {
        [PRIMARY_BOWLER_ID]: leaderName,
      },
      highBlock: isLegacySeason ? 1189 : 1264,
      highBlockBowlers: {
        [PRIMARY_BOWLER_ID]: leaderName,
      },
      highAverage: average,
      highAverageBowlers: {
        [PRIMARY_BOWLER_ID]: leaderName,
      },
    },
    fieldMatchPlaySummary: {
      highestWinPercentage: 66.7,
      highestWinPercentageBowlers: {
        [PRIMARY_BOWLER_ID]: leaderName,
      },
      mostFinals: 5,
      mostFinalsBowlers: {
        [PRIMARY_BOWLER_ID]: leaderName,
      },
    },
    bowlerOfTheYearPointsRace: [
      {
        bowlerId: PRIMARY_BOWLER_ID,
        bowlerName: leaderName,
        results: [
          {
            tournamentName: 'Season Opener',
            tournamentDate: isLegacySeason ? '2020-09-19' : '2024-09-21',
            cumulativePoints: isLegacySeason ? 32 : 45,
          },
          {
            tournamentName: 'Winter Classic',
            tournamentDate: isLegacySeason ? '2021-01-16' : '2025-01-18',
            cumulativePoints: points,
          },
        ],
      },
    ],
    allBowlers: [
      {
        bowlerId: PRIMARY_BOWLER_ID,
        bowlerName: leaderName,
        points,
        average,
        games: 35,
        finals: 5,
        wins: 12,
        losses: 6,
        winPercentage: 66.7,
        matchPlayAverage: isLegacySeason ? 218.5 : 226.8,
        winnings,
        fieldAverage: 9.2,
        tournaments: 7,
      },
      {
        bowlerId: SECONDARY_BOWLER_ID,
        bowlerName: rivalName,
        points: points - 40,
        average: average - 4,
        games: 31,
        finals: 3,
        wins: 8,
        losses: 7,
        winPercentage: 53.3,
        matchPlayAverage: (isLegacySeason ? 218.5 : 226.8) - 3,
        winnings: winnings - 600,
        fieldAverage: 3.4,
        tournaments: 6,
      },
    ],
  };
}

const routes: Record<string, unknown> = {
  '/health': { status: 'healthy' },
  '/documents/tournament-rules': { html: MOCK_TOURNAMENT_RULES_HTML },
  '/documents/bylaws': { html: MOCK_BYLAWS_HTML },
  '/bowling-centers': MOCK_BOWLING_CENTERS,
  '/sponsors': MOCK_SPONSORS_ACTIVE,
  '/sponsors/pro-shop-plus': MOCK_SPONSOR_PRO_SHOP_PLUS,
  '/sponsors/old-sponsor': MOCK_SPONSOR_OLD_SPONSOR,
};

function handleRequest(req: IncomingMessage, res: ServerResponse): void {
  setCorsHeaders(res);

  if (req.method === 'OPTIONS') {
    res.writeHead(200);
    res.end();
    return;
  }

  if (req.method === 'GET') {
    const requestUrl = new URL(req.url ?? '/', 'http://localhost');

    if (requestUrl.pathname === '/stats') {
      const requestedYear = Number.parseInt(requestUrl.searchParams.get('year') ?? '2025', 10);
      const selectedYear = Number.isFinite(requestedYear) ? requestedYear : 2025;
      sendJsonResponse(res, createStatsResponse(selectedYear));
      return;
    }

    const data = routes[requestUrl.pathname];
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
