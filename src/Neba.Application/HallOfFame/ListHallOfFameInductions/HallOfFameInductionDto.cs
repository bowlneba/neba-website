
using Neba.Domain.Bowlers;
using Neba.Domain.HallOfFame;

namespace Neba.Application.HallOfFame.ListHallOfFameInductions;

/// <summary>
/// Data transfer object representing a Hall of Fame induction used by the application layer.
/// </summary>
public sealed record HallOfFameInductionDto
{
    /// <summary>
    /// The year the induction occurred.
    /// </summary>
    public required int Year { get; init; }

    /// <summary>
    /// The full name of the inducted bowler.
    /// </summary>
    public required Name BowlerName { get; init; }

    /// <summary>
    /// The categories associated with this induction.
    /// </summary>
    public required IReadOnlyCollection<HallOfFameCategory> Categories { get; init; }

    /// <summary>
    /// The name of the storage container where the induction photo is stored, if available.
    /// </summary>
    public string? PhotoContainer { get; init; }

    /// <summary>
    /// The path within the storage container to the induction photo, if available.
    /// </summary>
    public string? PhotoPath { get; init; }

    /// <summary>
    /// A public URI pointing to the bowler's photo, if available. This is populated by the application layer after retrieving the induction data.
    /// </summary>
    public Uri? PhotoUri { get; internal set; }
}