using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Microsoft.Extensions.Hosting;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Cache.ClearCache;

internal sealed class ClearCacheEndpoint(
    Messaging.ICommandHandler<ClearCacheCommand> commandHandler,
    IHostEnvironment environment)
    : EndpointWithoutRequest
{
    private readonly Messaging.ICommandHandler<ClearCacheCommand> _commandHandler = commandHandler;

    public override void Configure()
    {
        Delete("");
        Group<CacheEndpointGroup>();

        Options(options => options
            .WithVersionSet("Cache")
            .MapToApiVersion(new ApiVersion(1, 0)));

        // Freely available in Development for local debugging; requires the Cache.Clear permission
        // (held by Admins) everywhere else, since this clears production caches and deletes stored
        // documents. Read once at startup — matches the lifetime of the previous Program.cs check.
        if (environment.IsDevelopment())
        {
            AllowAnonymous();
        }
        else
        {
            Policies(PermissionCatalog.ClearCache.PolicyName);
        }

        Description(description => description
            .WithName("ClearCache")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await _commandHandler.HandleAsync(new ClearCacheCommand(), ct);

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}