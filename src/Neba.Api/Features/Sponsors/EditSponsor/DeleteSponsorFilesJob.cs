using Neba.Api.BackgroundJobs;

namespace Neba.Api.Features.Sponsors.EditSponsor;

/// <summary>
/// Represents a background job that deletes files associated with a sponsor.
/// </summary>
public sealed record DeleteSponsorFilesJob
    : IBackgroundJob
{
    /// <summary>
    /// Gets the collection of files to be deleted.
    /// </summary>
    /// <remarks>
    /// Copied into a <see cref="List{T}"/> because Hangfire's JSON serializer records the runtime type,
    /// and it cannot rebuild the compiler-generated type a collection expression produces.
    /// </remarks>
    public required IReadOnlyCollection<StoredFileReference> Files
    {
        get;
        init => field = [.. value];
    }

    /// <inheritdoc />
    public string JobName
        => $"{nameof(DeleteSponsorFilesJob)}: {Files.Count} file(s)";
}