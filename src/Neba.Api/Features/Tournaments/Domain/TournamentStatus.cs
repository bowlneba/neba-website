using Ardalis.SmartEnum;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// The lifecycle state of a <see cref="Tournament"/>.
/// </summary>
public sealed class TournamentStatus
    : SmartEnum<TournamentStatus>
{
    /// <summary>
    /// The tournament has been created but hasn't finished — covers both "not yet started" and
    /// "in progress"; <see cref="Tournament.StartDate"/>/<see cref="Tournament.EndDate"/> already
    /// distinguish those. Default status.
    /// </summary>
    public static readonly TournamentStatus Scheduled = new(nameof(Scheduled), 0);

    /// <summary>
    /// The tournament ran its full planned format to conclusion. The only status a tournament can
    /// be title-eligible from — see <see cref="Tournament.TitleEligible"/>.
    /// </summary>
    public static readonly TournamentStatus Completed = new(nameof(Completed), 1);

    /// <summary>
    /// The tournament was held but didn't finish as planned (e.g. finals cancelled for a state of
    /// emergency, seeding-based payout instead). Never title-eligible, regardless of entries.
    /// </summary>
    public static readonly TournamentStatus Truncated = new(nameof(Truncated), 2);

    /// <summary>
    /// No official NEBA event took place under this record — either nothing was bowled, or entries
    /// were too low to sanction it. Never stats- or title-eligible.
    /// </summary>
    public static readonly TournamentStatus Cancelled = new(nameof(Cancelled), 3);

    private TournamentStatus(string name, int value)
        : base(name, value)
    { }
}
