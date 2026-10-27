using ErrorOr;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Neba.Api.BackgroundJobs;
using Neba.Api.Caching;
using Neba.Api.Database;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.AnnounceOilPatternToDiscord;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.EvictOilPatternRevealCache;
using Neba.Api.Messaging;
using Neba.Api.Security.Infrastructure.Authorization;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Tournaments.EditTournament;

internal sealed class EditTournamentCommandHandler(
        AppDbContext appDbContext,
        IFusionCache cache,
        IBackgroundJobScheduler backgroundJobScheduler,
        TimeProvider timeProvider,
        IPermissionDenialReader permissionDenialReader,
        IOptions<OilPatternAnnouncementSettings> announcementSettings)
    : ICommandHandler<EditTournamentCommand, Updated>
{
    public async Task<ErrorOr<Updated>> HandleAsync(EditTournamentCommand command, CancellationToken cancellationToken)
    {
        var tournament = await appDbContext.Tournaments
            .Include(t => t.OilPatterns)
            .SingleOrDefaultAsync(t => t.Id == command.TournamentId, cancellationToken);

        if (tournament is null)
        {
            return TournamentErrors.TournamentNotFound(command.TournamentId);
        }

        // A denial is read only when the logo changes, so an edit that leaves it alone skips the lookup.
        if (!TournamentLogoClaim.IsSameFile(command.Logo, tournament.Logo)
            && await permissionDenialReader.IsDeniedAsync(
                command.CallerUserId, Contracts.Security.Permissions.EditTournamentLogo, cancellationToken))
        {
            return TournamentErrors.LogoChangeDenied;
        }

        var logoClaim = await TournamentLogoClaim.EnsureClaimableAsync(
            appDbContext, command.Logo, tournament.Logo, cancellationToken);

        if (logoClaim.IsError)
        {
            return logoClaim.Errors;
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
        var previousStatsEligible = tournament.StatsEligible;
        var previousChampionDetails = (tournament.Name, tournament.EndDate, tournament.TournamentType);

        // Must snapshot before Update() — Logo is mutated in place, so reading it after the call
        // would return the new value, not the one being replaced (see EditSponsorCommandHandler).
        var previousLogo = tournament.Logo;
        var previousRevealDateTime = tournament.OilPatternRevealDateTime;

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
            logo: TournamentLogoClaim.Resolve(command.Logo, previousLogo),
            patternLengthCategory: patternLengthCategory,
            patternRatioCategory: patternRatioCategory,
            oilPatternRevealDateTime: command.OilPatternRevealDateTime);

        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        if (command.OilPatternId is { } attachedOilPatternId
            && tournament.OilPatterns.All(oilPattern => oilPattern.OilPatternId != attachedOilPatternId))
        {
            // Same hardcoded rounds Create uses — every tournament currently uses a single oil pattern
            // for the whole event, so there's no per-round pattern selection in the UI yet.
            // Skipped when already attached: re-editing a tournament resubmits the same oil pattern on
            // every save, and AddOilPattern would otherwise error on the rounds already being associated.
            var addOilPatternResult = tournament.AddOilPattern(
                attachedOilPatternId, TournamentRound.Qualifying, TournamentRound.MatchPlay);

            if (addOilPatternResult.IsError)
            {
                return addOilPatternResult.Errors;
            }
        }

        await TournamentPendingUploadCleaner.RemoveClaimedAsync(appDbContext, command.Logo, cancellationToken);

        await appDbContext.SaveChangesAsync(cancellationToken);

        await EvictCachesAsync(
            tournament.Id,
            previousSeasonId,
            season.Id,
            previousStatsEligible != tournament.StatsEligible,
            previousChampionDetails != (tournament.Name, tournament.EndDate, tournament.TournamentType),
            cancellationToken);

        ScheduleBackgroundJobs(command, previousLogo, previousRevealDateTime, tournament, season.Id);

        return Result.Updated;
    }

    private async Task EvictCachesAsync(
        TournamentId tournamentId,
        SeasonId previousSeasonId,
        SeasonId currentSeasonId,
        bool statsEligibleChanged,
        bool championDetailsChanged,
        CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync($"neba:tournaments:{tournamentId}", token: cancellationToken);
        await cache.RemoveByTagAsync($"neba:tournaments:{previousSeasonId}", token: cancellationToken);

        if (currentSeasonId != previousSeasonId)
        {
            await cache.RemoveByTagAsync($"neba:tournaments:{currentSeasonId}", token: cancellationToken);
        }

        // The champions list and each bowler's titles project the tournament's name, end date and type.
        if (championDetailsChanged)
        {
            await cache.RemoveByTagAsync("neba:tournaments:champions", token: cancellationToken);
            await cache.RemoveByTagAsync("neba:bowlers", token: cancellationToken);
        }

        // Season stat minimums count stats-eligible tournaments, so flipping eligibility or moving a
        // tournament between seasons makes the cached season stats stale.
        if (statsEligibleChanged || currentSeasonId != previousSeasonId)
        {
            await cache.RemoveByTagAsync(CacheDescriptors.Stats.SeasonTag(previousSeasonId), token: cancellationToken);

            if (currentSeasonId != previousSeasonId)
            {
                await cache.RemoveByTagAsync(CacheDescriptors.Stats.SeasonTag(currentSeasonId), token: cancellationToken);
            }
        }
    }

    private void ScheduleBackgroundJobs(
        EditTournamentCommand command, StoredFile? previousLogo, DateTimeOffset? previousRevealDateTime, Tournament tournament, SeasonId seasonId)
    {
        TournamentLogoCleanup.ScheduleDeleteOfReplacedLogo(backgroundJobScheduler, previousLogo, command.Logo);

        if (command.OilPatternRevealDateTime is { } revealAt && revealAt > timeProvider.GetUtcNow())
        {
            backgroundJobScheduler.Schedule(
                new EvictOilPatternRevealCacheJob { TournamentId = tournament.Id, SeasonId = seasonId },
                revealAt);
        }

        backgroundJobScheduler.ScheduleOilPatternAnnouncement(
            tournament, timeProvider.GetUtcNow(), announcementSettings.Value.Delay, previousRevealDateTime);
    }
}