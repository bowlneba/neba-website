using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Features.Series;

internal static class SeriesFinaleChecks
{
    public static async Task<ErrorOr<Success>> EnsureBowlingCenterUsableAsync(
        AppDbContext appDbContext,
        SeriesFinaleDefinition? finale,
        CertificationNumber? currentCenterId,
        CancellationToken cancellationToken)
    {
        if (finale?.BowlingCenterId is not { } centerId || centerId == currentCenterId)
        {
            return Result.Success; // no Finale, host still to be announced, or the center the Finale already has
        }

        var usable = await appDbContext.BowlingCenters
            .AnyAsync(center => center.CertificationNumber == centerId && center.Status == BowlingCenterStatus.Open, cancellationToken);

        return usable ? Result.Success : SeriesErrors.FinaleBowlingCenterNotAvailable(centerId.Value);
    }
}
