using Neba.Api.Contracts.Memberships;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.TestFactory.Series;

/// <summary>
/// The invented replay scenario on the 2025 Stage layout, ready to save. Bowlers are keyed by scenario number (1 to 77, with
/// gaps); a bowler's id sorts in number order, so a BowlerId tie-break is a tie-break by number.
/// </summary>
/// <param name="Season">The season every tournament belongs to.</param>
/// <param name="Bowlers">Each bowler by scenario number, with memberships.</param>
/// <param name="Tournaments">The eleven tournaments in Stage order, all Completed, with results and (for the Survivor Stages) one squad.</param>
/// <param name="SquadScores">Every game bowled on the two Survivor squads.</param>
/// <param name="Series">The Series with its eleven Pending Stages.</param>
public sealed record Series2025Replay(
    Season Season,
    IReadOnlyDictionary<int, Bowler> Bowlers,
    IReadOnlyList<Tournament> Tournaments,
    IReadOnlyList<SquadScore> SquadScores,
    SeriesAggregate Series);

/// <summary>The 2025 reference configuration from #258, as tournaments plus Stage definitions.</summary>
public static class Series2025ReferenceFactory
{
    private static readonly TimeOnly SurvivorSquadStart = new(9, 0);

    // Name, start, end, kind, advancing count, squad date.
    private static readonly (string Name, DateOnly Start, DateOnly End, StageKind Kind, int? Count, DateOnly? Squad)[] Rows =
    [
        ("Callahan's Bowl-O-Rama", new(2025, 1, 24), new(2025, 1, 25), StageKind.Qualifying, 8, null),
        ("Trios Team Event", new(2025, 2, 21), new(2025, 2, 21), StageKind.Qualifying, 2, null),
        ("NEBA Doubles, Baker Finals", new(2025, 2, 22), new(2025, 2, 22), StageKind.Qualifying, 4, null),
        ("Sparetime", new(2025, 3, 22), new(2025, 3, 22), StageKind.Qualifying, 8, null),
        ("BuddiesProShop.com Open", new(2025, 4, 26), new(2025, 4, 26), StageKind.Qualifying, 8, null),
        ("Westgate Lanes", new(2025, 5, 31), new(2025, 5, 31), StageKind.Qualifying, 8, null),
        ("Over 50 / Under Baker Doubles", new(2025, 6, 13), new(2025, 6, 13), StageKind.Qualifying, 4, null),
        ("Singles, Cranston", new(2025, 6, 14), new(2025, 6, 14), StageKind.Qualifying, 8, null),
        ("Callahan's Bowl-O-Rama Survivor", new(2025, 7, 12), new(2025, 7, 12), StageKind.Survivor, 40, new(2025, 7, 12)),
        ("Lang's Singles Open", new(2025, 8, 23), new(2025, 8, 23), StageKind.Survivor, 20, new(2025, 8, 23)),
        ("East Providence Singles", new(2025, 9, 13), new(2025, 9, 13), StageKind.Placement, null, null),
    ];

    /// <summary>Builds the reference configuration, with or without a Finale.</summary>
    /// <param name="withFinale">True: a Finale of 4 finalists on 9/27/2025 at 2:00 PM with no host center yet. False: the same Stages and no Finale.</param>
    public static (Season Season, IReadOnlyList<Tournament> Tournaments, IReadOnlyList<SeriesStageDefinition> Stages, SeriesFinaleDefinition? Finale) Create(bool withFinale = true)
    {
        var season = SeasonFactory.Create();

        var tournaments = Rows
            .Select(row => TournamentFactory.Create(
                name: row.Name,
                startDate: row.Start,
                endDate: row.End,
                seasonId: season.Id))
            .ToList();

        var stages = Rows
            .Select((row, index) => SeriesStageDefinitionFactory.Create(
                kind: row.Kind,
                tournamentId: tournaments[index].Id,
                advancingCount: row.Count,
                squadDate: row.Squad,
                squadStartTime: row.Squad is null ? null : SurvivorSquadStart))
            .ToList();

        return (season, tournaments, stages, withFinale ? SeriesFinaleDefinitionFactory.Create() : null);
    }

