using System.Net;
using System.Net.Http.Json;

using Neba.Api.Contracts.Series.CreateSeries;
using Neba.Api.Contracts.Series.GetSeries;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.BowlingCenters;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series;

/// <summary>Posts the 2025 reference configuration from #258 and reads it back.</summary>
[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class Series2025ReferenceTests(AppDbContextFixture fixture, SecurityDbContextFixture securityFixture)
    : IClassFixture<AppDbContextFixture>, IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    private SeriesApiHost _host = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        await securityFixture.ResetAsync();
        _host = await SeriesApiHost.StartAsync(fixture, securityFixture);
    }

    public async ValueTask DisposeAsync()
    {
        await _host.DisposeAsync();
        await fixture.ResetAsync();
    }

    [Theory(DisplayName = "POST /series saves the 2025 reference configuration and GET /series/{id} returns all 11 Stages")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Post_ShouldSaveReferenceConfiguration_AndGetShouldReturnIt(bool withFinale)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var reference = Series2025ReferenceFactory.Create(withFinale);
        var center = BowlingCenterFactory.Create();

        await using (var db = fixture.CreateDbContext())
        {
            db.Seasons.Add(reference.Season);
            db.Tournaments.AddRange(reference.Tournaments);
            db.BowlingCenters.Add(center);
            await db.SaveChangesAsync(ct);
        }

        SeriesFinaleInput? finale = reference.Finale is null
            ? null
            : SeriesFinaleInputFactory.Create(
                finalistCount: reference.Finale.FinalistCount,
                date: reference.Finale.Date,
                startTime: reference.Finale.StartTime,
                bowlingCenterId: center.CertificationNumber.Value);

        var request = new CreateSeriesRequest
        {
            Series = SeriesInputFactory.Create(
                name: "2025 Vincent Hall Memorial Series",
                seasonId: reference.Season.Id.Value.ToString(),
                stages: [.. reference.Stages.Select(stage => SeriesStageInputFactory.Create(
                    kind: stage.Kind.Name,
                    tournamentId: stage.TournamentId.Value.ToString(),
                    advancingCount: stage.AdvancingCount,
                    squadDate: stage.SquadDate,
                    squadStartTime: stage.SquadStartTime))],
                finale: finale),
        };

        // Act
        using var post = SeriesApiHost.Request(HttpMethod.Post, "/series", SeriesApiHost.ManagerToken(), request);
        using var created = await _host.Client.SendAsync(post, ct);
        var body = await created.Content.ReadFromJsonAsync<CreatedSeriesResponse>(ct);
        var detail = await _host.Client.GetFromJsonAsync<SeriesDetailResponse>(new Uri($"/series/{body!.SeriesId}", UriKind.Relative), ct);

        // Assert
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        detail!.Stages.Count.ShouldBe(11);
        detail.Stages.Select(s => s.Order).ShouldBe(Enumerable.Range(1, 11));
        detail.Stages.Select(s => s.Kind).ShouldBe(
            [.. Enumerable.Repeat("Qualifying", 8), "Survivor", "Survivor", "Placement"]);
        detail.Stages.Select(s => s.AdvancingCount).ShouldBe([8, 2, 4, 8, 8, 8, 4, 8, 40, 20, null]);

        var stages = detail.Stages.ToList();
        stages[8].SquadDate.ShouldBe(new DateOnly(2025, 7, 12));
        stages[8].SquadStartTime.ShouldBe(new TimeOnly(9, 0));
        stages[9].SquadDate.ShouldBe(new DateOnly(2025, 8, 23));
        stages[9].SquadStartTime.ShouldBe(new TimeOnly(9, 0));
        stages.Where((_, i) => i is not (8 or 9)).ShouldAllBe(s => s.SquadDate == null);

        if (withFinale)
        {
            detail.Finale.ShouldNotBeNull();
            detail.Finale.FinalistCount.ShouldBe(4);
            detail.Finale.Date.ShouldBe(new DateOnly(2025, 9, 27));
            detail.Finale.StartTime.ShouldBe(new TimeOnly(14, 0));
            detail.Finale.BowlingCenterId.ShouldBe(center.CertificationNumber.Value);
        }
        else
        {
            detail.Finale.ShouldBeNull();
        }
    }
}
