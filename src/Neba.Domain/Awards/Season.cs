namespace Neba.Domain.Awards;

/// <summary>
/// The temporal boundary over which awards are calculated and assigned.
/// Must be marked <see cref="Complete"/> before any awards may be assigned to bowlers.
/// </summary>
public sealed class Season
    : AggregateRoot
{
    /// <summary>
    /// System-generated unique identifier.
    /// </summary>
    public required SeasonId Id { get; init; }

    /// <summary>
    /// Human-readable label for the season (e.g., "2022 Season", "2020–2021 Season").
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The first date of the season. Inclusive.
    /// </summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>
    /// The last date of the season. Inclusive.
    /// </summary>
    public required DateOnly EndDate { get; init; }

    /// <summary>
    /// Whether the season has been closed and awards may be assigned.
    /// Once <see langword="true"/>, a season may not be reopened.
    /// </summary>
    public bool Complete { get; init; }

    private readonly List<BowlerOfTheYearAward> _bowlerOfTheYearAwards = [];

    /// <summary>
    /// Bowler of the Year awards assigned to this season.
    /// </summary>
    public IReadOnlyCollection<BowlerOfTheYearAward> BowlerOfTheYearAwards
        => _bowlerOfTheYearAwards.AsReadOnly();

    private readonly List<HighAverageAward> _highAverageAwards = [];

    /// <summary>
    /// High Average awards assigned to this season.
    /// </summary>
    public IReadOnlyCollection<HighAverageAward> HighAverageAwards
        => _highAverageAwards.AsReadOnly();

    private readonly List<HighBlockAward> _highBlockAwards = [];

    /// <summary>
    /// High Block awards assigned to this season.
    /// </summary>
    public IReadOnlyCollection<HighBlockAward> HighBlockAwards
        => _highBlockAwards.AsReadOnly();
}