    /// <summary>
    /// Builds the 2025 Stage layout over invented bowlers, results, memberships, Bowler of the Year points and Survivor squad
    /// scores. The values are fixed (no randomness) so a test can state its expected output by hand. Calling it with no
    /// arguments gives a Series that saves, with every tournament Completed and every Stage Pending.
    /// </summary>
    /// <param name="withFinale">True: a Finale of 4 finalists. False: no Finale.</param>
    public static Series2025Replay CreateWithResults(bool withFinale = true)
    {
        var season = SeasonFactory.Create();
        var bowlers = ReplayBowlerNumbers.ToDictionary(number => number, CreateReplayBowler);

        var tournaments = new List<Tournament>();
        var squadScores = new List<SquadScore>();

        for (var index = 0; index < Rows.Length; index++)
        {
            var row = Rows[index];
            var squads = row.Squad is { } date
                ? new[] { SquadFactory.Create(new DateTimeOffset(date.Year, date.Month, date.Day, 13, 0, 0, TimeSpan.Zero)) }
                : null;

            var tournament = TournamentFactory.Create(
                name: row.Name,
                tournamentType: TournamentTypes[index],
                startDate: row.Start,
                endDate: row.End,
                seasonId: season.Id,
                squads: squads,
                status: TournamentStatus.Completed,
                results: [.. ResultRows[index].Select(result => TournamentResultFactory.Create(
                    bowlers[result.Bowler].Id, result.Place, points: result.Points))]);

            tournaments.Add(tournament);

            if (index == FirstSurvivorIndex)
            {
                squadScores.AddRange(SquadGames(tournament.Squads.Single().Id, bowlers, FirstSurvivorGames, FirstSurvivorDidNotBowl));
            }
            else if (index == SecondSurvivorIndex)
            {
                squadScores.AddRange(SquadGames(tournament.Squads.Single().Id, bowlers, SecondSurvivorGames, SecondSurvivorDidNotBowl));
            }
        }

        var series = SeriesFactory.CreateFor(
            season,
            [.. Rows.Select((row, index) => (
                tournaments[index],
                row.Kind,
                row.Count,
                row.Squad,
                row.Squad is null ? (TimeOnly?)null : SurvivorSquadStart))],
            withFinale ? SeriesFinaleDefinitionFactory.Create() : null);

        return new Series2025Replay(season, bowlers, tournaments, squadScores, series);
    }

    private const int FirstSurvivorIndex = 8;
    private const int SecondSurvivorIndex = 9;

    private static readonly DateOnly YearStart = new(2025, 1, 1);
    private static readonly DateOnly YearEnd = new(2025, 12, 31);

    // Scenario numbers of the invented bowlers. 39 and 40 are never used.
    private static readonly int[] ReplayBowlerNumbers =
    [
        .. Enumerable.Range(1, 38),
        .. Enumerable.Range(41, 7),
        .. Enumerable.Range(50, 10),
        .. Enumerable.Range(61, 7),
        .. Enumerable.Range(71, 7),
    ];

    private static readonly TournamentType[] TournamentTypes =
    [
        TournamentType.Singles,
        TournamentType.Trios,
        TournamentType.Doubles,
        TournamentType.Singles,
        TournamentType.Singles,
        TournamentType.Singles,
        TournamentType.Doubles,
        TournamentType.Singles,
        TournamentType.Singles,
        TournamentType.Singles,
        TournamentType.Singles,
    ];

