using Neba.Application.Caching;
using Neba.Application.Messaging;
using Neba.Domain.Tournaments;

namespace Neba.Application.Tournaments.GetTournament;

/// <summary>
/// Query for retrieving detailed information about a specific tournament, identified by its unique TournamentId. The query returns a TournamentDetailDto containing comprehensive details about the tournament, including its name, date, location, participants, and results. This query is used to display tournament details in the application and may be cached for performance optimization.
/// </summary>
public sealed record GetTournamentDetailQuery
    : ICachedQuery<TournamentDetailDto>
{
    /// <summary>
    /// The unique identifier of the tournament for which details are being requested.
    /// </summary>
    public required TournamentId Id { get; init; }

    /// <inheritdoc />
    public CacheDescriptor Cache
        => CacheDescriptors.Tournaments.TournamentDetail(Id);

    /// <inheritdoc />
    public TimeSpan Expiry
        => TimeSpan.FromDays(5);
}