<Query Kind="Program">
  <Connection>
    <ID>1fc48aec-40be-4c6b-a8e7-a5b7f0248ee4</ID>
    <NamingServiceVersion>3</NamingServiceVersion>
    <Persist>true</Persist>
    <Driver Assembly="(internal)" PublicKeyToken="no-strong-name">LINQPad.Drivers.EFCore.DynamicDriver</Driver>
    <AllowDateOnlyTimeOnly>true</AllowDateOnlyTimeOnly>
    <SqlSecurity>true</SqlSecurity>
    <UserName>nebaadmin</UserName>
    <Database>bowlneba</Database>
    <Server>postgres-j7jlg5nmk67ym.postgres.database.azure.com</Server>
    <DisplayName>neba-website</DisplayName>
    <IsProduction>true</IsProduction>
    <DriverData>
      <EncryptSqlTraffic>True</EncryptSqlTraffic>
      <MapSQLiteDecimals>true</MapSQLiteDecimals>
      <PreserveNumeric1>True</PreserveNumeric1>
      <EFProvider>Npgsql.EntityFrameworkCore.PostgreSQL</EFProvider>
    </DriverData>
  </Connection>
  <NuGetReference>Ardalis.SmartEnum</NuGetReference>
  <Namespace>Ardalis.SmartEnum</Namespace>
</Query>

