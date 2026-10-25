namespace Neba.Website.Server.HallOfFame;

/// <summary>
/// View model used by the Hall of Fame UI to display a single induction entry.
/// </summary>
public sealed record HallOfFameInductionViewModel
{
    /// <summary>
    /// The ULID string identifying the induction. Used to address the induction's photo.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The full display name of the bowler inducted into the Hall of Fame.
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// The calendar year the bowler was inducted.
    /// </summary>
    public required int InductionYear { get; init; }

    /// <summary>
    /// Human-friendly category names describing the induction reason(s).
    /// Multiple categories may be present; these are the display strings
    /// (for example, "Superior Performance", "Meritorious Service").
    /// </summary>
    public required IReadOnlyCollection<string> Categories { get; init; }

    /// <summary>
    /// URI of the bowler's photo, if available.
    /// </summary>
    /// <value>
    /// The URI of the photo, or <c>null</c> if no photo is available.
    /// </value>
    public Uri? PhotoUri { get; init; }

    /// <summary>
    /// Saved horizontal focal point of the photo, 0 to 100; null when none is saved.
    /// </summary>
    public int? PhotoFocalX { get; init; }

    /// <summary>
    /// Saved vertical focal point of the photo, 0 to 100; null when none is saved.
    /// </summary>
    public int? PhotoFocalY { get; init; }

    /// <summary>
    /// Saved zoom of the photo about its focal point, 100 to 300 percent; null when none is saved.
    /// </summary>
    public int? PhotoZoom { get; init; }
}