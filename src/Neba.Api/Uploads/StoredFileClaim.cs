using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Storage.Domain;

namespace Neba.Api.Uploads;

/// <summary>
/// Confirms a caller-supplied file is one this API staged, so a request cannot attach (or later delete)
/// an arbitrary blob by naming its container and path.
/// </summary>
internal static class StoredFileClaim
{
    /// <summary>
    /// Returns <paramref name="current"/> when <paramref name="file"/> names the same blob, so a re-save
    /// never lets the caller overwrite the stored content type or size.
    /// </summary>
    public static StoredFile? Resolve(StoredFile? file, StoredFile? current)
        => file is not null && NamesFile(file, current) ? current : file;

    /// <summary>
    /// A file is claimable when it is null (removal), when it already is the current file
    /// (a re-save resubmits it), or when it matches a pending upload.
    /// </summary>
    public static async Task<ErrorOr<Success>> EnsureClaimableAsync(
        AppDbContext appDbContext,
        StoredFile? file,
        StoredFile? current,
        Error notUploadedError,
        CancellationToken cancellationToken)
    {
        if (file is null || NamesFile(file, current))
        {
            return Result.Success;
        }

        var isPending = await appDbContext.PendingUploads
            .AnyAsync(pending => pending.Container == file.Container && pending.Path == file.Path, cancellationToken);

        return isPending ? Result.Success : notUploadedError;
    }

    /// <summary>
    /// True when both name the same blob, or both are null (no file before and after).
    /// </summary>
    public static bool IsSameFile(StoredFile? file, StoredFile? current)
        => file is null
            ? current is null
            : NamesFile(file, current);

    private static bool NamesFile(StoredFile file, StoredFile? current)
        => current is not null
            && current.Container == file.Container
            && current.Path == file.Path;
}
