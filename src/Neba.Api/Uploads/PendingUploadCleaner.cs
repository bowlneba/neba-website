using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Storage.Domain;

namespace Neba.Api.Uploads;

/// <summary>
/// Removes the pending-upload record for a file once an entity has claimed it.
/// </summary>
internal static class PendingUploadCleaner
{
    public static async Task RemoveClaimedAsync(AppDbContext appDbContext, StoredFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return;
        }

        var claimed = await appDbContext.PendingUploads
            .Where(pending => pending.Container == file.Container && pending.Path == file.Path)
            .ToListAsync(cancellationToken);

        appDbContext.PendingUploads.RemoveRange(claimed);
    }
}
