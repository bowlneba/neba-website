using System.Text.Json;
using System.Text.Json.Serialization;

using FastEndpoints;

using Neba.Api;
using Neba.Api.ErrorHandling;
using Neba.Api.OpenApi;
using Neba.Api.Versioning;
using Neba.Application;
using Neba.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddErrorHandling();

builder.Services
    .AddFastEndpoints(options => options.Assemblies = [typeof(Program).Assembly])
    .AddVersioning();

builder.Services.AddOpenApiDocumentation();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

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

app.MapDefaultEndpoints();

await app.RunAsync();