namespace Neba.Api.Contracts.Seasons.ListSeasons;

/// <summary>
/// Represents a season returned by the API.
/// </summary>
public sealed record SeasonResponse
{
    /// <summary>
    /// The unique identifier of the season as a ULID string.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// A human-readable description of the season, e.g. "2024-2025 Season".
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The date on which the season begins.
    /// </summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>
    /// The date on which the season ends.
    /// </summary>
    public required DateOnly EndDate { get; init; }
}