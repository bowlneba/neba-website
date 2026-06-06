using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.HallOfFame.Domain;

namespace Neba.Api.Features.HallOfFame.ListHallOfFameInductions;

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
    /// A public URI pointing to the bowler's photo; null if no photo is available.
    /// </summary>
    public Uri? PhotoUri { get; init; }
}