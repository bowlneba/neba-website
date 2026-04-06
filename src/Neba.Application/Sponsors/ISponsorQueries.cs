namespace Neba.Application.Sponsors;

/// <summary>
/// Defines queries for retrieving sponsor data.
/// </summary>
public interface ISponsorQueries
{
    /// <summary>
    /// Retrieves a list of all active sponsors with summary information.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A read-only collection of sponsor summary DTOs.</returns>
    Task<IReadOnlyCollection<SponsorSummaryDto>> GetActiveSponsorsAsync(CancellationToken cancellationToken);
}