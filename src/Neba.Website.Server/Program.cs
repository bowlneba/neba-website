using Neba.Website.Server;
using Neba.Website.Server.BackgroundJobs;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddBackgroundJobs(builder.Configuration);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddOutputCache();

builder.Services.AddSingleton<IStopwatchProvider, StopwatchProvider>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    var path = response.StatusCode switch
    {
        401 => "/unauthorized",
        403 => "/forbidden",
        404 => "/not-found",
        _ => $"/error?code={response.StatusCode}"
    };

    response.Redirect(path);
    await Task.CompletedTask;
});

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Neba.Website.Client._Imports).Assembly);

app.MapDefaultEndpoints();

app.UseBackgroundJobsDashboard();

await app.RunAsync();