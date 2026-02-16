using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Neba.Application.Storage;

namespace Neba.Infrastructure.Storage;

internal sealed class AzureBlobStorageService(BlobServiceClient blobServiceClient)
    : IFileStorageService
{
    private readonly BlobServiceClient _blobServiceClient = blobServiceClient;

    public async Task<bool> ExistsAsync(string container, string path, CancellationToken cancellationToken)
    {
        var blobClient = GetBlobClient(container, path);

        return await blobClient.ExistsAsync(cancellationToken);
    }

    public async Task<StoredFile?> GetFileAsync(string container, string path, CancellationToken cancellationToken)
    {
        var blobClient = GetBlobClient(container, path);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var result = await blobClient.DownloadContentAsync(cancellationToken);

        return new StoredFile
        {
            Content = result.Value.Content.ToString(),
            ContentType = result.Value.Details.ContentType,
            Metadata = result.Value.Details.Metadata
        };
    }

    public async Task UploadFileAsync(string container, string path, string content, string contentType, IDictionary<string, string> metadata, CancellationToken cancellationToken)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(container);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(path);

        await blobClient.UploadAsync(
            BinaryData.FromString(content),
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                },
                Metadata = metadata
            },
            cancellationToken
        );
    }

    private BlobClient GetBlobClient(string container, string path)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(container);

        return containerClient.GetBlobClient(path);
    }
}