using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Features.Series;

internal static class SeriesConcurrency
{
    /// <summary>Saves, turning a race with an advancement job (the Version token) into a 409 instead of a 500.</summary>
    public static async Task<ErrorOr<Success>> SaveOrConflictAsync(AppDbContext appDbContext, CancellationToken cancellationToken)
    {
        try
        {
            await appDbContext.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            return SeriesAdvancementErrors.ConcurrentChange;
        }
    }
}