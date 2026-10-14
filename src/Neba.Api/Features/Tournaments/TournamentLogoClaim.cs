using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments;

/// <summary>
/// Confirms a caller-supplied logo points at a file this API staged, so a request cannot attach
/// (or later delete) an arbitrary blob by naming its container and path.
/// </summary>
internal static class TournamentLogoClaim
{
    /// <summary>
    /// A logo is claimable when it is null (removal), when it already is the tournament's current
    /// logo (a re-save resubmits it), or when it matches a pending upload.
    /// </summary>
    public static async Task<ErrorOr<Success>> EnsureClaimableAsync(
        AppDbContext appDbContext,
        StoredFile? logo,
        StoredFile? currentLogo,
        CancellationToken cancellationToken)
    {
        if (logo is null
            || (currentLogo is not null
                && currentLogo.Container == logo.Container
                && currentLogo.Path == logo.Path))
        {
            return Result.Success;
        }

        var isPending = await appDbContext.PendingUploads
            .AnyAsync(pending => pending.Container == logo.Container && pending.Path == logo.Path, cancellationToken);

        return isPending ? Result.Success : TournamentErrors.LogoNotUploaded;
    }
}
