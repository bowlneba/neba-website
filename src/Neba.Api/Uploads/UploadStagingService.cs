using Neba.Api.Database;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Storage;

namespace Neba.Api.Uploads;

internal sealed class UploadStagingService(
        IFileStorageService fileStorageService,
        AppDbContext appDbContext,
        TimeProvider timeProvider)
    : IUploadStagingService
{
    public async Task<StoredFile> StageUploadAsync(
        IFormFile file,
        string container,
        string pathPrefix,
        IDictionary<string, string>? metadata,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        return await StageAsync(stream, new UploadDescriptor(file.FileName, file.ContentType, file.Length), container, pathPrefix, metadata, cancellationToken);
    }

    public Task<StoredFile> StageUploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string container,
        string pathPrefix,
        IDictionary<string, string>? metadata,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        return !content.CanSeek
            ? throw new ArgumentException("The stream must be seekable so its size is known.", nameof(content))
            : StageAsync(content, new UploadDescriptor(fileName, contentType, content.Length - content.Position), container, pathPrefix, metadata, cancellationToken);
    }

    private async Task<StoredFile> StageAsync(
        Stream content,
        UploadDescriptor upload,
        string container,
        string pathPrefix,
        IDictionary<string, string>? metadata,
        CancellationToken cancellationToken)
    {
        var path = $"uploads/{pathPrefix}/{Ulid.NewUlid()}-{SanitizeFileName(upload.FileName)}";

        await fileStorageService.UploadFileAsync(container, path, content, upload.ContentType, metadata ?? new Dictionary<string, string>(), cancellationToken);

        await appDbContext.PendingUploads.AddAsync(new PendingUpload
        {
            Container = container,
            Path = path,
            UploadedAtUtc = timeProvider.GetUtcNow()
        }, cancellationToken);
        await appDbContext.SaveChangesAsync(cancellationToken);

        return new StoredFile
        {
            Container = container,
            Path = path,
            ContentType = upload.ContentType,
            SizeInBytes = upload.SizeInBytes
        };
    }

    private readonly record struct UploadDescriptor(string FileName, string ContentType, long SizeInBytes);

    /// <summary>
    /// Strips any directory component from a client-supplied file name before it's interpolated into
    /// a blob path — an unsanitized name containing '/' or '\' could otherwise let a caller influence
    /// the storage path's structure.
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var normalized = fileName.Replace('\\', '/');
        var lastSeparator = normalized.LastIndexOf('/');

        return lastSeparator >= 0 ? normalized[(lastSeparator + 1)..] : normalized;
    }
}

internal interface IUploadStagingService
{
    Task<StoredFile> StageUploadAsync(
        IFormFile file,
        string container,
        string pathPrefix,
        IDictionary<string, string>? metadata,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stages bytes the caller produced, such as a resized image, instead of the uploaded form file.
    /// The stream must be seekable; staging reads from its current position to its end.
    /// </summary>
    Task<StoredFile> StageUploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string container,
        string pathPrefix,
        IDictionary<string, string>? metadata,
        CancellationToken cancellationToken);
}