using Neba.Website.Server;
using Neba.Website.Server.Components;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddOutputCache();

var nebaApiBaseUrl = builder.Configuration["NebaApi:BaseUrl"]
    ?? throw new InvalidOperationException("Configuration value 'NebaApi:BaseUrl' is required.");

builder.Services.AddHttpClient<NebaApiClient>(client => client.BaseAddress = new(nebaApiBaseUrl));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Neba.Website.Client._Imports).Assembly);

app.MapDefaultEndpoints();

await app.RunAsync();