using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Neba.Application.Storage;

namespace Neba.Infrastructure.Storage;

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S2325 // Methods that don't access instance data should be static
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