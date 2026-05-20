using System.Text.Json;
using System.Text.Json.Serialization;

using FastEndpoints;

using Neba.Api;
using Neba.Api.ErrorHandling;
using Neba.Api.OpenApi;
using Neba.Api.Versioning;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddErrorHandling();

builder.Services
    .AddFastEndpoints(options => options.Assemblies = [typeof(Program).Assembly])
    .AddVersioning();

builder.Services.AddHttpContextAccessor();

builder.Services.AddOpenApiDocumentation();

builder.Services
    .AddDomain()
    .AddApplication();

builder.AddInfrastructure();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.UseFastEndpoints(config =>
{
    config.Serializer.Options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    config.Serializer.Options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

    config.Binding.UsePropertyNamingPolicy = true;

    config.Errors.ConfigureErrorHandling();
});

app.UseOpenApiDocumentation();

app.UseInfrastructure();

#if DEBUG
app.MapGet("/debug/cache", async (
    ZiggyCreatures.Caching.Fusion.IFusionCache fusionCache,
    Microsoft.Extensions.Caching.Hybrid.HybridCache hybridCache,
    CancellationToken ct) =>
{
    await fusionCache.RemoveByTagAsync("neba", token: ct);
    await hybridCache.RemoveByTagAsync("neba", ct);
    return Results.Ok("Cache cleared.");
}).AllowAnonymous();
#endif

app.MapDefaultEndpoints();

await app.RunAsync();