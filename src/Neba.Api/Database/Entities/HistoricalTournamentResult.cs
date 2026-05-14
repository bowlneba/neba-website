using Neba.Domain.Bowlers;
using Neba.Domain.Tournaments;

namespace Neba.Api.Database.Entities;

internal sealed class HistoricalTournamentResult
{
    public int BowlerId { get; init; }

    public Bowler Bowler { get; init; } = null!;

    public int TournamentId { get; init; }

    public Tournament Tournament { get; init; } = null!;

    public int? Place { get; init; }

    public decimal PrizeMoney { get; init; }

    public int Points { get; init; }

    public int? SideCutId { get; init; }

    public SideCut? SideCut { get; init; }
}