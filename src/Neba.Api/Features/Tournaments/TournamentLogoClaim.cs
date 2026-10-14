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
    /// Returns the tournament's stored logo when <paramref name="logo"/> names the same file, so a
    /// re-save never lets the caller overwrite the stored content type or size.
    /// </summary>
    public static StoredFile? Resolve(StoredFile? logo, StoredFile? currentLogo)
        => logo is not null && NamesCurrentFile(logo, currentLogo) ? currentLogo : logo;

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
        if (logo is null || NamesCurrentFile(logo, currentLogo))
        {
            return Result.Success;
        }

        var isPending = await appDbContext.PendingUploads
            .AnyAsync(pending => pending.Container == logo.Container && pending.Path == logo.Path, cancellationToken);

        return isPending ? Result.Success : TournamentErrors.LogoNotUploaded;
    }

    /// <summary>
    /// True when both name the same file, or both are null (no logo before and after).
    /// </summary>
    public static bool IsSameFile(StoredFile? logo, StoredFile? currentLogo)
        => logo is null
            ? currentLogo is null
            : NamesCurrentFile(logo, currentLogo);

    private static bool NamesCurrentFile(StoredFile logo, StoredFile? currentLogo)
        => currentLogo is not null
            && currentLogo.Container == logo.Container
            && currentLogo.Path == logo.Path;
}