using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Series.CreateSeries;
using Neba.Api.Contracts.Series.EditSeries;
using Neba.Api.Contracts.Series.GetSeries;
using Neba.Api.Contracts.Series.ListSeries;
using Neba.Api.Contracts.Series.ListSeriesFinales;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.BowlingCenters;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class SeriesEndpointsTests(AppDbContextFixture fixture, SecurityDbContextFixture securityFixture)
    : IClassFixture<AppDbContextFixture>, IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    private SeriesApiHost _host = null!;
    private HttpClient Client => _host.Client;

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

    // ---- seeding and request helpers ----

    private async Task<(Season Season, List<Tournament> Tournaments)> SeedTournamentsAsync(int count)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateDbContext();
        var season = await SeriesSeed.AddSeasonAsync(db, 2025, ct);
        var tournaments = new List<Tournament>();

        for (var i = 0; i < count; i++)
        {
            tournaments.Add(await SeriesSeed.AddTournamentAsync(db, season, $"T{i + 1}", new DateOnly(2025, 1, 10).AddDays(30 * i), null, ct));
        }

        return (season, tournaments);
    }

    private async Task<SeriesAggregate> SeedSeriesAsync(string name, StageStatus? status = null, SeriesFinaleDefinition? finale = null)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateDbContext();
        var season = await SeriesSeed.AddSeasonAsync(db, 2025, ct);
        return await SeriesSeed.AddSeriesAsync(db, season, new DateOnly(2025, 2, 1), ct, name: name, finale: finale, status: status);
    }

    private static SeriesStageInput Stage(Tournament tournament, int kind = 1, int? count = 8, string? id = null, bool squad = false)
        => SeriesStageInputFactory.Create(
            id: id,
            kind: kind,
            tournamentId: tournament.Id.Value.ToString(),
            advancingCount: count,
            squadDate: squad ? tournament.StartDate : null,
            squadStartTime: squad ? new TimeOnly(9, 0) : null);

    private static CreateSeriesRequest Create(Season season, IReadOnlyList<SeriesStageInput> stages, SeriesFinaleInput? finale = null, string name = "New Series")
        => new() { Series = SeriesInputFactory.Create(name: name, seasonId: season.Id.Value.ToString(), stages: stages, finale: finale) };

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? token, object? body = null)
    {
        using var request = SeriesApiHost.Request(method, url, token, body);
        return await Client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    // FastEndpoints keeps its error-response settings in process-wide static state, so another test host that ran
    // first can change the body from the ProblemDetails array ({ errors: [{ name }] }) to a dictionary
    // ({ errors: { "name": [...] } }), and the serializer lower-cases the first segment of a name ("series.Stages[2]").
    // Read both shapes and compare the first letter case-insensitively.
    private static async Task<List<string>> ErrorNamesAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        var errors = doc.RootElement.GetProperty("errors");

        IEnumerable<string> names = errors.ValueKind == JsonValueKind.Array
            ? errors.EnumerateArray().Select(e => e.GetProperty("name").GetString()!)
            : errors.EnumerateObject().Select(p => p.Name);

        return [.. names.Select(name => char.ToLowerInvariant(name[0]) + name[1..])];
    }

    private async Task<EditSeriesRequest> EditRequestForAsync(SeriesAggregate series, Func<List<SeriesStageInput>, List<SeriesStageInput>>? changeStages = null, SeriesFinaleInput? finale = null, string? name = null)
    {
        var detail = await Client.GetFromJsonAsync<SeriesDetailResponse>(new Uri($"/series/{series.Id.Value}", UriKind.Relative), TestContext.Current.CancellationToken);
        var stages = detail!.Stages.Select(s => SeriesStageInputFactory.Create(
            s.Id, Neba.Api.Features.Series.Domain.StageKind.FromName(s.Kind).Value, s.TournamentId, s.AdvancingCount, s.SquadDate, s.SquadStartTime)).ToList();

        return new EditSeriesRequest
        {
            Id = series.Id.Value.ToString(),
            Series = new EditSeriesInput
            {
                Name = name ?? detail.Name,
                Stages = changeStages is null ? stages : changeStages(stages),
                Finale = finale,
            },
        };
    }

    private static SeriesFinaleInput FinaleInput(DateOnly? date = null, int? count = null, string? center = null, TimeOnly? time = null)
        => SeriesFinaleInputFactory.Create(count, date ?? new DateOnly(2025, 12, 1), time, center);

    // ---- reads ----

    [Fact(DisplayName = "GET /series is anonymous and lists the Series")]
    public async Task ListSeries_ShouldReturnSeries_ToAnonymousCaller()
    {
        // Arrange
        await SeedSeriesAsync("VH");

        // Act
        using var response = await SendAsync(HttpMethod.Get, "/series", null);
        var body = await response.Content.ReadFromJsonAsync<CollectionResponse<SeriesSummaryResponse>>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body!.Items.Single().Name.ShouldBe("VH");
        body.Items.Single().QualifyingStageCount.ShouldBe(1);
    }

    [Fact(DisplayName = "GET /series/{id} is anonymous and returns the Series with its Stages")]
    public async Task GetSeries_ShouldReturnSeries_ToAnonymousCaller()
    {
        // Arrange
        var series = await SeedSeriesAsync("VH");

        // Act
        using var response = await SendAsync(HttpMethod.Get, $"/series/{series.Id.Value}", null);
        var body = await response.Content.ReadFromJsonAsync<SeriesDetailResponse>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body!.Name.ShouldBe("VH");
        body.Stages.Count.ShouldBe(3);
        body.Finale.ShouldBeNull();
    }

    [Fact(DisplayName = "GET /series/{id} returns 404 for an unknown id")]
    public async Task GetSeries_ShouldReturn404_WhenIdIsUnknown()
    {
        // Arrange & Act
        using var response = await SendAsync(HttpMethod.Get, $"/series/{SeriesId.New().Value}", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "GET /series/{id} returns 400 for an id of the wrong length")]
    public async Task GetSeries_ShouldReturn400_WhenIdHasWrongLength()
    {
        // Arrange & Act
        using var response = await SendAsync(HttpMethod.Get, "/series/short", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "GET /series/finales is anonymous, lists the Finales, and is not captured by GET /series/{id}")]
    public async Task ListSeriesFinales_ShouldReturnFinales_AndNotRouteToGetSeries()
    {
        // Arrange
        await SeedSeriesAsync("VH", finale: SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 12, 1)));

        // Act
        using var response = await SendAsync(HttpMethod.Get, "/series/finales", null);
        var body = await response.Content.ReadFromJsonAsync<CollectionResponse<SeriesFinaleSummaryResponse>>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body!.Items.Single().SeriesName.ShouldBe("VH");
        body.Items.Single().Finale.FinalistCount.ShouldBe(4);
    }

    // ---- authorization ----

    [Fact(DisplayName = "POST /series returns 401 without a token")]
    public async Task Create_ShouldReturn401_WhenAnonymous()
    {
        // Arrange
        var (season, tournaments) = await SeedTournamentsAsync(1);

        // Act
        using var response = await SendAsync(HttpMethod.Post, "/series", null, Create(season, [Stage(tournaments[0])]));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST, PUT and DELETE /series return 403 for a Member")]
    public async Task Writes_ShouldReturn403_ForMember()
    {
        // Arrange
        var (season, tournaments) = await SeedTournamentsAsync(1);
        var series = await SeedSeriesAsync("Other");
        var token = SeriesApiHost.MemberToken();

        // Act
        using var post = await SendAsync(HttpMethod.Post, "/series", token, Create(season, [Stage(tournaments[0])]));
        using var put = await SendAsync(HttpMethod.Put, $"/series/{series.Id.Value}", token, await EditRequestForAsync(series));

        // Assert
        post.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        put.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory(DisplayName = "POST /series returns 201 with a Location header for a Manager and an Admin")]
    [InlineData("Manager")]
    [InlineData("Admin")]
    public async Task Create_ShouldReturn201WithLocation_ForManagerAndAdmin(string who)
    {
        // Arrange
        var (season, tournaments) = await SeedTournamentsAsync(2);
        var token = who == "Manager" ? SeriesApiHost.ManagerToken() : SeriesApiHost.AdminToken();
        var request = Create(season, [Stage(tournaments[0]), Stage(tournaments[1], count: 4)]);

        // Act
        using var response = await SendAsync(HttpMethod.Post, "/series", token, request);
        var body = await response.Content.ReadFromJsonAsync<CreatedSeriesResponse>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location.ToString().ShouldContain(body!.SeriesId);

        using var read = await SendAsync(HttpMethod.Get, $"/series/{body.SeriesId}", null);
        (await read.Content.ReadFromJsonAsync<SeriesDetailResponse>(TestContext.Current.CancellationToken))!.Stages.Count.ShouldBe(2);
    }

    // ---- create errors ----

    [Fact(DisplayName = "POST /series returns 422 naming the broken row")]
    public async Task Create_ShouldReturn422NamingRow_WhenAStageIsBroken()
    {
        // Arrange
        var (season, tournaments) = await SeedTournamentsAsync(3);
        var stages = new List<SeriesStageInput> { Stage(tournaments[0]), Stage(tournaments[1]), Stage(tournaments[2], count: null) };

        // Act
        using var response = await SendAsync(HttpMethod.Post, "/series", SeriesApiHost.ManagerToken(), Create(season, stages));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ErrorNamesAsync(response)).ShouldBe(["series.Stages[2]"]);
    }

    [Fact(DisplayName = "POST /series returns 409 when a tournament is a Stage in another Series")]
    public async Task Create_ShouldReturn409_WhenTournamentIsInAnotherSeries()
    {
        // Arrange
        var other = await SeedSeriesAsync("Other");
        TournamentId held = other.Stages.First().TournamentId;
        Season season;
        Tournament tournament;
        await using (var db = fixture.CreateDbContext())
        {
            season = await db.Seasons.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
            tournament = await db.Tournaments.AsNoTracking().SingleAsync(t => t.Id == held, TestContext.Current.CancellationToken);
        }

        // Act
        using var response = await SendAsync(HttpMethod.Post, "/series", SeriesApiHost.ManagerToken(), Create(season, [Stage(tournament)]));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "POST /series returns 422 naming the Finale date when a Stage ends after it")]
    public async Task Create_ShouldReturn422NamingFinaleDate()
    {
        // Arrange
        var (season, tournaments) = await SeedTournamentsAsync(1);

        // Act
        using var response = await SendAsync(HttpMethod.Post, "/series", SeriesApiHost.ManagerToken(),
            Create(season, [Stage(tournaments[0])], FinaleInput(date: tournaments[0].EndDate.AddDays(-1))));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ErrorNamesAsync(response)).ShouldBe(["series.Finale.Date"]);
    }

    [Fact(DisplayName = "POST /series returns 422 naming the Finale center when it is unknown")]
    public async Task Create_ShouldReturn422NamingFinaleCenter_WhenUnknown()
    {
        // Arrange
        var (season, tournaments) = await SeedTournamentsAsync(1);

        // Act
        using var response = await SendAsync(HttpMethod.Post, "/series", SeriesApiHost.ManagerToken(),
            Create(season, [Stage(tournaments[0])], FinaleInput(center: "99999")));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ErrorNamesAsync(response)).ShouldBe(["series.Finale.BowlingCenterId"]);
    }

    [Fact(DisplayName = "POST /series returns 422 naming the Finale count when it is zero")]
    public async Task Create_ShouldReturn422NamingFinaleCount_WhenZero()
    {
        // Arrange
        var (season, tournaments) = await SeedTournamentsAsync(1);

        // Act
        using var response = await SendAsync(HttpMethod.Post, "/series", SeriesApiHost.ManagerToken(),
            Create(season, [Stage(tournaments[0])], FinaleInput(count: 0)));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ErrorNamesAsync(response)).ShouldBe(["series.Finale.FinalistCount"]);
    }

    [Fact(DisplayName = "POST /series returns 400 naming the field when the Finale has no date")]
    public async Task Create_ShouldReturn400_WhenFinaleHasNoDate()
    {
        // Arrange
        var (season, tournaments) = await SeedTournamentsAsync(1);
        var finale = new SeriesFinaleInput { FinalistCount = 4, StartTime = new TimeOnly(14, 0) };

        // Act
        using var response = await SendAsync(HttpMethod.Post, "/series", SeriesApiHost.ManagerToken(),
            Create(season, [Stage(tournaments[0])], finale));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ErrorNamesAsync(response)).ShouldContain(name => name.EndsWith("Date", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "POST /series returns 422 naming the Stage when a Placement Stage has a count")]
    public async Task Create_ShouldReturn422_WhenPlacementHasCount()
    {
        // Arrange
        var (season, tournaments) = await SeedTournamentsAsync(1);

        // Act
        using var response = await SendAsync(HttpMethod.Post, "/series", SeriesApiHost.ManagerToken(),
            Create(season, [Stage(tournaments[0], kind: 3, count: 4)]));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ErrorNamesAsync(response)).ShouldBe(["series.Stages[0]"]);
    }

    // ---- edit ----

    [Fact(DisplayName = "PUT /series/{id} returns 204 and the change shows on the next read")]
    public async Task Edit_ShouldReturn204_AndPersist()
    {
        // Arrange
        var series = await SeedSeriesAsync("VH");

        // Act
        using var response = await SendAsync(HttpMethod.Put, $"/series/{series.Id.Value}", SeriesApiHost.ManagerToken(),
            await EditRequestForAsync(series, name: "Renamed"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var read = await Client.GetFromJsonAsync<SeriesDetailResponse>(new Uri($"/series/{series.Id.Value}", UriKind.Relative), TestContext.Current.CancellationToken);
        read!.Name.ShouldBe("Renamed");
    }

    [Fact(DisplayName = "PUT /series/{id} returns 404 for an unknown Series")]
    public async Task Edit_ShouldReturn404_WhenSeriesIsUnknown()
    {
        // Arrange
        var series = await SeedSeriesAsync("VH");
        var request = await EditRequestForAsync(series);
        var unknown = request with { Id = SeriesId.New().Value.ToString() };

        // Act
        using var response = await SendAsync(HttpMethod.Put, $"/series/{unknown.Id}", SeriesApiHost.ManagerToken(), unknown);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "PUT /series/{id} renames a locked Series (204) but refuses changed Stages (409)")]
    public async Task Edit_ShouldAllowRename_AndRefuseStageChanges_WhenLocked()
    {
        // Arrange
        var series = await SeedSeriesAsync("VH", status: StageStatus.Computed);
        var token = SeriesApiHost.ManagerToken();

        // Act
        using var rename = await SendAsync(HttpMethod.Put, $"/series/{series.Id.Value}", token, await EditRequestForAsync(series, name: "Renamed"));
        using var changed = await SendAsync(HttpMethod.Put, $"/series/{series.Id.Value}", token,
            await EditRequestForAsync(series, stages => [.. stages.Skip(1)]));

        // Assert
        rename.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        changed.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "PUT /series/{id} reschedules a locked Series' Finale (204) but refuses removing it or changing its count (409)")]
    public async Task Edit_ShouldRescheduleLockedFinale_AndRefuseShapeChanges()
    {
        // Arrange
        var series = await SeedSeriesAsync("VH", StageStatus.Computed, SeriesFinaleDefinitionFactory.Create(date: new DateOnly(2025, 12, 1)));
        var token = SeriesApiHost.ManagerToken();

        // Act
        using var reschedule = await SendAsync(HttpMethod.Put, $"/series/{series.Id.Value}", token,
            await EditRequestForAsync(series, finale: FinaleInput(date: new DateOnly(2025, 12, 8), time: new TimeOnly(18, 0))));
        using var removed = await SendAsync(HttpMethod.Put, $"/series/{series.Id.Value}", token, await EditRequestForAsync(series, finale: null));
        using var recount = await SendAsync(HttpMethod.Put, $"/series/{series.Id.Value}", token,
            await EditRequestForAsync(series, finale: FinaleInput(count: 3)));

        // Assert
        reschedule.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        removed.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        recount.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    // ---- delete ----

    [Fact(DisplayName = "DELETE /series/{id} returns 401 without a token and 403 for a Member")]
    public async Task Delete_ShouldReturn401And403_WhenNotAuthorized()
    {
        // Arrange
        var series = await SeedSeriesAsync("VH");

        // Act
        using var anonymous = await SendAsync(HttpMethod.Delete, $"/series/{series.Id.Value}", null);
        using var member = await SendAsync(HttpMethod.Delete, $"/series/{series.Id.Value}", SeriesApiHost.MemberToken());

        // Assert
        anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        member.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "DELETE /series/{id} returns 204, and 204 again once the Series is gone")]
    public async Task Delete_ShouldReturn204_AndBeIdempotent()
    {
        // Arrange
        var series = await SeedSeriesAsync("VH");
        var token = SeriesApiHost.ManagerToken();

        // Act
        using var first = await SendAsync(HttpMethod.Delete, $"/series/{series.Id.Value}", token);
        using var second = await SendAsync(HttpMethod.Delete, $"/series/{series.Id.Value}", token);
        using var read = await SendAsync(HttpMethod.Get, $"/series/{series.Id.Value}", null);

        // Assert
        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        second.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        read.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "DELETE /series/{id} returns 409 for a locked Series")]
    public async Task Delete_ShouldReturn409_WhenLocked()
    {
        // Arrange
        var series = await SeedSeriesAsync("VH", StageStatus.Computed);

        // Act
        using var response = await SendAsync(HttpMethod.Delete, $"/series/{series.Id.Value}", SeriesApiHost.AdminToken());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "DELETE /series/{id} returns 400 for an id of the wrong length")]
    public async Task Delete_ShouldReturn400_WhenIdHasWrongLength()
    {
        // Arrange & Act
        using var response = await SendAsync(HttpMethod.Delete, "/series/short", SeriesApiHost.ManagerToken());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}