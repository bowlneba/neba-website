using ApexCharts;

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Rewrite;

using Neba.Website.Server;
using Neba.Website.Server.Account;
using Neba.Website.Server.Account.Session;
using Neba.Website.Server.Clock;
using Neba.Website.Server.FeatureManagement;
using Neba.Website.Server.Help;
using Neba.Website.Server.Maps;
using Neba.Website.Server.ReferenceData;
using Neba.Website.Server.Services;
using Neba.Website.Server.Stats;
using Neba.Website.Server.Time;
using Neba.Website.Server.Tournaments;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder
    .AddServiceDefaults()
    .AddInfrastructure()
    .AddFeatureManagement();

builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddMaps(builder.Configuration);
builder.Services.AddApexCharts();
builder.Services.AddScoped<IStatsApiService, StatsApiService>();

builder.Services.AddAccountServices(builder.Configuration, builder.Environment);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddOutputCache();

// Trust X-Forwarded-Proto from the Azure ingress so redirects keep the https scheme.
// Azure ingress addresses vary, so clear the default loopback-only trust lists.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddSingleton<IStopwatchProvider, StopwatchProvider>();
builder.Services.AddScoped<Neba.Website.Server.Notifications.ToastService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITournamentApiService, TournamentApiService>();
builder.Services.AddScoped<IReferenceDataService, ReferenceDataService>();
builder.Services.AddScoped<IClientTimeZoneService, ClientTimeZoneService>();
builder.Services.AddSingleton<HelpDocumentService>();

var app = builder.Build();

// Must run before static files so www.* redirects to the apex for every path.
app.UseForwardedHeaders();
app.UseRewriter(new RewriteOptions().AddRedirectToNonWwwPermanent());

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapTestAuthEndpoints();
}

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();
app.MapHelpImageEndpoints();
app.MapSessionTokenEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Neba.Website.Client._Imports).Assembly);

app.MapDefaultEndpoints();

await app.RunAsync();