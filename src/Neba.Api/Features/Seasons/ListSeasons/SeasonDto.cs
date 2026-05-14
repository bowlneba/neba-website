using Neba.Domain.Seasons;

namespace Neba.Api.Features.Seasons.ListSeasons;

/// <summary>
/// Data Transfer Object (DTO) representing a Season in the NEBA application. This DTO is used to transfer season data between the application layers, particularly for read operations. It includes essential information about a season, such as its unique identifier, description, and date range. The SeasonDto is designed to be immutable and is intended for use in scenarios where season details need to be displayed or processed without exposing the underlying domain entities directly.
/// </summary>
public sealed record SeasonDto
{
    /// <summary>
    /// The unique identifier of the Season. This is typically a ULID that serves as the primary key for season records in the database. It is used to reference the season in various operations, such as retrieving associated statistics or determining eligibility for awards.
    /// </summary>
    public required SeasonId Id { get; init; }

    /// <summary>
    /// A human-readable description of the Season, such as "2024-2025 Season". This field provides context and clarity when displaying season information in the user interface or reports. It is intended to be concise yet descriptive enough to distinguish between different seasons at a glance.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The date on which the Season begins. This field is crucial for determining the eligibility of bowler performances and statistics, as well as for scheduling tournaments and events within the season. The StartDate helps to establish the temporal boundaries of the season and is used in various calculations related to awards and standings.
    /// </summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>
    /// The date on which the Season ends. Similar to the StartDate, the EndDate is essential for defining the duration of the season and for determining which performances and statistics fall within the season's scope. The EndDate is used in conjunction with the StartDate to establish the season's timeframe and to ensure that all relevant data is accurately categorized for reporting and award purposes.
    /// </summary>
    public required DateOnly EndDate { get; init; }
}