using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Storage.Domain;

namespace Neba.Api.Features.Tournaments;

internal static class TournamentPendingUploadCleaner
{
    public static async Task RemoveClaimedAsync(AppDbContext appDbContext, StoredFile? logo, CancellationToken cancellationToken)
    {
        if (logo is null)
        {
            return;
        }

        var claimed = await appDbContext.PendingUploads
            .Where(pending => pending.Container == logo.Container && pending.Path == logo.Path)
            .ToListAsync(cancellationToken);

        appDbContext.PendingUploads.RemoveRange(claimed);
    }
}