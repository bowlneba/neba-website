using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.EvictOilPatternRevealCache;
using Neba.Api.Messaging;

using Neba.Api.Features.Storage.Domain;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Tournaments.EditTournament;

internal sealed class EditTournamentCommandHandler(
        AppDbContext appDbContext,
        IFusionCache cache,
        IBackgroundJobScheduler backgroundJobScheduler,
        TimeProvider timeProvider)
    : ICommandHandler<EditTournamentCommand, Updated>
{
    public async Task<ErrorOr<Updated>> HandleAsync(EditTournamentCommand command, CancellationToken cancellationToken)
    {
        var tournament = await appDbContext.Tournaments
            .SingleOrDefaultAsync(t => t.Id == command.TournamentId, cancellationToken);

        if (tournament is null)
        {
            return TournamentErrors.TournamentNotFound(command.TournamentId);
        }

        var resolveResult = await TournamentSeasonAndPatternResolver.ResolveAsync(
            appDbContext,
            command.StartDate,
            command.EndDate,
            command.BowlingCenterId,
            command.OilPatternId,
            command.PatternLengthCategory,
            command.PatternRatioCategory,
            cancellationToken);

        if (resolveResult.IsError)
        {
            return resolveResult.Errors;
        }

        (Season season, PatternLengthCategory? patternLengthCategory, PatternRatioCategory? patternRatioCategory) = resolveResult.Value;

        var previousSeasonId = tournament.SeasonId;

        // Must snapshot before Update() — Logo is mutated in place, so reading it after the call
        // would return the new value, not the one being replaced (see EditSponsorCommandHandler).
        var previousLogo = tournament.Logo;

        var updateResult = tournament.Update(
            name: command.Name,
            tournamentType: command.TournamentType,
            startDate: command.StartDate,
            endDate: command.EndDate,
            seasonId: season.Id,
            statsEligible: command.StatsEligible,
            entryFee: command.EntryFee,
            nebaAddedMoney: command.NebaAddedMoney,
            bowlingCenterId: command.BowlingCenterId,
            externalRegistrationUrl: command.ExternalRegistrationUrl,
            logo: command.Logo,
            patternLengthCategory: patternLengthCategory,
            patternRatioCategory: patternRatioCategory,
            oilPatternRevealDateTime: command.OilPatternRevealDateTime);

        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        if (command.OilPatternId is { } attachedOilPatternId)
        {
            // Same hardcoded rounds Create uses — every tournament currently uses a single oil pattern
            // for the whole event, so there's no per-round pattern selection in the UI yet.
            var addOilPatternResult = tournament.AddOilPattern(
                attachedOilPatternId, TournamentRound.Qualifying, TournamentRound.MatchPlay);

            if (addOilPatternResult.IsError)
            {
                return addOilPatternResult.Errors;
            }
        }

        await TournamentPendingUploadCleaner.RemoveClaimedAsync(appDbContext, command.Logo, cancellationToken);

        await appDbContext.SaveChangesAsync(cancellationToken);

        await EvictCachesAsync(tournament.Id, previousSeasonId, season.Id, cancellationToken);

        ScheduleBackgroundJobs(command, previousLogo, tournament.Id, season.Id);

        return Result.Updated;
    }

    private async Task EvictCachesAsync(
        TournamentId tournamentId, SeasonId previousSeasonId, SeasonId currentSeasonId, CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync($"neba:tournaments:{tournamentId}", token: cancellationToken);
        await cache.RemoveByTagAsync($"neba:tournaments:{previousSeasonId}", token: cancellationToken);

        if (currentSeasonId != previousSeasonId)
        {
            await cache.RemoveByTagAsync($"neba:tournaments:{currentSeasonId}", token: cancellationToken);
        }
    }

    private void ScheduleBackgroundJobs(
        EditTournamentCommand command, StoredFile? previousLogo, TournamentId tournamentId, SeasonId seasonId)
    {
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

        if (command.OilPatternRevealDateTime is { } revealAt && revealAt > timeProvider.GetUtcNow())
        {
            backgroundJobScheduler.Schedule(
                new EvictOilPatternRevealCacheJob { TournamentId = tournamentId, SeasonId = seasonId },
                revealAt);
        }
    }
}