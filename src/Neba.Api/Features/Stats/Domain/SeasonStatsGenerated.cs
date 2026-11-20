using Neba.Api.Domain;
using Neba.Api.Features.Seasons.Domain;

namespace Neba.Api.Features.Stats.Domain;

/// <summary>Raised after a season's stats were regenerated. Published by the stats job, not an aggregate.</summary>
public sealed record SeasonStatsGenerated
    : IDomainEvent
{
    /// <summary>The season whose stats were regenerated.</summary>
    public required SeasonId SeasonId { get; init; }
}