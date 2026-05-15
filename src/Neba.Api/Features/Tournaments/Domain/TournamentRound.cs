using System.Text.Json.Serialization;

using Ardalis.SmartEnum;

using Neba.Api.Domain;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// Represents the various rounds that may be present in a tournament structure. A tournament may have one or more rounds, and the specific rounds used may vary based on the tournament format.
/// </summary>
[JsonConverter(typeof(SmartFlagEnumJsonConverter<TournamentRound>))]
public sealed class TournamentRound
    : SmartFlagEnum<TournamentRound>
{
    /// <summary>
    /// A round of squads in which bowlers establish a pinfall score to determine advancement eligibility.
    /// All qualifying squads in a tournament use the same oil pattern. Bowlers may enter one or more
    /// qualifying squads depending on tournament entry rules.
    /// </summary>
    public static readonly TournamentRound Qualifying = new(nameof(Qualifying), 1);

    /// <summary>
    /// An intermediate round after qualifying in which bowlers who made an initial cut compete for a
    /// secondary cut. Bowlers eliminated here still earn a cash prize. Not currently used by NEBA;
    /// supported for compatibility with external tournament formats.
    /// </summary>
    public static readonly TournamentRound Cashers = new(nameof(Cashers), 1 << 1);

    /// <summary>
    /// A bracket-based finals round in which bowlers compete head-to-head, with advancement determined
    /// by individual game outcomes rather than cumulative pinfall. Bracket structure may be traditional
    /// or eliminator format.
    /// </summary>
    public static readonly TournamentRound MatchPlay = new("Match Play", 1 << 2);

    /// <summary>
    /// A finals round in which a small number of advancing bowlers compete in sequenced single
    /// elimination. The lowest seed bowls the next lowest, with the winner advancing up the ladder
    /// until a champion is determined.
    /// </summary>
    public static readonly TournamentRound StepLadder = new("Step Ladder", 1 << 3);

    private TournamentRound(string name, int value)
        : base(name, value)
    { }
}