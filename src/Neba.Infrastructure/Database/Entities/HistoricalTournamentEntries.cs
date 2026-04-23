using Neba.Domain.Tournaments;

namespace Neba.Infrastructure.Database.Entities;

internal sealed class HistoricalTournamentEntries
{
    public int TournamentId { get; init; }

    public Tournament Tournament { get; init; } = null!;

    public int Entries { get; init; }
}