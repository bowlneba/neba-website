using System.Text.Json;
using System.Text.Json.Serialization;

using FastEndpoints;

using Neba.Api;
using Neba.Api.ErrorHandling;
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
    .AddSecurity();

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

app.MapGet("/debug/email", async (
    Neba.Api.Email.IEmailSender emailSender,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    var timestamp = DateTimeOffset.UtcNow;
    try
    {
        await emailSender.SendAsync(new Neba.Api.Email.EmailMessage
        {
            To = "some_user@test.com",
            Subject = "BowlNEBA Test Email",
            HtmlBody = Neba.Api.Email.EmailLayout.Wrap($"<p>This is a test email from the BowlNEBA debug tools.</p><p>Sent at: <strong>{timestamp:O}</strong></p>")
        }, ct);

        return Results.Ok($"Test email sent at {timestamp:O}.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Debug test email failed at {Timestamp}", timestamp);
        return Results.Problem("Failed to send test email. See API logs for details.", statusCode: 500);
    }
}).AllowAnonymous();
#pragma warning restore
#endif

app.MapDefaultEndpoints();

await app.RunAsync();