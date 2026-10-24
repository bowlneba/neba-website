using System.Diagnostics.CodeAnalysis;

using Neba.Api.BackgroundJobs;

namespace Neba.Api.Uploads;

/// <summary>
/// Represents a background job that deletes blobs no entity points at any more.
/// </summary>
public sealed record DeleteStoredFilesJob
    : IBackgroundJob
{
    /// <summary>
    /// Gets the collection of files to be deleted.
    /// </summary>
    /// <remarks>
    /// Copied into a <see cref="List{T}"/> because Hangfire's JSON serializer records the runtime type,
    /// and it cannot rebuild the compiler-generated type a collection expression produces.
    /// </remarks>
    [SuppressMessage("Style", "IDE0305:Simplify collection initialization", Justification = "A collection expression here compiles to a compiler-generated type Hangfire cannot deserialize.")]
    [SuppressMessage("Style", "IDE0306:Use collection expression for new", Justification = "A collection expression here compiles to a compiler-generated type Hangfire cannot deserialize.")]
    [SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "A collection expression here compiles to a compiler-generated type Hangfire cannot deserialize.")]
    public required IReadOnlyCollection<BlobReference> Files
    {
        get;
        init => field = new List<BlobReference>(value);
    }

    /// <inheritdoc />
    public string JobName
        => $"{nameof(DeleteStoredFilesJob)}: {Files.Count} file(s)";
}

/// <summary>
/// The container and path of a blob.
/// </summary>
public sealed record BlobReference
{
    /// <summary>
    /// Gets the name of the container where the blob is stored.
    /// </summary>
    public required string Container { get; init; }

    /// <summary>
    /// Gets the path of the blob within the container.
    /// </summary>
    public required string Path { get; init; }
}