    // Bowler number, place, Bowler of the Year points, per tournament in Stage order. A team event gives each teammate the
    // team's place. The points are invented and need not follow the place.
    private static readonly (int Bowler, int Place, int Points)[][] ResultRows =
    [
        // Stage 1, 8 advance: a tie at 8 gives 9 slots; bowler 4 is not yet a member on 1/25.
        [(1, 1, 100), (2, 2, 90), (3, 3, 80), (4, 4, 70), (5, 5, 60), (6, 6, 50), (7, 7, 40), (8, 8, 30), (9, 8, 30), (10, 10, 20), (11, 11, 15), (12, 12, 10)],
        // Stage 2, trios, 2 teams advance (6 slots); bowler 13 never joins.
        [(14, 1, 60), (15, 1, 60), (16, 1, 60), (13, 2, 50), (17, 2, 50), (18, 2, 50), (19, 3, 40), (20, 3, 40), (21, 3, 40)],
        // Stage 3, doubles, 4 teams advance (8 slots); bowler 1 already qualified.
        [(22, 1, 50), (23, 1, 50), (24, 2, 45), (25, 2, 45), (1, 3, 40), (26, 3, 40), (27, 4, 35), (28, 4, 35), (29, 5, 30), (30, 5, 30), (50, 6, 15)],
        // Stage 4, 8 advance; bowler 38 joins on the event's last day. Bowler 59's membership ends 5/31.
        [(31, 1, 100), (32, 2, 90), (33, 3, 80), (34, 4, 70), (35, 5, 60), (36, 6, 50), (37, 7, 40), (38, 8, 30), (10, 9, 20), (11, 10, 10), (59, 11, 45)],
        // Stage 5, 8 advance; bowler 5 already qualified.
        [(41, 1, 100), (42, 2, 90), (43, 3, 80), (5, 4, 70), (44, 5, 60), (45, 6, 50), (46, 7, 40), (47, 8, 30), (10, 9, 20), (12, 10, 10), (59, 11, 45)],
        // Stage 6, 8 advance; bowler 50 finishes 9th.
        [(51, 1, 100), (52, 2, 90), (53, 3, 80), (54, 4, 70), (55, 5, 60), (56, 6, 50), (57, 7, 40), (58, 8, 30), (50, 9, 20), (11, 10, 10)],
        // Stage 7, doubles, 4 teams advance (8 slots); bowler 6 already qualified.
        [(61, 1, 50), (62, 1, 50), (63, 2, 45), (64, 2, 45), (65, 3, 40), (66, 3, 40), (67, 4, 35), (6, 4, 35), (29, 5, 30), (30, 5, 30)],
        // Stage 8, 8 advance, the Backfill date 6/14; bowler 76 never joins, bowler 31 already qualified.
        [(71, 1, 100), (72, 2, 90), (73, 3, 80), (74, 4, 70), (75, 5, 60), (76, 6, 50), (77, 7, 40), (31, 8, 30), (10, 9, 20), (12, 10, 15)],
        // Stages 9 and 10 (Survivor): a result row must exist; the squad scores decide who advances.
        [(1, 1, 10), (2, 2, 10), (3, 3, 10)],
        [(1, 1, 10), (2, 2, 10), (4, 3, 10)],
        // Stage 11 (Placement). Bowlers 33 and 54 are not in the field. Bowlers 8, 11, 17, 18 and 27 have no row.
        [(20, 1, 10), (7, 2, 10), (9, 2, 10), (1, 4, 10), (23, 5, 10), (14, 6, 10), (5, 7, 10), (12, 8, 10), (26, 9, 10), (2, 10, 10), (21, 11, 10), (16, 12, 10), (10, 13, 10), (6, 14, 10), (25, 15, 10), (33, 16, 10), (4, 17, 10), (22, 18, 10), (19, 19, 10), (54, 20, 10)],
    ];

    // Total pins for the first Survivor squad: 1500 less 10 a bowler number, over 6 games, except the pins below.
    private static readonly IReadOnlySet<int> FirstSurvivorDidNotBowl = new HashSet<int> { 15, 28, 45, 62, 75 };

