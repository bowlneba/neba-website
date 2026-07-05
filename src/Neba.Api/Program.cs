using System.Text.Json;
using System.Text.Json.Serialization;

using FastEndpoints;

using Neba.Api;
using Neba.Api.ErrorHandling;
using Neba.Api.FeatureManagement;
using Neba.Api.OpenApi;
using Neba.Api.Security;
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

builder
    .AddInfrastructure()
    .AddSecurity()
    .AddFeatureManagement();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.UseForwardedHeaders();
app.UseRateLimiter();

app.UseSecurityInfrastructure();

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
#pragma warning disable CA1031, CA1848
app.MapGet("/debug/cache", async (
    ZiggyCreatures.Caching.Fusion.IFusionCache fusionCache,
    Microsoft.Extensions.Caching.Hybrid.HybridCache hybridCache,
    Neba.Api.Storage.IFileStorageService storageService,
    Neba.Api.Documents.GoogleSettings googleSettings,
    CancellationToken ct) =>
{
    await fusionCache.RemoveByTagAsync("neba", token: ct);
    await hybridCache.RemoveByTagAsync("neba", ct);

    var deleteTasks = googleSettings.Documents
        .Select(doc => storageService.DeleteAsync("bowlneba-private", $"documents/{doc.Name}", ct));
    await Task.WhenAll(deleteTasks);

    return Results.Ok("Cache cleared.");
}).AllowAnonymous();

app.MapGet("/debug/clear-audits", async (
    Azure.Data.Tables.TableServiceClient tableServiceClient,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    string[] auditTableNames = ["EFAuditEvents", "SecurityAuditEvents", "JobAuditEvents"];

    try
    {
        foreach (var tableName in auditTableNames)
        {
            await tableServiceClient.DeleteTableAsync(tableName, ct);
            await tableServiceClient.CreateTableIfNotExistsAsync(tableName, ct);
        }

        return Results.Ok("Audit tables cleared.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Debug clear audit tables failed");
        return Results.Problem("Failed to clear audit tables. See API logs for details.", statusCode: 500);
    }
}).AllowAnonymous();
#pragma warning restore
#endif

app.MapDefaultEndpoints();

await app.RunAsync();