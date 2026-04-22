using Neba.Domain.Bowlers;
using Neba.Domain.Tournaments;

namespace Neba.Infrastructure.Database.Entities;

/// <summary>
/// Internal class used solely for EF Core mapping of champion bowler IDs.
/// This class exists only to satisfy EF Core's requirement that OwnsMany collections
/// must be of an entity type, not a primitive/value type.
/// Represents the many-to-many relationship between tournaments and bowlers.
/// </summary>
internal sealed class HistoricalTournamentChampion
{
    public int BowlerId { get; init; }

    public Bowler Bowler { get; init; } = null!;

    public int TournamentId { get; init; }

    public Tournament Tournament { get; init; } = null!;
}
