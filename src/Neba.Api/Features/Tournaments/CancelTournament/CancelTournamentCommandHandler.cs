using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Tournaments.CancelTournament;

internal sealed class CancelTournamentCommandHandler(AppDbContext appDbContext, IFusionCache cache)
    : ICommandHandler<CancelTournamentCommand, Success>
{
    public async Task<ErrorOr<Success>> HandleAsync(CancelTournamentCommand command, CancellationToken cancellationToken)
    {
        var tournament = await appDbContext.Tournaments
            .SingleOrDefaultAsync(t => t.Id == command.TournamentId, cancellationToken);

        if (tournament is null)
        {
            return TournamentErrors.TournamentNotFound(command.TournamentId);
        }

        var cancelResult = tournament.CancelTournament();
        if (cancelResult.IsError)
        {
            return cancelResult.Errors;
        }

        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync($"neba:tournaments:{tournament.Id}", token: cancellationToken);
        await cache.RemoveByTagAsync($"neba:tournaments:{tournament.SeasonId}", token: cancellationToken);

        return Result.Success;
    }
}