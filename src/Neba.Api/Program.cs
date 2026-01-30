using System.Text.Json;
using System.Text.Json.Serialization;

using FastEndpoints;
using FastEndpoints.Swagger;

using Neba.Api;
using Neba.Application;
using Neba.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        context.ProblemDetails.Extensions["requestPath"] = context.HttpContext.Request.Path.Value;
    };
});

builder.Services.AddFastEndpoints(options =>
{
    options.Assemblies = [typeof(Program).Assembly];
})
    .SwaggerDocument();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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

    config.Errors.UseProblemDetails(options =>
    {
        options.AllowDuplicateErrors = true;
        options.IndicateErrorCode = true;
        options.IndicateErrorSeverity = true;

        options.TypeValue = "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.1";

        options.TitleTransformer = problemDetails => problemDetails.Status switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            409 => "Conflict",
            500 => "Internal Server Error",
            _ => problemDetails.Title
        };
    });

    config.Errors.StatusCode = 400;
    config.Errors.ProducesMetadataType = typeof(ProblemDetails);
})
    .UseSwaggerGen();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseInfrastructure();

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/", () => "API service is running. Navigate to /weatherforecast to see sample data.");

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
#pragma warning disable CA5394 // Using Random.Shared for simplicity in sample code
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

app.MapDefaultEndpoints();

await app.RunAsync();