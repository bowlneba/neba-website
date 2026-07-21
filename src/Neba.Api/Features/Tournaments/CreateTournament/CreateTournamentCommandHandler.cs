using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Tournaments.CreateTournament;

internal sealed class CreateTournamentCommandHandler(AppDbContext appDbContext, IFusionCache cache)
    : ICommandHandler<CreateTournamentCommand, TournamentId>
{
    public async Task<ErrorOr<TournamentId>> HandleAsync(CreateTournamentCommand command, CancellationToken cancellationToken)
    {
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
            patternRatioCategory: patternRatioCategory);

        if (tournamentResult.IsError)
        {
            return tournamentResult.Errors;
        }

        var tournament = tournamentResult.Value;

        await appDbContext.Tournaments.AddAsync(tournament, cancellationToken);

        await TournamentPendingUploadCleaner.RemoveClaimedAsync(appDbContext, tournament.Logo, cancellationToken);

        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync($"neba:tournaments:{season.Id}", token: cancellationToken);

        return tournament.Id;
    }
}