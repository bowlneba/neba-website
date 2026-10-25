using Neba.Api.Contracts.OpenApi;

namespace Neba.Api.Contracts.HallOfFame.ListHallOfFameInductions;

/// <summary>
/// Data transfer object representing a Hall of Fame induction used by the API layer. This is designed to be easily serializable and may differ from the internal application DTO to better suit API consumers.
/// </summary>
public sealed record HallOfFameInductionResponse
{
    /// <summary>
    /// The ULID string identifying the induction. Used to address the induction's photo.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The year the induction occurred.
    /// </summary>
    public required int Year { get; init; }

    /// <summary>
    /// Formal name of the inducted bowler, formatted as "First Last". This is a simplified representation for API consumers, as opposed to the more complex Name type used internally in the application layer.
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// The categories associated with this induction, represented as a collection of strings for easier consumption by API clients. This is a simplified representation compared to the HallOfFameCategory type used internally in the application layer.
    /// </summary>
    [OpenApiSmartEnum("HallOfFameCategory")]
    public required IReadOnlyCollection<string> Categories { get; init; }

    /// <summary>
    /// A public URI pointing to the bowler's photo, if available. This is included in the API response to allow clients to easily access the induction photo without needing to understand the underlying storage details.
    /// </summary>
    public Uri? PhotoUri { get; init; }

    /// <summary>
    /// Horizontal focal point of the photo, 0 to 100 percent from the left; null when no focal point is set.
    /// </summary>
    public int? PhotoFocalX { get; init; }

    /// <summary>
    /// Vertical focal point of the photo, 0 to 100 percent from the top; null when no focal point is set.
    /// </summary>
    public int? PhotoFocalY { get; init; }

    /// <summary>
    /// Zoom of the photo about its focal point, 100 to 300 percent (100 is no zoom); null when none is set.
    /// </summary>
    public int? PhotoZoom { get; init; }
}