namespace Neba.Api.Database.Entities;

internal sealed class HistoricalTournamentEntry
{
    public int TournamentId { get; init; }

    public Tournament Tournament { get; init; } = null!;

    public int Entries { get; init; }
}