    private static readonly IReadOnlySet<int> SecondSurvivorDidNotBowl = new HashSet<int> { 3, 24, 38 };

    private static (int Total, int Games) FirstSurvivorGames(int number)
        => number switch
        {
            13 => (1600, 6), // not in the field: never advances
            59 => (1590, 6), // not a member on 6/14: never in the field
            76 => (1580, 6), // never a member: never in the field
            33 => (700, 3), // a partial block counts as bowled, and loses
            51 => (1000, 6), // ties the total of bowler 50
            53 => (980, 6), // ties the total of bowler 52
            _ => (1500 - (10 * number), 6),
        };

    // Total pins for the second Survivor squad: 1400 less 9 a bowler number, over 6 games, except the pins below.
    private static (int Total, int Games) SecondSurvivorGames(int number)
        => number switch
        {
            54 => (1600, 6), // eliminated in the first Survivor Stage: never returns
            33 => (1590, 6), // eliminated in the first Survivor Stage: never returns
            13 => (1580, 6), // not in the field
            25 => (1193, 6), // ties the total of bowler 23
            27 => (1166, 6), // ties the total of bowler 26
            _ => (1400 - (9 * number), 6),
        };

    private static List<SquadScore> SquadGames(
        SquadId squadId,
        Dictionary<int, Bowler> bowlers,
        Func<int, (int Total, int Games)> pins,
        IReadOnlySet<int> didNotBowl)
    {
        var scores = new List<SquadScore>();

        foreach (var (number, bowler) in bowlers.Where(pair => !didNotBowl.Contains(pair.Key)).OrderBy(pair => pair.Key))
        {
            var (total, games) = pins(number);

            // Whole pins per game, the remainder going to the first games, so the games sum to the total.
            for (var game = 0; game < games; game++)
            {
                var score = (total / games) + (game < total % games ? 1 : 0);
                scores.Add(SquadScoreFactory.Create(squadId, bowler.Id, (short)(game + 1), score));
            }
        }

        return scores;
    }

    private static BowlerId ReplayBowlerId(int number)
    {
        // One timestamp and the number in the last byte, so ids sort in number order.
        var random = new byte[10];
        random[9] = (byte)number;

        return new BowlerId(Ulid.NewUlid(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), random).ToString());
    }

    private static Name ReplayName(int number)
        => number switch
        {
            8 => NameFactory.Create(firstName: "Pat", lastName: "Quinn"),
            11 => NameFactory.Create(firstName: "Sam", lastName: "Baker"),
            17 or 18 => NameFactory.Create(firstName: "Zoe", lastName: "Adler"), // identical names: the BowlerId breaks the tie
            27 => NameFactory.Create(firstName: "Lee", lastName: "Moss"),
            _ => NameFactory.Create(firstName: "Replay", lastName: $"Bowler{number:00}"),
        };

    private static Bowler CreateReplayBowler(int number)
    {
        var bowler = BowlerFactory.Create(
            id: ReplayBowlerId(number),
            name: ReplayName(number),
            dateOfBirth: new DateOnly(1980, 1, 1).AddDays(number));

        // Nearly everyone is a member all year. 13 and 76 never join.
        var window = number switch
        {
            13 or 76 => ((DateOnly, DateOnly)?)null,
            4 => (new DateOnly(2025, 3, 1), YearEnd),
            38 => (new DateOnly(2025, 3, 22), YearEnd),
            59 => (YearStart, new DateOnly(2025, 5, 31)),
            _ => (YearStart, YearEnd),
        };

        if (window is { } membership)
        {
            var added = bowler.AddLegacyMembership(number, MembershipType.New, 25m, membership.Item1, membership.Item2);

            if (added.IsError)
            {
                throw new InvalidOperationException($"Failed to add membership: {added.Errors[0].Description}");
            }
        }

        return bowler;
    }
}