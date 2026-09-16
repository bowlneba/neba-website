using System.Text.Json;
using System.Text.Json.Serialization;

using FastEndpoints;

using Neba.Api;
using Neba.Api.ErrorHandling;
using Neba.Api.FeatureManagement;
using Neba.Api.Legacy;
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
    .AddFeatureManagement()
    .AddLegacy();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.UseForwardedHeaders();

// Must run after authentication - the "public" rate-limiting policy partitions authenticated
// callers by user id (see RateLimitingConfiguration), which requires context.User to already be
// populated by the time the rate limiter's partition selector runs.
await app.UseSecurityInfrastructureAsync();

app.UseRateLimiter();

app.UseFastEndpoints(config =>
{
    config.Serializer.Options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    config.Serializer.Options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

    config.Binding.UsePropertyNamingPolicy = true;

    config.Errors.ConfigureErrorHandling();
});

app.MapLegacyGroup();
app.MapLegacyHealth();

app.UseOpenApiDocumentation();

app.UseInfrastructure();

#if DEBUG
#pragma warning disable CA1031, CA1848
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
            try
            {
                await tableServiceClient.DeleteTableAsync(tableName, ct);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                // Table didn't exist; nothing to delete.
            }

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