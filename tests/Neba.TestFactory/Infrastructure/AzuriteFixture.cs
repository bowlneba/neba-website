using Azure.Storage.Blobs;

using Testcontainers.Azurite;

namespace Neba.TestFactory.Infrastructure;

public sealed class AzuriteFixture
    : IAsyncLifetime
{
    private readonly AzuriteContainer _container = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest")
        .Build();

    public BlobServiceClient BlobServiceClient { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        BlobServiceClient = new BlobServiceClient(_container.GetConnectionString());
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}