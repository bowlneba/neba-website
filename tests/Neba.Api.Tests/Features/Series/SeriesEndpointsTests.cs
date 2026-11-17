using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;

using FastEndpoints;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Series.GetSeries;
using Neba.Api.Contracts.Series.ListSeries;
using Neba.Api.Contracts.Series.ListSeriesFinales;
using Neba.Api.Database;
using Neba.Api.Features.Series;
using Neba.Api.Features.Series.GetSeries;
using Neba.Api.Features.Series.ListSeries;
using Neba.Api.Features.Series.ListSeriesFinales;
using Neba.Api.Messaging;
using Neba.Api.Storage;
using Neba.Api.Versioning;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series;

[IntegrationTest]
[Component("Series")]
[SuppressMessage("Design", "CA2213:Disposable fields should be disposed", Justification = "_app is intentionally never disposed - see DisposeAsync.")]
[Collection<AppDbContextFixture>]
public sealed class SeriesEndpointsTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseSetting(WebHostDefaults.ServerUrlsKey, "http://127.0.0.1:0");

        builder.Services.AddScoped(_ => fixture.CreateDbContext());
        builder.Services.AddSingleton(TimeProvider.System);

        var storage = new Mock<IFileStorageService>(MockBehavior.Strict);
        storage.Setup(s => s.GetBlobUri(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string container, string path) => new Uri($"https://storage.example.com/{container}/{path}"));
        builder.Services.AddSingleton(storage.Object);

        builder.Services.AddScoped<IQueryHandler<ListSeriesQuery, IReadOnlyCollection<SeriesSummaryDto>>, ListSeriesQueryHandler>();
        builder.Services.AddScoped<IQueryHandler<GetSeriesQuery, ErrorOr.ErrorOr<SeriesDetailDto>>, GetSeriesQueryHandler>();
        builder.Services.AddScoped<IQueryHandler<ListSeriesFinalesQuery, IReadOnlyCollection<SeriesFinaleSummaryDto>>, ListSeriesFinalesQueryHandler>();

        builder.Services
            .AddFastEndpoints(options =>
            {
                options.Assemblies = [typeof(SeriesEndpointGroup).Assembly];
                options.Filter = type => type.Namespace?.StartsWith("Neba.Api.Features.Series", StringComparison.Ordinal) == true;
            })
            .AddVersioning();

        _app = builder.Build();
        _app.UseFastEndpoints(c => c.Validation.UsePropertyNamingPolicy = false);
        await _app.StartAsync();

        var address = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        _client = new HttpClient { BaseAddress = new Uri(address) };
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.StopAsync();

        // Deliberately not disposing _app: UseFastEndpoints() points FastEndpoints' process-wide static
        // service resolver at this host's provider (see UploadTournamentLogoEndpointAuthorizationTests).
        await fixture.ResetAsync();
    }

    private async Task<(Neba.Api.Features.Seasons.Domain.Season Season, Neba.Api.Features.Series.Domain.Series Series)> SeedAsync(
        string name, DateOnly? finaleDate = null)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateDbContext();
        var season = await SeriesSeed.AddSeasonAsync(db, 2025, ct);
        var series = await SeriesSeed.AddSeriesAsync(db, season, new DateOnly(2025, 2, 1), ct, name: name,
            finale: finaleDate is null ? null : SeriesFinaleDefinitionFactory.Create(date: finaleDate));

        return (season, series);
    }

    [Fact(DisplayName = "GET /series is anonymous and lists the Series")]
    public async Task ListSeries_ShouldReturnSeries_ToAnonymousCaller()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedAsync("VH");

        // Act
        using var response = await _client.GetAsync(new Uri("/series", UriKind.Relative), ct);
        var body = await response.Content.ReadFromJsonAsync<CollectionResponse<SeriesSummaryResponse>>(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body!.Items.Single().Name.ShouldBe("VH");
        body.Items.Single().QualifyingStageCount.ShouldBe(1);
    }

    [Fact(DisplayName = "GET /series/{id} is anonymous and returns the Series with its Stages")]
    public async Task GetSeries_ShouldReturnSeries_ToAnonymousCaller()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (_, series) = await SeedAsync("VH");

        // Act
        using var response = await _client.GetAsync(new Uri($"/series/{series.Id.Value}", UriKind.Relative), ct);
        var body = await response.Content.ReadFromJsonAsync<SeriesDetailResponse>(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body!.Name.ShouldBe("VH");
        body.Stages.Count.ShouldBe(3);
        body.Finale.ShouldBeNull();
    }

    [Fact(DisplayName = "GET /series/{id} returns 404 for an unknown id")]
    public async Task GetSeries_ShouldReturn404_WhenIdIsUnknown()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await _client.GetAsync(new Uri($"/series/{Neba.Api.Features.Series.Domain.SeriesId.New().Value}", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "GET /series/{id} returns 400 for an id of the wrong length")]
    public async Task GetSeries_ShouldReturn400_WhenIdHasWrongLength()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await _client.GetAsync(new Uri("/series/short", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "GET /series/finales is anonymous, lists the Finales, and is not captured by GET /series/{id}")]
    public async Task ListSeriesFinales_ShouldReturnFinales_AndNotRouteToGetSeries()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedAsync("VH", new DateOnly(2025, 12, 1));

        // Act
        using var response = await _client.GetAsync(new Uri("/series/finales", UriKind.Relative), ct);
        var body = await response.Content.ReadFromJsonAsync<CollectionResponse<SeriesFinaleSummaryResponse>>(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body!.Items.Single().SeriesName.ShouldBe("VH");
        body.Items.Single().Finale.FinalistCount.ShouldBe(4);
    }

    [Fact(DisplayName = "GET /series/finales returns an empty list when no Series has a Finale")]
    public async Task ListSeriesFinales_ShouldReturnEmpty_WhenNoFinales()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedAsync("VH");

        // Act
        using var response = await _client.GetAsync(new Uri("/series/finales", UriKind.Relative), ct);
        var body = await response.Content.ReadFromJsonAsync<CollectionResponse<SeriesFinaleSummaryResponse>>(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body!.Items.ShouldBeEmpty();
    }
}
