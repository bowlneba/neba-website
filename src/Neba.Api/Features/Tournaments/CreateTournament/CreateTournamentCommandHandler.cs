using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.EvictOilPatternRevealCache;
using Neba.Api.Messaging;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Tournaments.CreateTournament;

internal sealed class CreateTournamentCommandHandler(
    AppDbContext appDbContext,
    IFusionCache cache,
    IBackgroundJobScheduler jobScheduler,
    TimeProvider timeProvider)
    : ICommandHandler<CreateTournamentCommand, TournamentId>
{
    public async Task<ErrorOr<TournamentId>> HandleAsync(CreateTournamentCommand command, CancellationToken cancellationToken)
    {
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

        var (season, patternLengthCategory, patternRatioCategory) = resolveResult.Value;

        var tournamentResult = Tournament.Create(
            name: command.Name,
            tournamentType: command.TournamentType,
            startDate: command.StartDate,
            endDate: command.EndDate,
            seasonId: season.Id,
            statsEligible: command.StatsEligible,
            entryFee: command.EntryFee,
            bowlingCenterId: command.BowlingCenterId,
            externalRegistrationUrl: command.ExternalRegistrationUrl,
            logo: command.Logo,
            patternLengthCategory: patternLengthCategory,
            patternRatioCategory: patternRatioCategory,
            oilPatternRevealDateTime: command.OilPatternRevealDateTime,
            nebaAddedMoney: command.NebaAddedMoney);

        if (tournamentResult.IsError)
        {
            return tournamentResult.Errors;
        }

        var tournament = tournamentResult.Value;

        if (command.OilPatternId is { } attachedOilPatternId)
        {
            // Every tournament currently uses a single oil pattern for the whole event — there's no
            // per-round pattern selection in the UI yet — so it's hardcoded to Qualifying + Match Play
            // (bitmask 1 | 4 = 5) rather than asking the caller to specify rounds.
            var addOilPatternResult = tournament.AddOilPattern(
                attachedOilPatternId, TournamentRound.Qualifying, TournamentRound.MatchPlay);

            if (addOilPatternResult.IsError)
            {
                return addOilPatternResult.Errors;
            }
        }

        await appDbContext.Tournaments.AddAsync(tournament, cancellationToken);

        await TournamentPendingUploadCleaner.RemoveClaimedAsync(appDbContext, tournament.Logo, cancellationToken);

        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync($"neba:tournaments:{season.Id}", token: cancellationToken);

        if (command.OilPatternRevealDateTime is { } revealAt && revealAt > timeProvider.GetUtcNow())
        {
            jobScheduler.Schedule(
                new EvictOilPatternRevealCacheJob { TournamentId = tournament.Id, SeasonId = season.Id },
                revealAt);
        }

        return tournament.Id;
    }
}