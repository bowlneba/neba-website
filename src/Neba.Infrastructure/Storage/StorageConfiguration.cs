using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Neba.Application.Storage;

namespace Neba.Infrastructure.Storage;

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