using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.EditTournament;
using Neba.Api.Messaging;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Tournaments.EditTournamentLogo;

internal sealed class EditTournamentLogoCommandHandler(
        AppDbContext appDbContext,
        IFusionCache cache,
        IBackgroundJobScheduler backgroundJobScheduler)
    : ICommandHandler<EditTournamentLogoCommand, Updated>
{
    public async Task<ErrorOr<Updated>> HandleAsync(EditTournamentLogoCommand command, CancellationToken cancellationToken)
    {
        var tournament = await appDbContext.Tournaments
            .SingleOrDefaultAsync(t => t.Id == command.TournamentId, cancellationToken);

        if (tournament is null)
        {
            return TournamentErrors.TournamentNotFound(command.TournamentId);
        }

        var logoClaim = await TournamentLogoClaim.EnsureClaimableAsync(
            appDbContext, command.Logo, tournament.Logo, cancellationToken);

        if (logoClaim.IsError)
        {
            return logoClaim.Errors;
        }

        // Snapshot before ChangeLogo: Logo is mutated in place, so reading it afterwards returns the new value.
        var previousLogo = tournament.Logo;

        tournament.ChangeLogo(command.Logo);

        await TournamentPendingUploadCleaner.RemoveClaimedAsync(appDbContext, command.Logo, cancellationToken);

        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync($"neba:tournaments:{tournament.Id}", token: cancellationToken);
        await cache.RemoveByTagAsync($"neba:tournaments:{tournament.SeasonId}", token: cancellationToken);

        if (previousLogo is not null && previousLogo != command.Logo)
        {
            backgroundJobScheduler.Enqueue(new DeleteTournamentFilesJob
            {
                Files =
                [
                    new TournamentFileReference { Container = previousLogo.Container, Path = previousLogo.Path }
                ]
            });
        }

        return Result.Updated;
    }
}
