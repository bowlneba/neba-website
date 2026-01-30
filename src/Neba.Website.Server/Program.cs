using System.Text.Json;

using Microsoft.Extensions.Options;

using Neba.Api.Contracts.Weather;
using Neba.Website.Server.BackgroundJobs;
using Neba.Website.Server.Components;
using Neba.Website.Server.Services;
using Neba.Website.Server.Weather;

using Refit;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Services.AddBackgroundJobs(builder.Configuration);

// look to move this to api configuration

builder.Services.AddOptions<NebaApiConfiguration>()
    .Bind(builder.Configuration.GetSection("NebaApi"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<NebaApiConfiguration>>().Value);

var jsonSerializerOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
};

builder.Services
    .AddRefitClient<IWeatherApi>(new RefitSettings
    {
        ContentSerializer = new SystemTextJsonContentSerializer(jsonSerializerOptions),
    })
    .ConfigureHttpClient((sp, client) =>
    {
        var apiConfig = sp.GetRequiredService<NebaApiConfiguration>();
        client.BaseAddress = new Uri(apiConfig.BaseUrl);
    });

builder.Services.AddScoped<WeatherService>(); // the add refit client can be refactored to extension and tied w/ the service

// end api configuration refactoring opportunity

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddOutputCache();

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

app.UseBackgroundJobsDashboard();

await app.RunAsync();