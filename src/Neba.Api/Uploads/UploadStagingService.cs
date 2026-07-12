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
        var path = $"uploads/{pathPrefix}/{Ulid.NewUlid()}-{file.FileName}";

        await using var stream = file.OpenReadStream();
        await fileStorageService.UploadFileAsync(container, path, stream, file.ContentType, metadata ?? new Dictionary<string, string>(), cancellationToken);

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
            ContentType = file.ContentType,
            SizeInBytes = file.Length
        };
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
}