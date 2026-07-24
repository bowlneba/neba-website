using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.EvictOilPatternRevealCache;
using Neba.Api.Messaging;

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

        var season = await appDbContext.Seasons
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.StartDate <= command.StartDate && s.EndDate >= command.EndDate, cancellationToken);

        if (season is null)
        {
            return TournamentErrors.NoSeasonForDates(command.StartDate, command.EndDate);
        }

        if (command.BowlingCenterId is { } bowlingCenterId
            && !await appDbContext.BowlingCenters.AnyAsync(bc => bc.CertificationNumber == bowlingCenterId, cancellationToken))
        {
            return TournamentErrors.BowlingCenterNotFound(bowlingCenterId);
        }

        var patternLengthCategory = command.PatternLengthCategory;
        var patternRatioCategory = command.PatternRatioCategory;

        if (command.OilPatternId is { } oilPatternId)
        {
            var oilPattern = await appDbContext.OilPatterns
                .AsNoTracking()
                .SingleOrDefaultAsync(p => p.Id == oilPatternId, cancellationToken);

            if (oilPattern is null)
            {
                return TournamentErrors.OilPatternNotFound(oilPatternId);
            }

            patternLengthCategory = oilPattern.LengthCategory;
            patternRatioCategory = oilPattern.RatioCategory;
        }

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

        await cache.RemoveByTagAsync($"neba:tournaments:{tournament.Id}", token: cancellationToken);
        await cache.RemoveByTagAsync($"neba:tournaments:{previousSeasonId}", token: cancellationToken);

        if (season.Id != previousSeasonId)
        {
            await cache.RemoveByTagAsync($"neba:tournaments:{season.Id}", token: cancellationToken);
        }

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
                new EvictOilPatternRevealCacheJob { TournamentId = tournament.Id, SeasonId = season.Id },
                revealAt);
        }

        return Result.Updated;
    }
}