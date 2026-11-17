using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Database.Configurations;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.EditTournament;
using Neba.Api.Messaging;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Tournaments.DeleteTournament;

internal sealed class DeleteTournamentCommandHandler(
        AppDbContext appDbContext,
        IBackgroundJobScheduler backgroundJobScheduler,
        IFusionCache cache)
    : ICommandHandler<DeleteTournamentCommand, Deleted>
{
    public async Task<ErrorOr<Deleted>> HandleAsync(DeleteTournamentCommand command, CancellationToken cancellationToken)
    {
        var tournament = await appDbContext.Tournaments
            .SingleOrDefaultAsync(t => t.Id == command.TournamentId, cancellationToken);

        if (tournament is null)
        {
            return Result.Deleted;
        }

        // A Series Stage references the tournament with a foreign key, so deleting it would fail as a 500.
        var seriesName = await appDbContext.Series
            .Where(series => series.Stages.Any(stage => stage.TournamentId == command.TournamentId))
            .Select(series => series.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (seriesName is not null)
        {
            return TournamentErrors.IsSeriesStage(command.TournamentId, seriesName);
        }

        if (await HasHistoricalRecordsAsync(tournament, cancellationToken))
        {
            return TournamentErrors.HasHistoricalRecords(command.TournamentId);
        }

        var seasonId = tournament.SeasonId;
        var logo = tournament.Logo;

        // Found before the delete: the database clears each successor's link to this tournament, and the walk needs it.
        var successors = await TournamentSuccessorCacheEviction.FindAsync(appDbContext, tournament.Id, cancellationToken);

        appDbContext.Tournaments.Remove(tournament);
        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync($"neba:tournaments:{tournament.Id}", token: cancellationToken);
        await cache.RemoveByTagAsync($"neba:tournaments:{seasonId}", token: cancellationToken);
        await TournamentSuccessorCacheEviction.EvictAsync(cache, successors, cancellationToken);

        if (logo is not null)
        {
            backgroundJobScheduler.Enqueue(new DeleteTournamentFilesJob
            {
                Files = [new TournamentFileReference { Container = logo.Container, Path = logo.Path }]
            });
        }

        return Result.Deleted;
    }

    private async Task<bool> HasHistoricalRecordsAsync(
        Tournament tournament, CancellationToken cancellationToken)
    {
        var tournamentDbId = appDbContext.Entry(tournament)
            .Property<int>(ShadowIdConfiguration.DefaultPropertyName).CurrentValue;

        return await appDbContext.HistoricalTournamentChampions
                .AnyAsync(c => c.TournamentId == tournamentDbId, cancellationToken)
            || await appDbContext.HistoricalTournamentEntries
                .AnyAsync(e => e.TournamentId == tournamentDbId, cancellationToken)
            || await appDbContext.HistoricalTournamentResults
                .AnyAsync(r => r.TournamentId == tournamentDbId, cancellationToken);
    }
}