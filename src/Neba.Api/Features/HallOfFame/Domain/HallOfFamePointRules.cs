using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.HallOfFame.Domain;

/// <summary>
/// The Hall of Fame Superior Performance point table and thresholds.
/// </summary>
internal static class HallOfFamePointRules
{
    /// <summary>Points needed for Superior Performance.</summary>
    public const int RequiredPoints = 36;

    /// <summary>Different seasons with a title or award needed.</summary>
    public const int RequiredSeasonsWithPoints = 3;

    /// <summary>Membership years needed.</summary>
    public const int RequiredMembershipYears = 10;

    /// <summary>First year with membership records; earlier years are not counted.</summary>
    public const int MembershipDataStartYear = 1999;

    private const int PreNinetyOneSinglesCutoffYear = 1991;

    private const string AwardGroup = "Bowler of the Year Awards";
    private const string TitleGroup = "Tournament Titles";
    private const string LegacyGroup = "Legacy Titles";

    // Rules are declared before the lookups below: static initializers run in text order.
    private static readonly HallOfFamePointRule BowlerOfTheYear = new(AwardGroup, "Bowler of the Year", 12);
    private static readonly HallOfFamePointRule SeniorBowlerOfTheYear = new(AwardGroup, "Senior Bowler of the Year", 2);
    private static readonly HallOfFamePointRule SuperSeniorBowlerOfTheYear = new(AwardGroup, "Super Senior Bowler of the Year", 2);

    private static readonly HallOfFamePointRule Singles = new(TitleGroup, "Singles Title", 6);
    private static readonly HallOfFamePointRule TournamentOfChampions = new(TitleGroup, "TOC Title", 6);
    private static readonly HallOfFamePointRule Doubles = new(TitleGroup, "Doubles Title", 6);
    private static readonly HallOfFamePointRule Invitational = new(TitleGroup, "Points Leader/Invitational Title", 6);
    private static readonly HallOfFamePointRule Masters = new(TitleGroup, "NEBA Masters Title", 6);
    private static readonly HallOfFamePointRule OverUnder = new(TitleGroup, "Over/Under Title", 3);
    private static readonly HallOfFamePointRule NonChampions = new(TitleGroup, "Non-Champions Title", 2);
    private static readonly HallOfFamePointRule Senior = new(TitleGroup, "Senior Title", 2);
    private static readonly HallOfFamePointRule Trios = new(TitleGroup, "NEBA Trios Title", 2);

    private static readonly HallOfFamePointRule SinglesBeforeNinetyOne = new(LegacyGroup, "NEBA Singles Title (prior to 1991)", 12);
    private static readonly HallOfFamePointRule HighRoller = new(LegacyGroup, "NEBA High-Roller Title", 3);
    private static readonly HallOfFamePointRule OverForty = new(LegacyGroup, "NEBA Over 40 Title", 2);

    /// <summary>Every rule, in display order. Sent to the page so its table cannot drift from the math.</summary>
    public static IReadOnlyList<HallOfFamePointRule> All { get; } =
    [
        BowlerOfTheYear, SeniorBowlerOfTheYear, SuperSeniorBowlerOfTheYear,
        Singles, TournamentOfChampions, Doubles, Invitational, Masters, OverUnder, NonChampions, Senior, Trios,
        SinglesBeforeNinetyOne, HighRoller, OverForty
    ];

    // A null value means the type is known and earns no points. A type missing from the map is a bug;
    // HallOfFamePointRulesTests fails when a new TournamentType or category has no entry.
    private static readonly Dictionary<TournamentType, HallOfFamePointRule?> TitleRules = new()
    {
        [TournamentType.Singles] = Singles,
        [TournamentType.Doubles] = Doubles,
        [TournamentType.TournamentOfChampions] = TournamentOfChampions,
        [TournamentType.Invitational] = Invitational,
        [TournamentType.Masters] = Masters,
        [TournamentType.OverUnderFiftyDoubles] = OverUnder,
        [TournamentType.OverUnderFortyDoubles] = OverUnder,
        [TournamentType.NonChampions] = NonChampions,
        [TournamentType.Senior] = Senior,
        [TournamentType.SeniorAndWomen] = Senior,
        [TournamentType.Trios] = Trios,
        [TournamentType.HighRoller] = HighRoller,
        [TournamentType.OverForty] = OverForty,
        [TournamentType.FortyToFortyNine] = OverForty,
        [TournamentType.Women] = null,
        [TournamentType.Youth] = null,
        [TournamentType.Eliminator] = null,
        [TournamentType.Baker] = null
    };

    private static readonly Dictionary<BowlerOfTheYearCategory, HallOfFamePointRule?> AwardRules = new()
    {
        [BowlerOfTheYearCategory.Open] = BowlerOfTheYear,
        [BowlerOfTheYearCategory.Senior] = SeniorBowlerOfTheYear,
        [BowlerOfTheYearCategory.SuperSenior] = SuperSeniorBowlerOfTheYear,
        [BowlerOfTheYearCategory.Woman] = null,
        [BowlerOfTheYearCategory.Rookie] = null,
        [BowlerOfTheYearCategory.Youth] = null
    };

    /// <summary>The rule for a title, or null when the title earns no points.</summary>
    public static HallOfFamePointRule? ForTitle(TournamentType type, DateOnly startDate)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type == TournamentType.Singles && startDate.Year < PreNinetyOneSinglesCutoffYear
            ? SinglesBeforeNinetyOne
            : TitleRules.GetValueOrDefault(type);
    }

    /// <summary>The rule for a Bowler of the Year award, or null when it earns no points.</summary>
    public static HallOfFamePointRule? ForAward(BowlerOfTheYearCategory category)
    {
        ArgumentNullException.ThrowIfNull(category);
        return AwardRules.GetValueOrDefault(category);
    }

    /// <summary>Whether the type has an entry in the table, including a deliberate zero.</summary>
    public static bool IsMapped(TournamentType type)
        => TitleRules.ContainsKey(type);

    /// <summary>Whether the category has an entry in the table, including a deliberate zero.</summary>
    public static bool IsMapped(BowlerOfTheYearCategory category)
        => AwardRules.ContainsKey(category);

    /// <summary>
    /// Whether all three Superior Performance criteria are met. Membership years can only be undercounted
    /// (records start in 1999), so a true result is reliable; a false one can miss a long-time member.
    /// </summary>
    public static bool MeetsAllCriteria(int totalPoints, int seasonsWithPoints, int membershipYears)
        => totalPoints >= RequiredPoints
            && seasonsWithPoints >= RequiredSeasonsWithPoints
            && membershipYears >= RequiredMembershipYears;
}