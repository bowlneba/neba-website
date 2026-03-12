namespace Neba.Api.Contracts.HallOfFame.ListHallOfFameInductions;

/// <summary>
/// Data transfer object representing a Hall of Fame induction used by the API layer. This is designed to be easily serializable and may differ from the internal application DTO to better suit API consumers.
/// </summary>
public sealed record HallOfFameInductionResponse
{
    /// <summary>
    /// The year the induction occurred.
    /// </summary>
    public required int Year { get; set; }

    /// <summary>
    /// Formal name of the inducted bowler, formatted as "First Last". This is a simplified representation for API consumers, as opposed to the more complex Name type used internally in the application layer.
    /// </summary>
    public required string BowlerName { get; set; }

    /// <summary>
    /// The categories associated with this induction, represented as a collection of strings for easier consumption by API clients. This is a simplified representation compared to the HallOfFameCategory type used internally in the application layer.
    /// </summary>
    public required IReadOnlyCollection<string> Categories { get; set; }

    /// <summary>
    /// A public URI pointing to the bowler's photo, if available. This is included in the API response to allow clients to easily access the induction photo without needing to understand the underlying storage details.
    /// </summary>
    public Uri? PhotoUri { get; set; }
}