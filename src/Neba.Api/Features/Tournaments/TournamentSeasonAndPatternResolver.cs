using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments;

/// <summary>
/// Resolves the season a tournament's dates fall in, confirms an optional bowling center exists,
/// and (when an oil pattern ID is given) derives the pattern length/ratio categories from it.
/// Shared by the create and edit command handlers, which perform this same lookup/validation
/// before touching the aggregate.
/// </summary>
internal static class TournamentSeasonAndPatternResolver
{
    public static async Task<ErrorOr<(Season Season, PatternLengthCategory? PatternLengthCategory, PatternRatioCategory? PatternRatioCategory)>> ResolveAsync(
        AppDbContext appDbContext,
        DateOnly startDate,
        DateOnly endDate,
        CertificationNumber? bowlingCenterId,
        OilPatternId? oilPatternId,
        PatternLengthCategory? patternLengthCategory,
        PatternRatioCategory? patternRatioCategory,
        CancellationToken cancellationToken)
    {
        var season = await appDbContext.Seasons
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.StartDate <= startDate && s.EndDate >= endDate, cancellationToken);

        if (season is null)
        {
            return TournamentErrors.NoSeasonForDates(startDate, endDate);
        }

        if (bowlingCenterId is { } certificationNumber
            && !await appDbContext.BowlingCenters.AnyAsync(bc => bc.CertificationNumber == certificationNumber, cancellationToken))
        {
            return TournamentErrors.BowlingCenterNotFound(certificationNumber);
        }

        if (oilPatternId is { } id)
        {
            var oilPattern = await appDbContext.OilPatterns
                .AsNoTracking()
                .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (oilPattern is null)
            {
                return TournamentErrors.OilPatternNotFound(id);
            }

            patternLengthCategory = oilPattern.LengthCategory;
            patternRatioCategory = oilPattern.RatioCategory;
        }

        return (season, patternLengthCategory, patternRatioCategory);
    }
}