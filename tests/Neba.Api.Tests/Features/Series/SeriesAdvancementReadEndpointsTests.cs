using System.Net;
using System.Net.Http.Json;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Series.GetSeriesAdvancement;
using Neba.Api.Contracts.Series.GetSeriesAdvancers;
using Neba.Api.Contracts.Series.ListStageResults;
using Neba.Api.Contracts.Series.ListStageSquads;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

using SecurityRoles = Neba.Api.Security.Domain.Roles;

namespace Neba.Api.Tests.Features.Series;

/// <summary>
/// The three advancement read endpoints through a real host: GET /series/{id}/advancement (Series.EditSeries),
/// GET /series/{id}/stages/{stageId}/results (Series.ManageAdvancement) and GET /series/{id}/stages/{stageId}/squads
/// (Series.EditSeries). A Manager holds EditSeries but not ManageAdvancement; a Webmaster and an Admin hold both.
/// </summary>
[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class SeriesAdvancementReadEndpointsTests(AppDbContextFixture fixture, SecurityDbContextFixture securityFixture)
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

    // ---- helpers ----

    private static string WebmasterToken()
        => TestAccessTokenFactory.Create(
            roles: [SecurityRoles.Webmaster],
            permissions: [Permissions.EditSeries, Permissions.ManageAdvancement]);

    private static string Token(string who)
        => who switch
        {
            "Manager" => SeriesApiHost.ManagerToken(),
            "Webmaster" => WebmasterToken(),
            "Admin" => SeriesApiHost.AdminToken(),
            _ => throw new ArgumentOutOfRangeException(nameof(who), who, "Unknown caller."),
        };

    private async Task<HttpResponseMessage> GetAsync(string url, string? token)
    {
        using var request = SeriesApiHost.Request(HttpMethod.Get, url, token);
        return await _host.Client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static string AdvancementUrl(string seriesId)
        => $"/series/{seriesId}/advancement";

    private static string ResultsUrl(string seriesId, string stageId)
        => $"/series/{seriesId}/stages/{stageId}/results";

    private static string SquadsUrl(string seriesId, string stageId)
        => $"/series/{seriesId}/stages/{stageId}/squads";

    // A Series with all three Stages computed (the default read scenario).
    private async Task<ReadScenario> SeedComputedSeriesAsync()
    {
        await using var db = fixture.CreateDbContext();
        return await AdvancementReadSeed.SeedAsync(db, TestContext.Current.CancellationToken);
    }

    // Two Pending Series in one season, so a Stage id of one is unknown to the other.
    private async Task<(Neba.Api.Features.Series.Domain.Series First, Neba.Api.Features.Series.Domain.Series Second)> SeedTwoSeriesAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateDbContext();
        var season = await SeriesSeed.AddSeasonAsync(db, 2025, ct);
        var first = await SeriesSeed.AddSeriesAsync(db, season, new DateOnly(2025, 2, 1), ct, name: "First");
        var second = await SeriesSeed.AddSeriesAsync(db, season, new DateOnly(2025, 6, 1), ct, name: "Second");

        return (first, second);
    }

    private static string Id(SeriesId id)
        => id.Value.ToString();

    private static string Id(SeriesStageId id)
        => id.Value.ToString();

    // ---- GET /series/{id}/advancement ----

    [Fact(DisplayName = "GET /series/{id}/advancement returns 401 without a token")]
    public async Task GetAdvancement_ShouldReturn401_WhenAnonymous()
    {
        // Arrange & Act
        using var response = await GetAsync(AdvancementUrl(Id(SeriesId.New())), null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "GET /series/{id}/advancement returns 403 for a Member")]
    public async Task GetAdvancement_ShouldReturn403_ForMember()
    {
        // Arrange & Act
        using var response = await GetAsync(AdvancementUrl(Id(SeriesId.New())), SeriesApiHost.MemberToken());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory(DisplayName = "GET /series/{id}/advancement returns 200 for a Manager, a Webmaster and an Admin")]
    [InlineData("Manager")]
    [InlineData("Webmaster")]
    [InlineData("Admin")]
    public async Task GetAdvancement_ShouldReturn200_ForCallersWithEditSeries(string who)
    {
        // Arrange
        var scenario = await SeedComputedSeriesAsync();

        // Act
        using var response = await GetAsync(AdvancementUrl(Id(scenario.Series.Id)), Token(who));
        var body = await response.Content.ReadFromJsonAsync<SeriesAdvancementResponse>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body!.Stages.Count.ShouldBe(3);
    }

    [Fact(DisplayName = "GET /series/{id}/advancement returns 404 for an unknown Series")]
    public async Task GetAdvancement_ShouldReturn404_WhenSeriesIsUnknown()
    {
        // Arrange & Act
        using var response = await GetAsync(AdvancementUrl(Id(SeriesId.New())), SeriesApiHost.AdminToken());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory(DisplayName = "GET /series/{id}/advancement returns 400 for an id that is not 26 characters")]
    [InlineData("short")]
    [InlineData("0000000000000000000000000")]
    [InlineData("000000000000000000000000000")]
    public async Task GetAdvancement_ShouldReturn400_WhenIdHasWrongLength(string id)
    {
        // Arrange & Act
        using var response = await GetAsync(AdvancementUrl(id), SeriesApiHost.AdminToken());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "GET /series/{id}/advancement returns the stored output with ids as ULID strings")]
    public async Task GetAdvancement_ShouldReturnStoredOutput()
    {
        // Arrange
        var scenario = await SeedComputedSeriesAsync();

        // Act
        using var response = await GetAsync(AdvancementUrl(Id(scenario.Series.Id)), SeriesApiHost.AdminToken());
        var body = await response.Content.ReadFromJsonAsync<SeriesAdvancementResponse>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull();
        body.Version.ShouldBe(scenario.Series.Version);
        body.BackfillAsOf.ShouldBe(scenario.Qualifier.EndDate);
        body.TargetField.ShouldBe(8);
        body.Stages.Select(s => s.StageId).ShouldBe([Id(scenario.Stage(1).Id), Id(scenario.Stage(2).Id), Id(scenario.Stage(3).Id)]);
        body.Stages.Select(s => s.Kind).ShouldBe(["Qualifying", "Survivor", "Placement"]);
        body.Stages.ShouldAllBe(s => s.Status == "Computed" && s.TournamentStatus == "Completed");
        body.Stages.Select(s => s.TournamentId).ShouldBe(
            [scenario.Qualifier.Id.Value.ToString(), scenario.Survivor.Id.Value.ToString(), scenario.Placement.Id.Value.ToString()]);
        body.Stages[1].ResolvedSquadId.ShouldBe(scenario.SquadId.Value.ToString());
        body.Stages[0].Entries.Count.ShouldBe(8);
        body.Stages[0].Entries.ShouldAllBe(e => e.SourceStageId == Id(scenario.Stage(1).Id));
        body.Stages[0].Entries[0].BowlerId.ShouldBe(scenario.Id(0).Value.ToString());
        body.Stages[0].Entries[0].BowlerName.ShouldBe("First00 Last00");
        body.Stages[1].SurvivorOutcomes.Count.ShouldBe(8);
        body.Positions.Count.ShouldBe(8);
        body.Positions[0].BowlerId.ShouldBe(scenario.Id(0).Value.ToString());
    }

    // ---- GET /series/{id}/advancers (public) ----

    [Fact(DisplayName = "GET /series/{id}/advancers returns 200 with the Official Stage for an anonymous caller")]
    public async Task GetAdvancers_ShouldReturn200WithOfficialStage_WhenAnonymous()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateDbContext();
        var scenario = await AdvancementReadSeed.SeedAsync(db, ct);
        scenario.Stage(1).MarkOfficial(AdvancementReadSeed.Now.AddHours(2));
        await db.SaveChangesAsync(ct);

        // Act
        using var response = await GetAsync($"/series/{Id(scenario.Series.Id)}/advancers", null);
        var body = await response.Content.ReadFromJsonAsync<SeriesAdvancersResponse>(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var stage = body!.Stages.ShouldHaveSingleItem();
        stage.StageId.ShouldBe(Id(scenario.Stage(1).Id));
        stage.Advancers.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "GET /series/{id}/advancers returns 200 with no Stages while none is Official")]
    public async Task GetAdvancers_ShouldReturnNoStages_WhenNothingIsOfficial()
    {
        // Arrange
        var scenario = await SeedComputedSeriesAsync();

        // Act
        using var response = await GetAsync($"/series/{Id(scenario.Series.Id)}/advancers", null);
        var body = await response.Content.ReadFromJsonAsync<SeriesAdvancersResponse>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body!.Stages.ShouldBeEmpty();
    }

    [Fact(DisplayName = "GET /series/{id}/advancers returns 404 for an unknown Series")]
    public async Task GetAdvancers_ShouldReturn404_WhenSeriesIsUnknown()
    {
        // Arrange

        // Act
        using var response = await GetAsync($"/series/{Id(SeriesId.New())}/advancers", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ---- GET /series/{id}/stages/{stageId}/results ----

    [Fact(DisplayName = "GET /series/{id}/stages/{stageId}/results returns 401 without a token")]
    public async Task ListResults_ShouldReturn401_WhenAnonymous()
    {
        // Arrange & Act
        using var response = await GetAsync(ResultsUrl(Id(SeriesId.New()), Id(SeriesStageId.New())), null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory(DisplayName = "GET /series/{id}/stages/{stageId}/results returns 403 for a Member and for a Manager, who lacks Series.ManageAdvancement")]
    [InlineData("Member")]
    [InlineData("Manager")]
    public async Task ListResults_ShouldReturn403_WhenCallerLacksManageAdvancement(string who)
    {
        // Arrange
        var scenario = await SeedComputedSeriesAsync();
        var token = who == "Member" ? SeriesApiHost.MemberToken() : SeriesApiHost.ManagerToken();

        // Act
        using var response = await GetAsync(ResultsUrl(Id(scenario.Series.Id), Id(scenario.Stage(1).Id)), token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory(DisplayName = "GET /series/{id}/stages/{stageId}/results returns 200 for a Webmaster and an Admin")]
    [InlineData("Webmaster")]
    [InlineData("Admin")]
    public async Task ListResults_ShouldReturn200_ForCallersWithManageAdvancement(string who)
    {
        // Arrange
        var scenario = await SeedComputedSeriesAsync();

        // Act
        using var response = await GetAsync(ResultsUrl(Id(scenario.Series.Id), Id(scenario.Stage(1).Id)), Token(who));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "GET /series/{id}/stages/{stageId}/results returns 404 for an unknown Series, an unknown Stage and a Stage of another Series")]
    public async Task ListResults_ShouldReturn404_WhenSeriesOrStageIsUnknown()
    {
        // Arrange
        var (first, second) = await SeedTwoSeriesAsync();
        var token = SeriesApiHost.AdminToken();
        var firstStage = Id(first.Stages.First().Id);

        // Act
        using var unknownSeries = await GetAsync(ResultsUrl(Id(SeriesId.New()), firstStage), token);
        using var unknownStage = await GetAsync(ResultsUrl(Id(first.Id), Id(SeriesStageId.New())), token);
        using var otherSeriesStage = await GetAsync(ResultsUrl(Id(second.Id), firstStage), token);

        // Assert
        unknownSeries.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        unknownStage.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        otherSeriesStage.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory(DisplayName = "GET /series/{id}/stages/{stageId}/results returns 400 when the Series id or the Stage id is not 26 characters")]
    [InlineData("short", "00000000000000000000000000")]
    [InlineData("0000000000000000000000000", "00000000000000000000000000")]
    [InlineData("00000000000000000000000000", "short")]
    [InlineData("00000000000000000000000000", "000000000000000000000000000")]
    public async Task ListResults_ShouldReturn400_WhenAnIdHasWrongLength(string seriesId, string stageId)
    {
        // Arrange & Act
        using var response = await GetAsync(ResultsUrl(seriesId, stageId), SeriesApiHost.AdminToken());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "GET /series/{id}/stages/{stageId}/results returns the Stage's result rows best place first")]
    public async Task ListResults_ShouldReturnResultRows()
    {
        // Arrange
        var scenario = await SeedComputedSeriesAsync();

        // Act
        using var response = await GetAsync(ResultsUrl(Id(scenario.Series.Id), Id(scenario.Stage(1).Id)), SeriesApiHost.AdminToken());
        var body = await response.Content.ReadFromJsonAsync<CollectionResponse<StageResultSummaryResponse>>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var items = body!.Items.ToList();
        items.Select(i => i.Place).ShouldBe([1, 2, 3, 4, 5, 6, 7, 8, 9]);
        items.Select(i => i.BowlerId).ShouldBe([.. Enumerable.Range(0, 9).Select(i => scenario.Id(i).Value.ToString())]);
        items[0].BowlerName.ShouldBe("First00 Last00");
    }

    [Fact(DisplayName = "GET /series/{id}/stages/{stageId}/results returns 200 with no items when the tournament has no results")]
    public async Task ListResults_ShouldReturnEmptyCollection_WhenTournamentHasNoResults()
    {
        // Arrange
        var (first, _) = await SeedTwoSeriesAsync();

        // Act
        using var response = await GetAsync(ResultsUrl(Id(first.Id), Id(first.Stages.First().Id)), SeriesApiHost.AdminToken());
        var body = await response.Content.ReadFromJsonAsync<CollectionResponse<StageResultSummaryResponse>>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body!.Items.ShouldBeEmpty();
    }

    // ---- GET /series/{id}/stages/{stageId}/squads ----

    [Fact(DisplayName = "GET /series/{id}/stages/{stageId}/squads returns 401 without a token")]
    public async Task ListSquads_ShouldReturn401_WhenAnonymous()
    {
        // Arrange & Act
        using var response = await GetAsync(SquadsUrl(Id(SeriesId.New()), Id(SeriesStageId.New())), null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "GET /series/{id}/stages/{stageId}/squads returns 403 for a Member")]
    public async Task ListSquads_ShouldReturn403_ForMember()
    {
        // Arrange
        var scenario = await SeedComputedSeriesAsync();

        // Act
        using var response = await GetAsync(SquadsUrl(Id(scenario.Series.Id), Id(scenario.Stage(2).Id)), SeriesApiHost.MemberToken());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory(DisplayName = "GET /series/{id}/stages/{stageId}/squads returns 200 for a Manager, a Webmaster and an Admin")]
    [InlineData("Manager")]
    [InlineData("Webmaster")]
    [InlineData("Admin")]
    public async Task ListSquads_ShouldReturn200_ForCallersWithEditSeries(string who)
    {
        // Arrange
        var scenario = await SeedComputedSeriesAsync();

        // Act
        using var response = await GetAsync(SquadsUrl(Id(scenario.Series.Id), Id(scenario.Stage(2).Id)), Token(who));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "GET /series/{id}/stages/{stageId}/squads returns 404 for an unknown Series, an unknown Stage and a Stage of another Series")]
    public async Task ListSquads_ShouldReturn404_WhenSeriesOrStageIsUnknown()
    {
        // Arrange
        var (first, second) = await SeedTwoSeriesAsync();
        var token = SeriesApiHost.AdminToken();
        var firstStage = Id(first.Stages.First().Id);

        // Act
        using var unknownSeries = await GetAsync(SquadsUrl(Id(SeriesId.New()), firstStage), token);
        using var unknownStage = await GetAsync(SquadsUrl(Id(first.Id), Id(SeriesStageId.New())), token);
        using var otherSeriesStage = await GetAsync(SquadsUrl(Id(second.Id), firstStage), token);

        // Assert
        unknownSeries.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        unknownStage.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        otherSeriesStage.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory(DisplayName = "GET /series/{id}/stages/{stageId}/squads returns 400 when the Series id or the Stage id is not 26 characters")]
    [InlineData("short", "00000000000000000000000000")]
    [InlineData("0000000000000000000000000", "00000000000000000000000000")]
    [InlineData("00000000000000000000000000", "short")]
    [InlineData("00000000000000000000000000", "000000000000000000000000000")]
    public async Task ListSquads_ShouldReturn400_WhenAnIdHasWrongLength(string seriesId, string stageId)
    {
        // Arrange & Act
        using var response = await GetAsync(SquadsUrl(seriesId, stageId), SeriesApiHost.AdminToken());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "GET /series/{id}/stages/{stageId}/squads returns the Stage's squads with Eastern time and the selected squad")]
    public async Task ListSquads_ShouldReturnSquads()
    {
        // Arrange
        var scenario = await SeedComputedSeriesAsync();

        // Act
        using var response = await GetAsync(SquadsUrl(Id(scenario.Series.Id), Id(scenario.Stage(2).Id)), SeriesApiHost.AdminToken());
        var body = await response.Content.ReadFromJsonAsync<CollectionResponse<StageSquadSummaryResponse>>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var squad = body!.Items.Single();
        squad.SquadId.ShouldBe(scenario.SquadId.Value.ToString());
        squad.Date.ShouldBe(new DateOnly(2025, 3, 3));
        squad.StartTime.ShouldBe(new TimeOnly(9, 0));
        squad.HasScores.ShouldBeFalse();
        squad.IsSelected.ShouldBeTrue();
    }

    [Fact(DisplayName = "GET /series/{id}/stages/{stageId}/squads returns 200 with no items when the tournament has no squads")]
    public async Task ListSquads_ShouldReturnEmptyCollection_WhenTournamentHasNoSquads()
    {
        // Arrange
        var (first, _) = await SeedTwoSeriesAsync();

        // Act
        using var response = await GetAsync(SquadsUrl(Id(first.Id), Id(first.Stages.First().Id)), SeriesApiHost.AdminToken());
        var body = await response.Content.ReadFromJsonAsync<CollectionResponse<StageSquadSummaryResponse>>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body!.Items.ShouldBeEmpty();
    }
}