using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Storage.Domain;

namespace Neba.Api.Features.Sponsors;

/// <summary>
/// Removes the pending-upload record for a sponsor logo once it has been claimed by the sponsor.
/// Shared by <see cref="CreateSponsor.CreateSponsorCommandHandler"/> and
/// <see cref="EditSponsor.EditSponsorCommandHandler"/>.
/// </summary>
internal static class SponsorPendingUploadCleaner
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