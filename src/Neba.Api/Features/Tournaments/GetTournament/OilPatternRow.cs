using Neba.Application.Tournaments;
using Neba.Domain.Tournaments;

namespace Neba.Api.Features.Tournaments.GetTournament;

internal sealed partial class GetTournamentQueryHandler
{
    private sealed record OilPatternRow
    {
        public OilPatternDto OilPattern { get; init; } = null!;

        public IReadOnlyCollection<TournamentRound> TournamentRounds { get; init; } = [];
    }
}