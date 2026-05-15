using Ardalis.SmartEnum;

namespace Neba.Api.Features.Tournaments.Domain;


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
    public static readonly TournamentType Singles = new(nameof(Singles), 100, 1, true);

    /// <summary>
    /// Doubles format: two bowlers compete as a team.
    /// </summary>
    public static readonly TournamentType Doubles = new(nameof(Doubles), 200, 2, true);

    /// <summary>
    /// Trios format: three bowlers compete as a team.
    /// </summary>
    public static readonly TournamentType Trios = new(nameof(Trios), 300, 3, true);

    /// <summary>
    /// Baker format: five bowlers compete as a team, each bowling two frames per game in rotation.
    /// </summary>
    public static readonly TournamentType Baker = new(nameof(Baker), 500, 5, true);

    /// <summary>
    /// Non-Champions format: restricted to bowlers who have not previously won a NEBA title.
    /// </summary>
    public static readonly TournamentType NonChampions = new("Non-Champions", 101, 1, true);

    /// <summary>
    /// Tournament of Champions format: restricted to past NEBA title winners.
    /// </summary>
    public static readonly TournamentType TournamentOfChampions = new("Tournament of Champions", 102, 1, true);

    /// <summary>
    /// Invitational format: participation is restricted to specifically invited bowlers.
    /// </summary>
    public static readonly TournamentType Invitational = new(nameof(Invitational), 103, 1, true);

    /// <summary>
    /// Masters format: one of NEBA's Major tournaments, alongside the Tournament of Champions
    /// and the Invitational.
    /// </summary>
    public static readonly TournamentType Masters = new(nameof(Masters), 104, 1, true);

    /// <summary>
    /// High Roller format: features an elevated entry fee structure. No longer an active format.
    /// </summary>
    public static readonly TournamentType HighRoller = new("High Roller", 105, 1, false);

    /// <summary>
    /// Senior format: restricted to bowlers aged 50 or older per NEBA eligibility rules.
    /// </summary>
    public static readonly TournamentType Senior = new(nameof(Senior), 106, 1, true);

    /// <summary>
    /// Women's format: restricted to female bowlers.
    /// </summary>
    public static readonly TournamentType Women = new(nameof(Women), 107, 1, true);

    /// <summary>
    /// Over 40 format: restricted to bowlers aged 40 or older. No longer an active format.
    /// </summary>
    public static readonly TournamentType OverForty = new("Over 40", 108, 1, false);

    /// <summary>
    /// 40–49 format: restricted to bowlers aged 40 to 49. No longer an active format.
    /// </summary>
    public static readonly TournamentType FortyToFortyNine = new("40 - 49", 109, 1, false);

    /// <summary>
    /// Youth format: restricted to bowlers under the age of 18 per NEBA eligibility rules.
    /// </summary>
    public static readonly TournamentType Youth = new(nameof(Youth), 110, 1, true);

    /// <summary>
    /// Eliminator format: a progressive knockout format. No longer an active format.
    /// </summary>
    public static readonly TournamentType Eliminator = new(nameof(Eliminator), 111, 1, false);

    /// <summary>
    /// Senior/Women combined format: open to bowlers who qualify as Senior (aged 50 or older)
    /// or Women.
    /// </summary>
    public static readonly TournamentType SeniorAndWomen = new("Senior / Women", 112, 1, true);

    /// <summary>
    /// Over/Under 50 Doubles format: two-player teams consisting of one bowler aged 50 or older
    /// (Over) and one bowler under 50 (Under).
    /// </summary>
    public static readonly TournamentType OverUnderFiftyDoubles = new("Over/Under 50 Doubles", 201, 2, true);

    /// <summary>
    /// Over/Under 40 Doubles format: two-player teams consisting of one bowler aged 40 or older
    /// (Over) and one bowler under 40 (Under). No longer an active format.
    /// </summary>
    public static readonly TournamentType OverUnderFortyDoubles = new("Over/Under 40 Doubles", 202, 2, false);

    /// <summary>
    /// Initializes a new instance of the <see cref="TournamentType"/> class.
    /// </summary>
    /// <param name="name">The display name of the tournament type.</param>
    /// <param name="value">The unique integer value for the tournament type.</param>
    /// <param name="teamSize">The number of players per team for this tournament type.</param>
    /// <param name="activeFormat">Indicates whether this tournament type is an active format.</param>
    private TournamentType(string name, int value, int teamSize, bool activeFormat)
        : base(name, value)
    {
        TeamSize = teamSize;
        ActiveFormat = activeFormat;
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
}