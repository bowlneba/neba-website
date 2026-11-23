namespace Neba.Api.Tests.Features.Series;

/// <summary>One Qualifying Stage's expected output: the slot count and each (bowler number, place) it put in the field.</summary>
internal sealed record ExpectedQualifyingStage(int SlotCount, IReadOnlyList<(int Bowler, int Place)> Entries);

/// <summary>
/// What the synthetic 2025 replay must produce, worked out by hand from the rules (see <c>Series2025ReferenceFactory.CreateWithResults</c>
/// for the inputs). Bowlers are scenario numbers; a BowlerId tie-break is a tie-break by number.
/// </summary>
/// <remarks>
/// Rules applied. Qualifying: every row at Place up to N is a slot; a non-member on the tournament's end date or a bowler
/// already in the field leaves the slot vacant. Backfill: the vacancies of all Qualifying Stages go to non-field members on
/// 6/14 with Bowler of the Year points above zero through 6/14, highest first, every tie at the cutoff advancing, anyone
/// barred by an applied Clear excluded. Survivor: only field bowlers advance, by squad total, the top N plus every tie at N.
/// Placement: place, then number; bowlers with no row follow by last name, first name, number.
/// </remarks>
internal sealed record Series2025ReplayExpected(
    IReadOnlyList<ExpectedQualifyingStage> Qualifying,
    IReadOnlyList<(int Bowler, int Points)> Backfill,
    int FieldSize,
    IReadOnlyList<int> FirstSurvivorAdvanced,
    IReadOnlyList<int> SecondSurvivorAdvanced,
    IReadOnlyList<int> Positions,
    int BowledPositions)
{
    /// <summary>Last Qualifying tournament's end date.</summary>
    public static readonly DateOnly BackfillAsOf = new(2025, 6, 14);

    /// <summary>In the field and not on the first Survivor squad.</summary>
    public static readonly IReadOnlyList<int> FirstSurvivorDidNotBowl = [15, 28, 45, 62, 75];

    /// <summary>In the field entering the second Survivor Stage and not on its squad.</summary>
    public static readonly IReadOnlyList<int> SecondSurvivorDidNotBowl = [3, 24, 38];

    /// <summary>Block totals worth pinning on the first squad: a winner, a partial block, and the tie at 40th or 41st.</summary>
    public static readonly IReadOnlyDictionary<int, int> FirstSurvivorTotals = new Dictionary<int, int>
    {
        [1] = 1490,
        [33] = 700,
        [50] = 1000,
        [51] = 1000,
        [52] = 980,
        [53] = 980,
    };

    /// <summary>Block totals worth pinning on the second squad: a winner and the two ties.</summary>
    public static readonly IReadOnlyDictionary<int, int> SecondSurvivorTotals = new Dictionary<int, int>
    {
        [1] = 1391,
        [23] = 1193,
        [25] = 1193,
        [26] = 1166,
        [27] = 1166,
    };

    private static readonly IReadOnlyList<ExpectedQualifyingStage> QualifyingBeforeOverrides =
    [
        // 9 slots (tie at 8); bowler 4 was not a member on 1/25, so one vacancy.
        new(9, [(1, 1), (2, 2), (3, 3), (5, 5), (6, 6), (7, 7), (8, 8), (9, 8)]),
        // 6 slots (2 teams of 3); bowler 13 never joined, so one vacancy.
        new(6, [(14, 1), (15, 1), (16, 1), (17, 2), (18, 2)]),
        // 8 slots (4 teams of 2); bowler 1 already qualified, so one vacancy.
        new(8, [(22, 1), (23, 1), (24, 2), (25, 2), (26, 3), (27, 4), (28, 4)]),
        // 8 slots; bowler 38 joined on the last day of the event and counts.
        new(8, [(31, 1), (32, 2), (33, 3), (34, 4), (35, 5), (36, 6), (37, 7), (38, 8)]),
        // 8 slots; bowler 5 already qualified, so one vacancy.
        new(8, [(41, 1), (42, 2), (43, 3), (44, 5), (45, 6), (46, 7), (47, 8)]),
        new(8, [(51, 1), (52, 2), (53, 3), (54, 4), (55, 5), (56, 6), (57, 7), (58, 8)]),
        // 8 slots (4 teams of 2); bowler 6 already qualified, so one vacancy.
        new(8, [(61, 1), (62, 1), (63, 2), (64, 2), (65, 3), (66, 3), (67, 4)]),
        // 8 slots; bowler 76 never joined and bowler 31 already qualified, so two vacancies.
        new(8, [(71, 1), (72, 2), (73, 3), (74, 4), (75, 5), (77, 7)]),
    ];

    /// <summary>
    /// With no override. Seven vacancies. Backfill candidates by points: 10 (80), 4 (70), 29 and 30 (60), 19, 20 and 21
    /// (40), 11, 12 and 50 (35). The 7th is bowler 21 at 40, so the cutoff is 40. Bowlers 13 and 76 (never members) and 59
    /// (not a member on 6/14, 90 points) are not candidates.
    /// </summary>
    public static readonly Series2025ReplayExpected BeforeOverrides = new(
        QualifyingBeforeOverrides,
        [(10, 80), (4, 70), (29, 60), (30, 60), (19, 40), (20, 40), (21, 40)],
        FieldSize: 63,
        // 63 in the field, 5 did not bowl and 33 bowled 3 games. 40th is 52 (980) and 53 ties it.
        FirstSurvivorAdvanced:
        [
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 14, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 29, 30,
            31, 32, 34, 35, 36, 37, 38, 41, 42, 43, 44, 46, 47, 51, 52, 53,
        ],
        // 3, 24 and 38 did not bowl. 20th is 26 (1166) and 27 ties it.
        SecondSurvivorAdvanced: [1, 2, 4, 5, 6, 7, 8, 9, 10, 14, 16, 17, 18, 19, 20, 21, 22, 23, 25, 26, 27],
        // 17 bowled (26 is in). Not bowled: 17 and 18 (same name, number order), 27 Moss, 8 Quinn.
        Positions: [20, 7, 9, 1, 23, 14, 5, 26, 2, 21, 16, 10, 6, 25, 4, 22, 19, 17, 18, 27, 8],
        BowledPositions: 17);

    /// <summary>
    /// After an Add of bowler 50 on Stage 6 (9th, an extra entry that fills no slot) and a Clear of bowler 42 on Stage 5.
    /// Eight vacancies, and bowler 42 (90 points) is barred. Candidates: 10 (80), 4 (70), 29 and 30 (60), 19, 20, 21 (40),
    /// 11 and 12 (35); bowler 50 is in the field. The 8th is bowler 11 at 35, so the cutoff is 35 and bowler 12 ties it.
    /// </summary>
    public static readonly Series2025ReplayExpected AfterOverrides = new(
        [
            .. QualifyingBeforeOverrides.Take(4),
            new(8, [(41, 1), (43, 3), (44, 5), (45, 6), (46, 7), (47, 8)]),
            new(8, [(51, 1), (52, 2), (53, 3), (54, 4), (55, 5), (56, 6), (57, 7), (58, 8), (50, 9)]),
            .. QualifyingBeforeOverrides.Skip(6),
        ],
        [(10, 80), (4, 70), (29, 60), (30, 60), (19, 40), (20, 40), (21, 40), (11, 35), (12, 35)],
        FieldSize: 65,
        // 65 in the field. 40th is 50 (1000) and 51 ties it.
        FirstSurvivorAdvanced:
        [
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 14, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27,
            29, 30, 31, 32, 34, 35, 36, 37, 38, 41, 43, 44, 46, 47, 50, 51,
        ],
        // 20th is 23 (1193) and 25 ties it.
        SecondSurvivorAdvanced: [1, 2, 4, 5, 6, 7, 8, 9, 10, 11, 12, 14, 16, 17, 18, 19, 20, 21, 22, 23, 25],
        // 12 bowled 8th. Not bowled: 17, 18, 11 Baker, 8 Quinn.
        Positions: [20, 7, 9, 1, 23, 14, 5, 12, 2, 21, 16, 10, 6, 25, 4, 22, 19, 17, 18, 11, 8],
        BowledPositions: 17);
}