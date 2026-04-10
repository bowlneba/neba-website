using Neba.Application.Sponsors.GetSponsorDetail;

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

    /// <summary>
    /// Retrieves detailed information about a specific sponsor identified by its slug. Returns <c>null</c> if no sponsor with the given slug exists.
    /// </summary>
    /// <param name="slug">The slug of the sponsor to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="SponsorDetailDto"/> if found; otherwise, <c>null</c>.</returns>
    Task<SponsorDetailDto?> GetSponsorAsync(string slug, CancellationToken cancellationToken);
}