void Main()
{
	// Scope: seasons/tournaments 2018 and earlier (current system covers 2019+).
	var cutoff = new DateOnly(2019, 1, 1);

	var earned = new List<Earned>();

	// ---- Tournament titles ----
	var titles = TournamentChampions
		.Where(c => c.Tournament.StartDate < cutoff)
		.Select(c => new
		{
			c.BowlerId,
			c.Bowler.LegacyId,
			c.Bowler.FirstName,
			c.Bowler.LastName,
			c.Tournament.SeasonId,
			c.Tournament.StartDate,
			c.Tournament.EndDate,
			c.Tournament.TournamentType
		})
		.ToList();

	var unmappedTypes = new HashSet<int>();

	foreach (var t in titles)
	{
		if (!TitlePoints.TryGetValue(t.TournamentType, out var rule))
		{
			unmappedTypes.Add(t.TournamentType);
			continue;
		}

		var (source, points) = (rule.Source, rule.Points);

		// Legacy: NEBA Singles title prior to 1991 is worth 12 instead of 6.
		if (t.TournamentType == TournamentType.Singles.Value && t.StartDate.Year < 1991)
			(source, points) = ("NEBA Singles Title (pre-1991)", PreNinetyOneSinglesPoints);

		if (points > 0)
			earned.Add(new Earned(t.BowlerId, t.LegacyId, $"{t.FirstName} {t.LastName}", t.SeasonId, t.EndDate, source, points));
	}

	// ---- Bowler of the Year awards ----
	var boy = BowlersOfTheYearAwards
		.Where(a => a.Season.EndDate < cutoff)
		.Select(a => new
		{
			a.Bowler.Id,
			a.Bowler.LegacyId,
			a.Bowler.FirstName,
			a.Bowler.LastName,
			SeasonKey = a.Season.DomainId,
			SeasonEnd = a.Season.EndDate,
			a.Category
		})
		.ToList();

	foreach (var a in boy)
	{
		if (!BoyPoints.TryGetValue(a.Category, out var rule))
		{
			unmappedTypes.Add(-a.Category); // negative = BOY category with no mapping
			continue;
		}

		if (rule.Points > 0)
			earned.Add(new Earned(a.Id, a.LegacyId, $"{a.FirstName} {a.LastName}", a.SeasonKey, a.SeasonEnd, rule.Source, rule.Points));
	}

	// ---- Roll up per bowler ----
	var result = earned
		.GroupBy(e => e.BowlerId)
		.Select(g => new
		{
			BowlerId = g.Key,
			LegacyBowlerId = LegacyIdOverrides.TryGetValue(g.Key, out var overrideId) ? overrideId : g.First().LegacyId,
			Bowler = g.First().Name,
			TotalPoints = g.Sum(e => e.Points),
			SeasonsWithPoints = g.Select(e => e.Season).Distinct().Count(),
			Breakdown = g
				.OrderBy(e => e.Date)
				.Select(e => new { EndDate = e.Date, Type = e.Source, e.Points })
				.ToList()
		})
		.OrderByDescending(r => r.TotalPoints)
		.ThenBy(r => r.Bowler)
		.ToList();

	if (unmappedTypes.Any())
		unmappedTypes.Dump("WARNING: unmapped tournament types (positive) / BOY categories (negative) - ignored", 1);

	// ---- Reconcile against the legacy base points table (matched on LegacyId) ----
	// Bowlers already inducted into the Hall of Fame for Superior Performance (flag may be combined with other categories).
	var performanceFlag = HallOfFameCategory.SuperiorPerformance.Value;
	var hofPerformance = HallsOfFameInductions
		.Where(h => (h.Category & performanceFlag) != 0)
		.Select(h => new { h.Bowler.Id, h.InductionYear })
		.ToList()
		.GroupBy(h => h.Id)
		.ToDictionary(g => g.Key, g => g.Min(h => h.InductionYear));

	var baseTable = BasePointsData
		.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
		.Select(l => l.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		.ToDictionary(p => int.Parse(p[0]), p => (Points: int.Parse(p[1]), Seasons: int.Parse(p[2])));

	// A bowler with no LegacyId (or a LegacyId missing from the base table) is treated as having
	// 0 base points / 0 base seasons. Diffs are calculated minus base (positive = we calculated more).
	var differences = result
		.Select(r =>
		{
			var (basePoints, baseSeasons) = r.LegacyBowlerId is int id && baseTable.TryGetValue(id, out var v) ? v : (0, 0);
			return new
			{
				r.BowlerId,
				r.LegacyBowlerId,
				r.Bowler,
				BasePoints = basePoints,
				BaseSeasons = baseSeasons,
				CalcPoints = r.TotalPoints,
				CalcSeasons = r.SeasonsWithPoints,
				r.Breakdown
			};
		})
		.Where(r => r.BasePoints != r.CalcPoints || r.BaseSeasons != r.CalcSeasons)
		.ToList();

	// Base-table bowlers for whom we calculated nothing (calculated = 0, no breakdown).
	var calculatedLegacyIds = result.Where(r => r.LegacyBowlerId != null).Select(r => r.LegacyBowlerId!.Value).ToHashSet();
	var baseOnlyIds = baseTable.Keys.Where(k => !calculatedLegacyIds.Contains(k)).ToList();
	var baseOnlyBowlers = Bowlers
		.Where(b => b.LegacyId != null && baseOnlyIds.Contains(b.LegacyId.Value))
		.Select(b => new { b.Id, b.LegacyId, b.FirstName, b.LastName })
		.ToList();

	var baseOnlyRows = baseOnlyIds.Select(legacyId =>
	{
		var b = baseOnlyBowlers.FirstOrDefault(x => x.LegacyId == legacyId);
		return new
		{
			BowlerId = b?.Id ?? 0,
			LegacyBowlerId = (int?)legacyId,
			Bowler = b is null ? "(not found in Bowlers)" : $"{b.FirstName} {b.LastName}",
			BasePoints = baseTable[legacyId].Points,
			BaseSeasons = baseTable[legacyId].Seasons,
			CalcPoints = 0,
			CalcSeasons = 0,
			Breakdown = new[] { new { EndDate = default(DateOnly), Type = "", Points = 0 } }.Take(0).ToList()
		};
	});

	differences
		.Concat(baseOnlyRows)
		.Select(r => new
		{
			r.LegacyBowlerId,
			r.Bowler,
			InPerformanceHof = hofPerformance.ContainsKey(r.BowlerId),
			PerformanceHofYear = hofPerformance.TryGetValue(r.BowlerId, out var y) ? (int?)y : null,
			r.BasePoints,
			r.CalcPoints,
			PointsDiff = r.CalcPoints - r.BasePoints,
			r.BaseSeasons,
			r.CalcSeasons,
			SeasonsDiff = r.CalcSeasons - r.BaseSeasons,
			r.Breakdown
		})
		.OrderBy(r => r.InPerformanceHof)
		.ThenBy(r => r.PointsDiff)
		.ThenBy(r => r.Bowler)
		.Dump("Bowlers with differences vs base table (diff = calculated - base)", 3);
}

// Date = tournament end date for titles; season end date for Bowler of the Year awards.
record Earned(int BowlerId, int? LegacyId, string Name, string Season, DateOnly Date, string Source, int Points);

// Duplicate Bowlers records that hold titles but have no LegacyId. Maps Bowlers.Id -> the legacy ID of the
// same person, for reconciliation only (the database is not changed).
static readonly Dictionary<int, int> LegacyIdOverrides = new()
{
	[5119] = 1145, // Chuck Oeik     -> Chuck Oeinck (duplicate record)
	[5142] = 2172, // John Mammolit  -> John Mammolite (duplicate record)
};

record Recon(string Status, int? BasePoints, int? BaseSeasons, int? PointsDiff, int? SeasonsDiff);

// Legacy base points table: LegacyBowlerId Points Years
const string BasePointsData = """
2903 6 1
2394 114 11
3253 162 13
2994 2 1
3234 8 2
737 60 6
1406 18 3
826 6 1
554 2 1
720 44 3
2524 30 3
3909 5 1
1416 10 3
2783 0 1
2510 12 2
1383 6 1
2928 12 2
1290 28 6
2278 6 1
41 2 1
3195 10 2
2761 18 3
498 18 3
284 2 1
2909 20 3
701 6 1
3047 76 6
1801 6 1
3031 2 1
1073 24 2
3032 200 17
1311 6 1
1130 6 1
2063 2 1
493 6 1
1917 14 4
3619 2 1
273 6 1
2898 12 2
2314 12 2
2843 6 1
1944 8 2
3384 66 7
2210 2 1
2151 270 20
1460 6 1
2120 2 1
1610 10 3
3223 2 1
2538 24 3
2799 6 1
3129 6 1
693 32 4
1860 2 1
529 8 1
1721 15 2
2449 2 1
2367 32 5
5023 6 1
404 40 7
2912 40 7
1284 2 1
2621 10 1
3482 36 4
3453 2 1
2934 12 2
2677 2 1
2046 12 1
2855 12 2
1997 14 2
3511 10 3
2931 2 1
1664 2 1
653 59 7
2543 20 4
1309 26 3
1683 6 1
573 2 1
2320 6 1
3257 6 1
2894 2 1
2944 36 3
502 12 2
3165 6 1
172 30 3
290 2 1
2560 12 1
3334 102 10
2104 6 1
2803 6 1
2191 6 1
318 2 1
2736 2 1
1695 6 1
2615 2 1
1236 6 1
1373 6 1
61 6 1
3203 6 1
1671 20 3
339 14 3
1861 2 1
3142 18 3
1904 2 1
1889 24 4
3210 6 1
628 6 1
2599 20 4
868 6 1
3030 50 6
2842 2 1
1207 6 1
3303 6 1
2242 12 2
98 2 1
3079 18 2
2200 6 1
439 24 4
2568 6 1
525 6 1
1482 2 1
1132 48 7
1217 82 10
1535 6 1
165 2 1
2443 6 1
2142 6 1
454 6 1
943 54 7
2115 12 2
433 6 1
3183 6 1
2441 42 5
570 2 1
1287 6 1
3201 18 2
2466 8 2
1019 6 1
2624 6 1
3554 18 3
1736 18 3
3036 6 1
3440 2 1
1628 120 9
578 2 1
527 6 1
2273 6 1
2862 6 1
418 4 1
2074 86 11
1282 12 2
1084 6 1
461 12 2
853 2 1
2851 6 1
251 24 3
3411 6 1
2185 8 2
2696 20 4
104 2 1
3413 6 1
2092 6 1
1165 30 3
1787 36 6
1873 6 1
1667 6 1
1723 6 1
3246 2 1
2650 2 1
2461 12 2
1200 6 1
2925 21 3
2442 6 1
2037 6 1
2916 2 1
1835 6 1
2989 6 1
634 12 1
1697 12 2
3519 2 1
80 6 1
2531 14 3
1548 4 1
2588 6 1
2277 148 13
397 6 1
850 6 1
3551 6 1
2171 8 2
186 18 2
2868 18 2
3486 6 1
3455 6 1
532 2 1
2304 2 1
1410 6 1
2884 20 3
2287 6 1
308 6 1
309 2 1
1930 6 1
1662 6 1
1747 2 1
1674 2 1
1398 6 1
2475 132 8
548 2 1
1526 12 2
229 2 1
3512 42 5
2372 2 1
3262 6 1
2299 2 1
234 38 5
816 6 1
1845 63 6
612 2 1
2970 2 1
1824 21 3
763 6 1
642 4 2
1123 30 3
322 15 2
2577 2 1
1242 36 6
645 12 2
2542 124 9
3231 3 1
285 6 1
732 24 3
158 54 4
422 6 1
2663 38 3
619 6 1
2227 12 2
2368 3 1
3002 18 3
837 6 1
1448 6 1
56 6 1
2594 38 4
74 6 1
1028 6 1
1549 54 5
1478 3 1
1500 36 4
596 6 1
3339 18 2
3897 6 1
663 3 1
57 18 2
1568 6 1
3553 18 2
87 18 2
1105 48 3
1637 12 2
1408 6 1
2979 6 1
2938 6 1
1545 6 1
2239 6 1
364 12 2
2481 3 1
1793 6 1
1330 12 1
4170 6 1
1497 24 3
2609 66 5
977 2 1
3885 66 5
204 30 3
2270 6 1
802 6 1
1808 6 1
731 6 1
2072 12 2
3374 6 1
1774 6 1
2760 12 2
2850 24 2
2870 48 4
419 24 2
2209 12 1
4496 12 1
2825 12 1
555 12 1
2101 24 1
2135 60 5
1145 12 1
2077 6 1
2809 60 5
1869 24 2
2075 6 1
3882 48 4
936 48 3
446 12 1
1186 24 2
72 24 2
2036 12 1
1093 12 1
403 12 1
3547 60 3
2486 24 1
3368 12 1
673 12 1
2188 12 1
3887 48 3
1022 48 3
2255 36 3
2674 48 4
1372 24 1
1431 24 2
238 12 1
3884 48 4
616 36 1
2305 12 1
1124 60 4
534 12 1
1530 12 1
4226 12 1
306 12 1
207 12 1
1561 12 1
1283 48 3
1727 12 1
2548 12 1
2697 12 1
1979 12 1
2226 12 1
1015 12 1
2431 12 1
2172 12 1
3169 12 1
1542 24 1
3889 12 1
3496 2 1
3749 4 2
3117 2 1
276 2 1
515 2 1
1595 2 1
2616 2 1
2040 2 1
257 2 1
3206 2 1
3319 2 1
3528 2 1
""";

const int PreNinetyOneSinglesPoints = 12;

// Tournament type code -> (title label, points). Tweak here when reconciling.
static readonly Dictionary<int, (string Source, int Points)> TitlePoints = new()
{
	[TournamentType.Singles.Value]               = ("Singles Title", 6),                    // 100 (12 if before 1991, see above)
	[TournamentType.Doubles.Value]               = ("Doubles Title", 6),                    // 200
	[TournamentType.TournamentOfChampions.Value] = ("TOC Title", 6),                        // 102
	[TournamentType.Invitational.Value]          = ("Points Leader/Invitational Title", 6), // 103
	[TournamentType.Masters.Value]               = ("NEBA Masters Title", 6),               // 104
	[TournamentType.OverUnderFiftyDoubles.Value] = ("Over/Under Title", 3),                 // 201
	[TournamentType.OverUnderFortyDoubles.Value] = ("Over/Under Title", 3),                 // 202
	[TournamentType.NonChampions.Value]          = ("Non-Champions Title", 2),              // 101
	[TournamentType.Senior.Value]                = ("Senior Title", 2),                     // 106
	[TournamentType.SeniorAndWomen.Value]        = ("Senior Title", 2),                     // 112 counts as Senior
	[TournamentType.Trios.Value]                 = ("NEBA Trios Title", 2),                 // 300
	[TournamentType.HighRoller.Value]            = ("NEBA High-Roller Title", 3),           // 105 legacy
	[TournamentType.OverForty.Value]             = ("NEBA Over 40 Title", 2),               // 108 legacy
	// NOTE: 40-49 treated as NEBA Over 40 Title (2 pts). Revisit if this differs when reconciling.
	[TournamentType.FortyToFortyNine.Value]      = ("NEBA Over 40 Title (40-49)", 2),       // 109
	// Zero-point types, listed explicitly to help reconciliation.
	[TournamentType.Women.Value]                 = ("Women Title (no points)", 0),          // 107
	[TournamentType.Youth.Value]                 = ("Youth Title (no points)", 0),          // 110
	[TournamentType.Eliminator.Value]            = ("Eliminator Title (no points)", 0),     // 111
	[TournamentType.Baker.Value]                 = ("Baker Title (no points)", 0),          // 500
};

// Bowler of the Year category code -> (award label, points).
static readonly Dictionary<int, (string Source, int Points)> BoyPoints = new()
{
	[BowlerOfTheYearCategory.Open.Value]        = ("Bowler of the Year", 12),
	[BowlerOfTheYearCategory.Senior.Value]      = ("Senior Bowler of the Year", 2),
	[BowlerOfTheYearCategory.SuperSenior.Value] = ("Super Senior Bowler of the Year", 2),
	[BowlerOfTheYearCategory.Woman.Value]       = ("Woman Bowler of the Year", 2),
	[BowlerOfTheYearCategory.Rookie.Value]      = ("Rookie of the Year", 2),
	[BowlerOfTheYearCategory.Youth.Value]       = ("Youth Bowler of the Year (no points)", 0),
};

// You can define other methods, fields, classes and namespaces here
/// <summary>
/// Represents the format classification of a NEBA tournament, including its display name,
/// unique value, team size, and whether it is a currently active format.
/// Inherits from <see cref="SmartEnum{TournamentType}"/> for enhanced enum-like behavior.
/// </summary>
public sealed class TournamentType
	: SmartEnum<TournamentType>
{
	/// <summary>
	/// Singles format: one bowler competes as an individual entry.
	/// </summary>
	public static readonly TournamentType Singles = new(nameof(Singles), 100, 1, true, 50);

	/// <summary>
	/// Doubles format: two bowlers compete as a team.
	/// </summary>
	public static readonly TournamentType Doubles = new(nameof(Doubles), 200, 2, true, 25);

	/// <summary>
	/// Trios format: three bowlers compete as a team.
	/// </summary>
	public static readonly TournamentType Trios = new(nameof(Trios), 300, 3, true, 10);

	/// <summary>
	/// Baker format: five bowlers compete as a team, each bowling two frames per game in rotation.
	/// </summary>
	public static readonly TournamentType Baker = new(nameof(Baker), 500, 5, true, 10);

	/// <summary>
	/// Non-Champions format: restricted to bowlers who have not previously won a NEBA title.
	/// </summary>
	public static readonly TournamentType NonChampions = new("Non-Champions", 101, 1, true, 30);

	/// <summary>
	/// Tournament of Champions format: restricted to past NEBA title winners.
	/// </summary>
	public static readonly TournamentType TournamentOfChampions = new("Tournament of Champions", 102, 1, true, 20);

	/// <summary>
	/// Invitational format: participation is restricted to specifically invited bowlers.
	/// </summary>
	public static readonly TournamentType Invitational = new(nameof(Invitational), 103, 1, true, 50);

	/// <summary>
	/// Masters format: one of NEBA's Major tournaments, alongside the Tournament of Champions
	/// and the Invitational.
	/// </summary>
	public static readonly TournamentType Masters = new(nameof(Masters), 104, 1, true, 50);

	/// <summary>
	/// High Roller format: features an elevated entry fee structure. No longer an active format.
	/// </summary>
	public static readonly TournamentType HighRoller = new("High Roller", 105, 1, false, 0);

	/// <summary>
	/// Senior format: restricted to bowlers aged 50 or older per NEBA eligibility rules.
	/// </summary>
	public static readonly TournamentType Senior = new(nameof(Senior), 106, 1, true, 25);

	/// <summary>
	/// Women's format: restricted to female bowlers.
	/// </summary>
	public static readonly TournamentType Women = new(nameof(Women), 107, 1, true, 25);

	/// <summary>
	/// Over 40 format: restricted to bowlers aged 40 or older. No longer an active format.
	/// </summary>
	public static readonly TournamentType OverForty = new("Over 40", 108, 1, false, 0);

	/// <summary>
	/// 40–49 format: restricted to bowlers aged 40 to 49. No longer an active format.
	/// </summary>
	public static readonly TournamentType FortyToFortyNine = new("40 - 49", 109, 1, false, 0);

	/// <summary>
	/// Youth format: restricted to bowlers under the age of 18 per NEBA eligibility rules.
	/// </summary>
	public static readonly TournamentType Youth = new(nameof(Youth), 110, 1, true, 25);

	/// <summary>
	/// Eliminator format: a progressive knockout format. No longer an active format.
	/// </summary>
	public static readonly TournamentType Eliminator = new(nameof(Eliminator), 111, 1, false, 0);

	/// <summary>
	/// Senior/Women combined format: open to bowlers who qualify as Senior (aged 50 or older)
	/// or Women.
	/// </summary>
	public static readonly TournamentType SeniorAndWomen = new("Senior / Women", 112, 1, true, 40);

	/// <summary>
	/// Over/Under 50 Doubles format: two-player teams consisting of one bowler aged 50 or older
	/// (Over) and one bowler under 50 (Under).
	/// </summary>
	public static readonly TournamentType OverUnderFiftyDoubles = new("Over/Under 50 Doubles", 201, 2, true, 15);

	/// <summary>
	/// Over/Under 40 Doubles format: two-player teams consisting of one bowler aged 40 or older
	/// (Over) and one bowler under 40 (Under). No longer an active format.
	/// </summary>
	public static readonly TournamentType OverUnderFortyDoubles = new("Over/Under 40 Doubles", 202, 2, false, 0);

	/// <summary>
	/// Initializes a new instance of the <see cref="TournamentType"/> class.
	/// </summary>
	/// <param name="name">
	/// The display name of the tournament type.
	/// </param>
	/// <param name="value">
	/// The unique integer value for the tournament type.
	/// </param>
	/// <param name="teamSize">
	/// The number of players per team for this tournament type.
	/// </param>
	/// <param name="activeFormat">
	/// Indicates whether this tournament type is an active format.
	/// </param>
	/// <param name="minimumEntries">
	/// The minimum number of entries required for a completed tournament of this type to count
	/// toward a NEBA title.
	/// </param>
	private TournamentType(string name, int value, int teamSize, bool activeFormat, int minimumEntries)
		: base(name, value)
	{
		TeamSize = teamSize;
		ActiveFormat = activeFormat;
		MinimumEntries = minimumEntries;
	}

	/// <summary>
	/// Gets the number of bowlers who compete as a single entry unit for this tournament format.
	/// </summary>
	public int TeamSize { get; }

	/// <summary>
	/// Gets a value indicating whether this tournament type is currently offered by NEBA.
	/// Inactive formats are retained for historical data integrity but are not available
	/// for new tournament creation.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if this format is currently active; otherwise,
	/// <see langword="false"/>.
	/// </value>
	public bool ActiveFormat { get; }

	/// <summary>
	/// The minimum number of entries (bowlers for a 1-bowler format, teams otherwise) required for a
	/// completed tournament of this type to count toward a NEBA title. Not retroactive — a change to
	/// this value only affects tournaments completed after the change; <see cref="Tournament.TitleEligible"/>
	/// is frozen at the moment a tournament transitions to <see cref="TournamentStatus.Completed"/>.
	/// </summary>
	public int MinimumEntries { get; }
}

/// <summary>
/// The competitive category under which a Bowler of the Year award is given.
/// Age eligibility is evaluated as of each tournament date during the season.
/// </summary>
public sealed class BowlerOfTheYearCategory
	: SmartEnum<BowlerOfTheYearCategory>
{
	/// <summary>
	/// All eligible bowlers.
	/// </summary>
	public static readonly BowlerOfTheYearCategory Open = new("Open", 1);

	/// <summary>
	/// Female bowlers.
	/// </summary>
	public static readonly BowlerOfTheYearCategory Woman = new("Woman", 2);

	/// <summary>
	/// Bowlers age 50 or older.
	/// </summary>
	public static readonly BowlerOfTheYearCategory Senior = new("Senior", 50);

	/// <summary>
	/// Bowlers age 60 or older.
	/// </summary>
	public static readonly BowlerOfTheYearCategory SuperSenior = new("Super Senior", 60);

	/// <summary>
	/// Bowlers paying a New Member membership in the current season.
	/// </summary>
	public static readonly BowlerOfTheYearCategory Rookie = new("Rookie", 3);

	/// <summary>
	/// Bowlers under age 18.
	/// </summary>
	public static readonly BowlerOfTheYearCategory Youth = new("Youth", 18);

	private BowlerOfTheYearCategory(string name, int value)
		: base(name, value) { }
}

/// <summary>
/// Represents one or more Hall of Fame categories for a person or entity.
/// </summary>
/// <remarks>
/// This type is a set of bit flags (powers of two) so multiple categories
/// can be combined with a bitwise OR (for example,
/// <c>SuperiorPerformance | FriendOfNeba</c>). Use <see cref="SmartFlagEnum{T}"/>
/// helpers or bitwise operations to test membership.
/// </remarks>
public sealed class HallOfFameCategory
	: SmartFlagEnum<HallOfFameCategory>
{
	/// <summary>
	/// Indicates superior performance recognition.
	/// This flag has the value <c>1</c> (bit 0).
	/// </summary>
	public static readonly HallOfFameCategory SuperiorPerformance = new("Superior Performance", 1);

	/// <summary>
	/// Indicates meritorious service recognition.
	/// This flag has the value <c>2</c> (bit 1).
	/// </summary>
	public static readonly HallOfFameCategory MeritoriousService = new("Meritorious Service", 1 << 1);

	/// <summary>
	/// Indicates a friend of NEBA recognition.
	/// This flag has the value <c>4</c> (bit 2).
	/// </summary>
	public static readonly HallOfFameCategory FriendOfNeba = new("Friend of NEBA", 1 << 2);

	private HallOfFameCategory(string name, int value)
		: base(name, value)
	{ }
}