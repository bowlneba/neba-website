namespace Neba.Api.Storage;

internal static class StorageConfiguration
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddStorage()
        {
            builder.AddAzureBlobServiceClient("blob");
            builder.Services.AddSingleton<IFileStorageService, AzureBlobStorageService>();

            return builder;
        }
    }
}