using System.Net;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Contracts.Series;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series;

/// <summary>
/// The advancement write endpoints through a real host whose per-Series lock is always held: every write times out on the
/// lock, so the caller gets 409 Series.Advancement.Busy and nothing is saved.
/// </summary>
[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class SeriesAdvancementBusyEndpointTests(AppDbContextFixture fixture, SecurityDbContextFixture securityFixture)
    : IClassFixture<AppDbContextFixture>, IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    private SeriesApiHost _host = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        await securityFixture.ResetAsync();
        _host = await SeriesApiHost.StartAsync(fixture, securityFixture, KeyedLockDoubles.RefusingAny());
    }

    public async ValueTask DisposeAsync()
    {
        await _host.DisposeAsync();
        await fixture.ResetAsync();
    }

    private static string StageUrl(string seriesId, string stageId, string tail)
        => $"/series/{seriesId}/stages/{stageId}/{tail}";

    [Theory(DisplayName = "The advancement write endpoints return 409 Series.Advancement.Busy when the Series' lock is held")]
    [InlineData("Recompute")]
    [InlineData("PickSquad")]
    [InlineData("CreateOverride")]
    [InlineData("RemoveOverride")]
    public async Task Endpoint_ShouldReturn409Busy_WhenLockIsHeld(string endpoint)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateDbContext();
        var season = await SeriesSeed.AddSeasonAsync(db, 2025, ct);
        var series = await SeriesSeed.AddSeriesAsync(db, season, new DateOnly(2025, 2, 1), ct, name: "Busy");
        var seriesId = series.Id.Value.ToString();
        var stageId = series.Stages.First().Id.Value.ToString();
        var other = Ulid.NewUlid().ToString();
        var token = SeriesApiHost.AdminToken();

        using var request = endpoint switch
        {
            "Recompute" => SeriesApiHost.Request(
                HttpMethod.Post, StageUrl(seriesId, stageId, "recomputations"), token,
                RecomputeSeriesAdvancementRequestFactory.Create(seriesId, stageId)),
            "PickSquad" => SeriesApiHost.Request(
                HttpMethod.Put, StageUrl(seriesId, stageId, "squad"), token,
                PickStageSquadRequestFactory.Create(seriesId, stageId, PickStageSquadInputFactory.Create(other))),
            "CreateOverride" => SeriesApiHost.Request(
                HttpMethod.Post, StageUrl(seriesId, stageId, "overrides"), token,
                CreateStageOverrideRequestFactory.Create(seriesId, stageId, CreateStageOverrideInputFactory.Create(OverrideKindValues.Add, other))),
            _ => SeriesApiHost.Request(
                HttpMethod.Delete, StageUrl(seriesId, stageId, $"overrides/{other}") + "?version=0", token),
        };

        // Act
        using var response = await _host.Client.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        // The error code only appears in one problem-details format, and other test classes change that format
        // process-wide, so assert on the description, which appears in both.
        body.ShouldContain(SeriesAdvancementErrors.Busy.Description);
        (await StoredVersionAsync(series.Id, ct)).ShouldBe(series.Version);
    }

    private async Task<int> StoredVersionAsync(SeriesId seriesId, CancellationToken ct)
    {
        await using var db = fixture.CreateDbContext();

        return await db.Series.AsNoTracking().Where(series => series.Id == seriesId).Select(series => series.Version).SingleAsync(ct);
    }
}