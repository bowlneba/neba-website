using ApexCharts;

using Neba.Website.Server;
using Neba.Website.Server.Account;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Maps;
using Neba.Website.Server.Services;
using Neba.Website.Server.Stats;
using Neba.Website.Server.Tournaments;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddMaps(builder.Configuration);
builder.Services.AddApexCharts();
builder.Services.AddScoped<IStatsApiService, StatsApiService>();

builder.Services.AddAccountServices();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddOutputCache();

builder.Services.AddSingleton<IStopwatchProvider, StopwatchProvider>();
builder.Services.AddScoped<Neba.Website.Server.Notifications.DebugToastService>();
builder.Services.AddScoped<ITournamentApiService, TournamentApiService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Neba.Website.Client._Imports).Assembly);

app.MapDefaultEndpoints();

await app.RunAsync();