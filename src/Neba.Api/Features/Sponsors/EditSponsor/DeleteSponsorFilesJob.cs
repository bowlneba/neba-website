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
    public required IReadOnlyCollection<StoredFileReference> Files { get; init; }

    /// <inheritdoc />
    public string JobName
        => $"{nameof(DeleteSponsorFilesJob)}: {Files.Count} file(s)";
}