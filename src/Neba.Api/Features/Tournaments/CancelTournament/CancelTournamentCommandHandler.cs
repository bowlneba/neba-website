using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Caching;
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

        var previousTournamentId = tournament.PreviousTournamentId;

        var cancelResult = tournament.CancelTournament();
        if (cancelResult.IsError)
        {
            return cancelResult.Errors;
        }

        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync($"neba:tournaments:{tournament.Id}", token: cancellationToken);
        await cache.RemoveByTagAsync($"neba:tournaments:{tournament.SeasonId}", token: cancellationToken);

        // A Series shows its Stage tournaments' names, dates and types, and the next Stage for its list.
        await cache.RemoveByTagAsync(CacheDescriptors.Series.Tag, token: cancellationToken);

        // The previous tournament has no next tournament now; its season list shows that.
        await TournamentSuccessorCacheEviction.EvictPredecessorSeasonsAsync(
            appDbContext, cache, [previousTournamentId], cancellationToken);

        return Result.Success;
    }
}