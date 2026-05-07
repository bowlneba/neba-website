using Neba.Domain.Bowlers;
using Neba.Domain.Tournaments;

namespace Neba.Infrastructure.Database.Entities;

internal sealed class HistoricalTournamentChampion
{
    public int BowlerId { get; init; }

    public Bowler Bowler { get; init; } = null!;

    public int TournamentId { get; init; }

    public Tournament Tournament { get; init; } = null!;
}