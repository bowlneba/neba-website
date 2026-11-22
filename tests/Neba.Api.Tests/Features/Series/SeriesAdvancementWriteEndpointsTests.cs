using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using ErrorOr;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using Neba.Api.Contracts.Memberships;
using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Series;
using Neba.Api.Contracts.Series.CreateStageOverride;
using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;
using Neba.TestFactory.Tournaments;

using SecurityRoles = Neba.Api.Security.Domain.Roles;
using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Tests.Features.Series;

/// <summary>
/// The four advancement write endpoints through a real host with the real handlers and the real facts reader:
/// POST /series/{id}/stages/{stageId}/recomputations and PUT .../squad (Series.EditSeries), POST .../overrides and
/// DELETE .../overrides/{overrideId}?version=n (Series.ManageAdvancement). A Manager holds EditSeries but not
/// ManageAdvancement; a Webmaster and an Admin hold both. The lock is a grant-anything double; a busy lock is covered
/// by <see cref="SeriesAdvancementBusyEndpointTests"/>.
/// </summary>
[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class SeriesAdvancementWriteEndpointsTests(AppDbContextFixture fixture, SecurityDbContextFixture securityFixture)
    : IClassFixture<AppDbContextFixture>, IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    private static readonly TimeSpan Grace = TimeSpan.FromHours(24);

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

    // ---- scenario ----

    /// <summary>
    /// A Series over the 2025 layout: a Qualifying Stage whose tournament is Completed with ten results (bowlers 0 to 7 hold the
    /// eight slots, bowler 8 is 9th and bowler 9 is 10th, all members), a Survivor Stage with two squads and a Placement Stage.
    /// The Qualifying Stage is Computed. The Survivor tournament is still Scheduled, so that Stage stays Pending with the
    /// matching squad designated.
    /// </summary>
    private sealed record WriteScenario(
        SeriesAggregate Series,
        IReadOnlyList<Bowler> Bowlers,
        SquadId MatchingSquad,
        SquadId OtherSquad,
        StageOverrideId? OverrideId)
    {
        public string Id => Series.Id.Value.ToString();

        public SeriesStage Stage(int order)
            => Series.Stages.Single(stage => stage.Order == order);

        public string StageId(int order)
            => Stage(order).Id.Value.ToString();

        public BowlerId Holder => Bowlers[0].Id;

        public BowlerId Ninth => Bowlers[8].Id;

        public BowlerId Tenth => Bowlers[9].Id;
    }

    private async Task<WriteScenario> SeedAsync(bool withOverride = false)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateDbContext();

        var season = await SeriesSeed.AddSeasonAsync(db, 2025, ct);
        var bowlers = await AdvancementReaderSeed.AddBowlersAsync(db, 12, ct);

        var legacyId = 1;
        foreach (var bowler in bowlers)
        {
            bowler.AddLegacyMembership(legacyId++, MembershipType.New, 25m, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)).IsError.ShouldBeFalse();
        }

        await db.SaveChangesAsync(ct);

        var qualifier = await AdvancementReaderSeed.AddTournamentAsync(
            db, season, "The Qualifier", new DateOnly(2025, 2, 1), null, ct,
            status: TournamentStatus.Completed,
            results: [.. bowlers.Take(10).Select((bowler, index) => AdvancementReaderSeed.Result(bowler, index + 1))]);
        var survivor = await AdvancementReaderSeed.AddTournamentAsync(
            db, season, "The Survivor", new DateOnly(2025, 3, 3), null, ct,
            squads:
            [
                SquadFactory.Create(new DateTimeOffset(2025, 3, 3, 14, 0, 0, TimeSpan.Zero)),
                SquadFactory.Create(new DateTimeOffset(2025, 3, 3, 23, 0, 0, TimeSpan.Zero)),
            ]);
        var placement = await AdvancementReaderSeed.AddTournamentAsync(db, season, "The Placement", new DateOnly(2025, 4, 5), null, ct);
        var series = await AdvancementReaderSeed.AddSeriesAsync(db, season, qualifier, survivor, placement, ct);

        var matching = survivor.Squads.Single(squad => squad.BowlingDateTimeUtc.Hour == 14).Id;
        var other = survivor.Squads.Single(squad => squad.BowlingDateTimeUtc.Hour == 23).Id;

        (await RunAsync(series.Id, (aggregate, facts, now) => aggregate.Advance(qualifier.Id, facts, now))).IsError.ShouldBeFalse();

        StageOverrideId? overrideId = null;
        if (withOverride)
        {
            var version = await VersionAsync(series.Id);
            var stageId = series.Stages.Single(stage => stage.Order == 1).Id;
            var tenth = bowlers[9].Id;

            (await RunAsync(
                series.Id,
                (aggregate, facts, now) => aggregate.AddOverride(stageId, tenth, "Seeded.", "seed", version, facts, now))).IsError.ShouldBeFalse();

            overrideId = (await StoredAsync(series.Id)).Stages.Single(stage => stage.Order == 1).Overrides.Single().Id;
        }

        return new WriteScenario(series, bowlers, matching, other, overrideId);
    }

    private async Task<ErrorOr<AdvanceResult>> RunAsync(SeriesId seriesId, AdvancementOperation operation)
    {
        await using var db = fixture.CreateDbContext();

        return await CreateAdvancement(db).RunAsync(seriesId, operation, AdvancementRunHarness.Wait, TestContext.Current.CancellationToken);
    }

    private async Task<ErrorOr<AdvanceResult>> RunWithoutFactsAsync(SeriesId seriesId, AdvancementOperation operation)
    {
        await using var db = fixture.CreateDbContext();

        return await CreateAdvancement(db).RunWithoutFactsAsync(seriesId, operation, AdvancementRunHarness.Wait, TestContext.Current.CancellationToken);
    }

    private static SeriesAdvancement CreateAdvancement(AppDbContext db)
        => new(
            db,
            new SeriesAdvancementFactsReader(db),
            KeyedLockDoubles.GrantingAny().Provider.Object,
            SeriesCache.Evicting().Object,
            new FakeTimeProvider(AdvancementRunHarness.Now),
            NullLogger<SeriesAdvancement>.Instance);

    // Makes the Qualifying Stage Official, as the make-official job does once the grace period has passed.
    private async Task MakeQualifyingStageOfficialAsync(WriteScenario scenario)
    {
        var result = await RunWithoutFactsAsync(
            scenario.Series.Id, (series, _, now) => series.MakeDueOfficial(now.AddDays(3), Grace));

        result.IsError.ShouldBeFalse();
        result.Value.OfficialStageIds.ShouldBe([scenario.Stage(1).Id]);
    }

    private async Task<int> VersionAsync(SeriesId seriesId)
    {
        await using var db = fixture.CreateDbContext();

        return await db.Series.AsNoTracking().Where(series => series.Id == seriesId).Select(series => series.Version)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    private async Task<SeriesAggregate> StoredAsync(SeriesId seriesId)
    {
        await using var db = fixture.CreateDbContext();

        return await db.Series.AsNoTracking()
            .Include(series => series.Stages).ThenInclude(stage => stage.Overrides)
            .Include(series => series.FieldEntries)
            .AsSplitQuery()
            .SingleAsync(series => series.Id == seriesId, TestContext.Current.CancellationToken);
    }

    // ---- requests ----

    private static string WebmasterToken()
        => TestAccessTokenFactory.Create(
            roles: [SecurityRoles.Webmaster],
            permissions: [Permissions.EditSeries, Permissions.ManageAdvancement]);

    private static string Token(string who)
        => who switch
        {
            "Member" => SeriesApiHost.MemberToken(),
            "Manager" => SeriesApiHost.ManagerToken(),
            "Webmaster" => WebmasterToken(),
            "Admin" => SeriesApiHost.AdminToken(),
            _ => throw new ArgumentOutOfRangeException(nameof(who), who, "Unknown caller."),
        };

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? token, object? body = null)
    {
        using var request = SeriesApiHost.Request(method, url, token, body);

        return await _host.Client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static string StageUrl(string seriesId, string stageId, string tail)
        => $"/series/{seriesId}/stages/{stageId}/{tail}";

    private Task<HttpResponseMessage> RecomputeAsync(
        string seriesId, string stageId, string? token, int version, bool confirm = false)
        => SendAsync(
            HttpMethod.Post,
            StageUrl(seriesId, stageId, "recomputations"),
            token,
            RecomputeSeriesAdvancementRequestFactory.Create(
                seriesId, stageId, RecomputeSeriesAdvancementInputFactory.Create(version, confirm)));

    private Task<HttpResponseMessage> PickSquadAsync(
        string seriesId, string stageId, string? token, string squadId, int version)
        => SendAsync(
            HttpMethod.Put,
            StageUrl(seriesId, stageId, "squad"),
            token,
            PickStageSquadRequestFactory.Create(seriesId, stageId, PickStageSquadInputFactory.Create(squadId, version)));

    private Task<HttpResponseMessage> CreateOverrideAsync(
        string seriesId, string stageId, string? token, int kind, string bowlerId, int version, string? reason = null)
        => SendAsync(
            HttpMethod.Post,
            StageUrl(seriesId, stageId, "overrides"),
            token,
            CreateStageOverrideRequestFactory.Create(
                seriesId, stageId, CreateStageOverrideInputFactory.Create(kind, bowlerId, reason, version)));

    private Task<HttpResponseMessage> RemoveOverrideAsync(
        string seriesId, string stageId, string? token, string overrideId, int? version)
        => SendAsync(
            HttpMethod.Delete,
            StageUrl(seriesId, stageId, $"overrides/{overrideId}") + (version is null ? string.Empty : $"?version={version}"),
            token);

    // One valid call per endpoint, for the authentication and authorization matrix.
    private async Task<HttpResponseMessage> CallAsync(string endpoint, WriteScenario scenario, string? token)
    {
        var version = await VersionAsync(scenario.Series.Id);

        return endpoint switch
        {
            "Recompute" => await RecomputeAsync(scenario.Id, scenario.StageId(1), token, version),
            "PickSquad" => await PickSquadAsync(scenario.Id, scenario.StageId(2), token, scenario.OtherSquad.Value.ToString(), version),
            "CreateOverride" => await CreateOverrideAsync(scenario.Id, scenario.StageId(1), token, OverrideKindValues.Add, scenario.Ninth.Value.ToString(), version),
            "RemoveOverride" => await RemoveOverrideAsync(scenario.Id, scenario.StageId(1), token, scenario.OverrideId!.Value.Value.ToString(), version),
            _ => throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, "Unknown endpoint."),
        };
    }

    // FastEndpoints keeps its error-response settings in process-wide static state, so another test host that ran
    // first can change the body from the ProblemDetails array ({ errors: [{ name }] }) to a dictionary
    // ({ errors: { "name": [...] } }), and the serializer lower-cases the first segment of a name ("series.Stages[0]").
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

    // ---- authentication and authorization matrix ----

    [Theory(DisplayName = "The advancement write endpoints return 401 without a token")]
    [InlineData("Recompute")]
    [InlineData("PickSquad")]
    [InlineData("CreateOverride")]
    [InlineData("RemoveOverride")]
    public async Task Endpoint_ShouldReturn401_WhenAnonymous(string endpoint)
    {
        // Arrange
        var scenario = await SeedAsync(withOverride: true);

        // Act
        using var response = await CallAsync(endpoint, scenario, null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory(DisplayName = "The advancement write endpoints return 403 for a Member and change nothing")]
    [InlineData("Recompute")]
    [InlineData("PickSquad")]
    [InlineData("CreateOverride")]
    [InlineData("RemoveOverride")]
    public async Task Endpoint_ShouldReturn403_ForMember(string endpoint)
    {
        // Arrange
        var scenario = await SeedAsync(withOverride: true);
        var version = await VersionAsync(scenario.Series.Id);

        // Act
        using var response = await CallAsync(endpoint, scenario, Token("Member"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await VersionAsync(scenario.Series.Id)).ShouldBe(version);
    }

    [Theory(DisplayName = "A Manager gets 204 on Recompute and Pick squad and 403 on Create and Remove override")]
    [InlineData("Recompute", 204)]
    [InlineData("PickSquad", 204)]
    [InlineData("CreateOverride", 403)]
    [InlineData("RemoveOverride", 403)]
    public async Task Endpoint_ShouldFollowManagerPermissions(string endpoint, int expected)
    {
        // Arrange
        var scenario = await SeedAsync(withOverride: true);

        // Act
        using var response = await CallAsync(endpoint, scenario, Token("Manager"));

        // Assert
        ((int)response.StatusCode).ShouldBe(expected);
    }

    [Theory(DisplayName = "A Webmaster and an Admin succeed on all four advancement write endpoints")]
    [InlineData("Webmaster", "Recompute", 204)]
    [InlineData("Webmaster", "PickSquad", 204)]
    [InlineData("Webmaster", "CreateOverride", 201)]
    [InlineData("Webmaster", "RemoveOverride", 204)]
    [InlineData("Admin", "Recompute", 204)]
    [InlineData("Admin", "PickSquad", 204)]
    [InlineData("Admin", "CreateOverride", 201)]
    [InlineData("Admin", "RemoveOverride", 204)]
    public async Task Endpoint_ShouldSucceed_ForWebmasterAndAdmin(string who, string endpoint, int expected)
    {
        // Arrange
        var scenario = await SeedAsync(withOverride: true);

        // Act
        using var response = await CallAsync(endpoint, scenario, Token(who));

        // Assert
        ((int)response.StatusCode).ShouldBe(expected);
    }

    [Theory(DisplayName = "The advancement write endpoints return 404 for an unknown Series and for an unknown Stage")]
    [InlineData("Recompute", true)]
    [InlineData("Recompute", false)]
    [InlineData("PickSquad", true)]
    [InlineData("PickSquad", false)]
    [InlineData("CreateOverride", true)]
    [InlineData("CreateOverride", false)]
    [InlineData("RemoveOverride", true)]
    [InlineData("RemoveOverride", false)]
    public async Task Endpoint_ShouldReturn404_WhenSeriesOrStageIsUnknown(string endpoint, bool unknownSeries)
    {
        // Arrange
        var scenario = await SeedAsync(withOverride: true);
        var seriesId = unknownSeries ? SeriesId.New().Value.ToString() : scenario.Id;
        var stageId = unknownSeries ? scenario.StageId(1) : SeriesStageId.New().Value.ToString();
        var token = SeriesApiHost.AdminToken();

        // Act
        using var response = endpoint switch
        {
            "Recompute" => await RecomputeAsync(seriesId, stageId, token, 0),
            "PickSquad" => await PickSquadAsync(seriesId, stageId, token, scenario.OtherSquad.Value.ToString(), 0),
            "CreateOverride" => await CreateOverrideAsync(seriesId, stageId, token, OverrideKindValues.Add, scenario.Ninth.Value.ToString(), 0),
            _ => await RemoveOverrideAsync(seriesId, stageId, token, scenario.OverrideId!.Value.Value.ToString(), 0),
        };

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory(DisplayName = "The advancement write endpoints return 409 for a stale Version and change nothing")]
    [InlineData("Recompute")]
    [InlineData("PickSquad")]
    [InlineData("CreateOverride")]
    [InlineData("RemoveOverride")]
    public async Task Endpoint_ShouldReturn409_WhenVersionIsStale(string endpoint)
    {
        // Arrange
        var scenario = await SeedAsync(withOverride: true);
        var version = await VersionAsync(scenario.Series.Id);
        var stale = version + 5;
        var token = SeriesApiHost.AdminToken();

        // Act
        using var response = endpoint switch
        {
            "Recompute" => await RecomputeAsync(scenario.Id, scenario.StageId(1), token, stale),
            "PickSquad" => await PickSquadAsync(scenario.Id, scenario.StageId(2), token, scenario.OtherSquad.Value.ToString(), stale),
            "CreateOverride" => await CreateOverrideAsync(scenario.Id, scenario.StageId(1), token, OverrideKindValues.Add, scenario.Ninth.Value.ToString(), stale),
            _ => await RemoveOverrideAsync(scenario.Id, scenario.StageId(1), token, scenario.OverrideId!.Value.Value.ToString(), stale),
        };

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await VersionAsync(scenario.Series.Id)).ShouldBe(version);
    }

    // ---- POST /series/{id}/stages/{stageId}/recomputations ----

    [Fact(DisplayName = "POST recomputations returns 204 and keeps the stored output when nothing changed")]
    public async Task Recompute_ShouldReturn204_WhenNothingChanged()
    {
        // Arrange
        var scenario = await SeedAsync();
        var version = await VersionAsync(scenario.Series.Id);

        // Act
        using var response = await RecomputeAsync(scenario.Id, scenario.StageId(1), SeriesApiHost.AdminToken(), version);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await VersionAsync(scenario.Series.Id)).ShouldBe(version);
    }

    [Fact(DisplayName = "POST recomputations returns 409 with one Series.Stages[n] failure per official Stage and saves nothing when the caller has not confirmed")]
    public async Task Recompute_ShouldReturn409WithOneFailurePerOfficialStage_WhenNotConfirmed()
    {
        // Arrange
        var scenario = await SeedAsync();
        await MakeQualifyingStageOfficialAsync(scenario);
        var version = await VersionAsync(scenario.Series.Id);

        // Act
        using var response = await RecomputeAsync(scenario.Id, scenario.StageId(1), SeriesApiHost.AdminToken(), version);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ErrorNamesAsync(response)).ShouldBe(["series.Stages[0]"]);
        (await VersionAsync(scenario.Series.Id)).ShouldBe(version);
        (await StoredAsync(scenario.Series.Id)).Stages.Single(stage => stage.Order == 1).Status.ShouldBe(StageStatus.Official);
    }

    [Fact(DisplayName = "POST recomputations returns 204 and resets the official Stage to Computed when the caller confirms")]
    public async Task Recompute_ShouldReturn204AndResetOfficialStage_WhenConfirmed()
    {
        // Arrange
        var scenario = await SeedAsync();
        await MakeQualifyingStageOfficialAsync(scenario);
        var version = await VersionAsync(scenario.Series.Id);

        // Act
        using var response = await RecomputeAsync(scenario.Id, scenario.StageId(1), SeriesApiHost.AdminToken(), version, confirm: true);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await VersionAsync(scenario.Series.Id)).ShouldBeGreaterThan(version);
        (await StoredAsync(scenario.Series.Id)).Stages.Single(stage => stage.Order == 1).Status.ShouldBe(StageStatus.Computed);
    }

    [Theory(DisplayName = "POST recomputations returns 400 when the Series id or the Stage id is not 26 characters")]
    [InlineData("short", "00000000000000000000000000")]
    [InlineData("0000000000000000000000000", "00000000000000000000000000")]
    [InlineData("00000000000000000000000000", "short")]
    [InlineData("00000000000000000000000000", "000000000000000000000000000")]
    public async Task Recompute_ShouldReturn400_WhenAnIdHasWrongLength(string seriesId, string stageId)
    {
        // Arrange & Act
        using var response = await RecomputeAsync(seriesId, stageId, SeriesApiHost.AdminToken(), 0);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST recomputations returns 400 for a negative Version")]
    public async Task Recompute_ShouldReturn400_ForNegativeVersion()
    {
        // Arrange
        var scenario = await SeedAsync();

        // Act
        using var response = await RecomputeAsync(scenario.Id, scenario.StageId(1), SeriesApiHost.AdminToken(), -1);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ---- PUT /series/{id}/stages/{stageId}/squad ----

    [Fact(DisplayName = "PUT squad returns 204, designates the squad and moves Version")]
    public async Task PickSquad_ShouldReturn204AndDesignateSquad()
    {
        // Arrange
        var scenario = await SeedAsync();
        var version = await VersionAsync(scenario.Series.Id);
        (await StoredAsync(scenario.Series.Id)).Stages.Single(stage => stage.Order == 2).SquadDesignation!.SquadId.ShouldBe(scenario.MatchingSquad);

        // Act
        using var response = await PickSquadAsync(
            scenario.Id, scenario.StageId(2), SeriesApiHost.AdminToken(), scenario.OtherSquad.Value.ToString(), version);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await VersionAsync(scenario.Series.Id)).ShouldBeGreaterThan(version);
        (await StoredAsync(scenario.Series.Id)).Stages.Single(stage => stage.Order == 2).SquadDesignation!.SquadId.ShouldBe(scenario.OtherSquad);
    }

    [Fact(DisplayName = "PUT squad returns 422 for a Stage that is not a Survivor Stage")]
    public async Task PickSquad_ShouldReturn422_WhenStageIsNotSurvivor()
    {
        // Arrange
        var scenario = await SeedAsync();
        var version = await VersionAsync(scenario.Series.Id);

        // Act
        using var response = await PickSquadAsync(
            scenario.Id, scenario.StageId(1), SeriesApiHost.AdminToken(), scenario.OtherSquad.Value.ToString(), version);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact(DisplayName = "PUT squad returns 422 for a squad that is not in the Stage's tournament")]
    public async Task PickSquad_ShouldReturn422_WhenSquadIsNotInTournament()
    {
        // Arrange
        var scenario = await SeedAsync();
        var version = await VersionAsync(scenario.Series.Id);

        // Act
        using var response = await PickSquadAsync(
            scenario.Id, scenario.StageId(2), SeriesApiHost.AdminToken(), SquadId.New().Value.ToString(), version);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await VersionAsync(scenario.Series.Id)).ShouldBe(version);
    }

    [Theory(DisplayName = "PUT squad returns 400 when an id is not 26 characters")]
    [InlineData("Id", 25)]
    [InlineData("Id", 27)]
    [InlineData("StageId", 25)]
    [InlineData("StageId", 27)]
    [InlineData("SquadId", 25)]
    [InlineData("SquadId", 27)]
    public async Task PickSquad_ShouldReturn400_WhenAnIdHasWrongLength(string field, int length)
    {
        // Arrange
        var bad = new string('0', length);
        var good = new string('0', 26);

        // Act
        using var response = await PickSquadAsync(
            field == "Id" ? bad : good, field == "StageId" ? bad : good, SeriesApiHost.AdminToken(), field == "SquadId" ? bad : good, 0);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "PUT squad returns 400 for a negative Version")]
    public async Task PickSquad_ShouldReturn400_ForNegativeVersion()
    {
        // Arrange
        var scenario = await SeedAsync();

        // Act
        using var response = await PickSquadAsync(
            scenario.Id, scenario.StageId(2), SeriesApiHost.AdminToken(), scenario.OtherSquad.Value.ToString(), -1);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ---- POST /series/{id}/stages/{stageId}/overrides ----

    [Fact(DisplayName = "POST overrides returns 201 with the override id and the new Version, and adds the bowler to the field")]
    public async Task CreateOverride_ShouldReturn201WithIdAndVersion_ForAdd()
    {
        // Arrange
        var scenario = await SeedAsync();
        var version = await VersionAsync(scenario.Series.Id);

        // Act
        using var response = await CreateOverrideAsync(
            scenario.Id, scenario.StageId(1), SeriesApiHost.AdminToken(), OverrideKindValues.Add, scenario.Ninth.Value.ToString(), version);
        var body = await response.Content.ReadFromJsonAsync<CreatedStageOverrideResponse>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location!.ToString().ShouldEndWith($"/series/{scenario.Id}/advancement");
        var stored = await StoredAsync(scenario.Series.Id);
        var saved = stored.Stages.Single(stage => stage.Order == 1).Overrides.Single();
        body.ShouldNotBeNull();
        body.OverrideId.ShouldBe(saved.Id.Value.ToString());
        body.Version.ShouldBe(stored.Version);
        body.Version.ShouldBeGreaterThan(version);
        saved.Kind.ShouldBe(OverrideKind.Add);
        stored.FieldEntries.Single(entry => entry.BowlerId == scenario.Ninth).ViaOverride.ShouldBeTrue();
    }

    [Fact(DisplayName = "POST overrides returns 201 for a Clear and vacates the bowler's slot")]
    public async Task CreateOverride_ShouldReturn201_ForClear()
    {
        // Arrange
        var scenario = await SeedAsync();
        var version = await VersionAsync(scenario.Series.Id);

        // Act
        using var response = await CreateOverrideAsync(
            scenario.Id, scenario.StageId(1), SeriesApiHost.AdminToken(), OverrideKindValues.Clear, scenario.Holder.Value.ToString(), version);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var stored = await StoredAsync(scenario.Series.Id);
        stored.Stages.Single(stage => stage.Order == 1).Overrides.Single().Kind.ShouldBe(OverrideKind.Clear);
        stored.FieldEntries.Select(entry => entry.BowlerId).ShouldNotContain(scenario.Holder);
    }

    [Fact(DisplayName = "POST overrides stores the caller's user id as CreatedBy")]
    public async Task CreateOverride_ShouldStoreCallerAsCreatedBy()
    {
        // Arrange
        var scenario = await SeedAsync();
        var version = await VersionAsync(scenario.Series.Id);
        var caller = Ulid.NewUlid();
        var token = TestAccessTokenFactory.Create(roles: [SecurityRoles.Webmaster], permissions: [Permissions.ManageAdvancement], userId: caller);

        // Act
        using var response = await CreateOverrideAsync(
            scenario.Id, scenario.StageId(1), token, OverrideKindValues.Add, scenario.Ninth.Value.ToString(), version);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await StoredAsync(scenario.Series.Id)).Stages.Single(stage => stage.Order == 1).Overrides.Single().CreatedBy.ShouldBe(caller.ToString());
    }

    [Fact(DisplayName = "POST overrides returns 401 when the token has no user id claim")]
    public async Task CreateOverride_ShouldReturn401_WhenTokenHasNoUserId()
    {
        // Arrange
        var scenario = await SeedAsync();
        var version = await VersionAsync(scenario.Series.Id);
        var token = TestAccessTokenFactory.Create(roles: [SecurityRoles.Webmaster], permissions: [Permissions.ManageAdvancement], omitUserId: true);

        // Act
        using var response = await CreateOverrideAsync(
            scenario.Id, scenario.StageId(1), token, OverrideKindValues.Add, scenario.Ninth.Value.ToString(), version);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await VersionAsync(scenario.Series.Id)).ShouldBe(version);
    }

    [Theory(DisplayName = "POST overrides returns 400 for a Kind that is not 1 or 2")]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(-1)]
    public async Task CreateOverride_ShouldReturn400_ForKindOutsideAddAndClear(int kind)
    {
        // Arrange
        var scenario = await SeedAsync();

        // Act
        using var response = await CreateOverrideAsync(
            scenario.Id, scenario.StageId(1), SeriesApiHost.AdminToken(), kind, scenario.Ninth.Value.ToString(), 0);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory(DisplayName = "POST overrides returns 400 for an empty or blank reason")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateOverride_ShouldReturn400_ForBlankReason(string reason)
    {
        // Arrange
        var scenario = await SeedAsync();

        // Act
        using var response = await CreateOverrideAsync(
            scenario.Id, scenario.StageId(1), SeriesApiHost.AdminToken(), OverrideKindValues.Add, scenario.Ninth.Value.ToString(), 0, reason);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST overrides returns 400 for a reason of 501 characters and 201 for one of 500")]
    public async Task CreateOverride_ShouldEnforceTheReasonLimit()
    {
        // Arrange
        var scenario = await SeedAsync();
        var version = await VersionAsync(scenario.Series.Id);
        var token = SeriesApiHost.AdminToken();

        // Act
        using var tooLong = await CreateOverrideAsync(
            scenario.Id, scenario.StageId(1), token, OverrideKindValues.Add, scenario.Ninth.Value.ToString(), version, new string('r', 501));
        using var atTheLimit = await CreateOverrideAsync(
            scenario.Id, scenario.StageId(1), token, OverrideKindValues.Add, scenario.Ninth.Value.ToString(), version, new string('r', 500));

        // Assert
        tooLong.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        atTheLimit.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Theory(DisplayName = "POST overrides returns 400 when an id is not 26 characters")]
    [InlineData("Id", 25)]
    [InlineData("Id", 27)]
    [InlineData("StageId", 25)]
    [InlineData("StageId", 27)]
    [InlineData("BowlerId", 25)]
    [InlineData("BowlerId", 27)]
    public async Task CreateOverride_ShouldReturn400_WhenAnIdHasWrongLength(string field, int length)
    {
        // Arrange
        var bad = new string('0', length);
        var good = new string('0', 26);

        // Act
        using var response = await CreateOverrideAsync(
            field == "Id" ? bad : good,
            field == "StageId" ? bad : good,
            SeriesApiHost.AdminToken(),
            OverrideKindValues.Add,
            field == "BowlerId" ? bad : good,
            0);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST overrides returns 400 for a negative Version")]
    public async Task CreateOverride_ShouldReturn400_ForNegativeVersion()
    {
        // Arrange
        var scenario = await SeedAsync();

        // Act
        using var response = await CreateOverrideAsync(
            scenario.Id, scenario.StageId(1), SeriesApiHost.AdminToken(), OverrideKindValues.Add, scenario.Ninth.Value.ToString(), -1);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "POST overrides returns 409 for an official Stage and keeps the Stage Official")]
    public async Task CreateOverride_ShouldReturn409_WhenStageIsOfficial()
    {
        // Arrange
        var scenario = await SeedAsync();
        await MakeQualifyingStageOfficialAsync(scenario);
        var version = await VersionAsync(scenario.Series.Id);

        // Act
        using var response = await CreateOverrideAsync(
            scenario.Id, scenario.StageId(1), SeriesApiHost.AdminToken(), OverrideKindValues.Add, scenario.Ninth.Value.ToString(), version);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var stored = await StoredAsync(scenario.Series.Id);
        stored.Stages.Single(stage => stage.Order == 1).Status.ShouldBe(StageStatus.Official);
        stored.Stages.Single(stage => stage.Order == 1).Overrides.ShouldBeEmpty();
    }

    [Fact(DisplayName = "POST overrides returns 409 for a bowler who is already in the field")]
    public async Task CreateOverride_ShouldReturn409_WhenBowlerIsAlreadyInField()
    {
        // Arrange
        var scenario = await SeedAsync();
        var version = await VersionAsync(scenario.Series.Id);

        // Act
        using var response = await CreateOverrideAsync(
            scenario.Id, scenario.StageId(1), SeriesApiHost.AdminToken(), OverrideKindValues.Add, scenario.Holder.Value.ToString(), version);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "POST overrides returns 409 for a second Clear of the same bowler")]
    public async Task CreateOverride_ShouldReturn409_ForSecondClearOfTheSameBowler()
    {
        // Arrange
        var scenario = await SeedAsync();
        var token = SeriesApiHost.AdminToken();
        using var first = await CreateOverrideAsync(
            scenario.Id, scenario.StageId(1), token, OverrideKindValues.Clear, scenario.Holder.Value.ToString(), await VersionAsync(scenario.Series.Id));
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act
        using var second = await CreateOverrideAsync(
            scenario.Id, scenario.StageId(1), token, OverrideKindValues.Clear, scenario.Holder.Value.ToString(), await VersionAsync(scenario.Series.Id));

        // Assert
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "POST overrides returns 422 for a Stage that is not a Qualifying Stage")]
    public async Task CreateOverride_ShouldReturn422_WhenStageIsNotQualifying()
    {
        // Arrange
        var scenario = await SeedAsync();
        var version = await VersionAsync(scenario.Series.Id);

        // Act
        using var response = await CreateOverrideAsync(
            scenario.Id, scenario.StageId(2), SeriesApiHost.AdminToken(), OverrideKindValues.Add, scenario.Ninth.Value.ToString(), version);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact(DisplayName = "POST overrides returns 422 when an Add names a bowler with no result row")]
    public async Task CreateOverride_ShouldReturn422_WhenBowlerHasNoResultRow()
    {
        // Arrange
        var scenario = await SeedAsync();
        var version = await VersionAsync(scenario.Series.Id);

        // Act
        using var response = await CreateOverrideAsync(
            scenario.Id, scenario.StageId(1), SeriesApiHost.AdminToken(), OverrideKindValues.Add, scenario.Bowlers[11].Id.Value.ToString(), version);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact(DisplayName = "POST overrides returns 422 when a Clear names a bowler who holds no slot")]
    public async Task CreateOverride_ShouldReturn422_WhenBowlerHoldsNoSlot()
    {
        // Arrange
        var scenario = await SeedAsync();
        var version = await VersionAsync(scenario.Series.Id);

        // Act
        using var response = await CreateOverrideAsync(
            scenario.Id, scenario.StageId(1), SeriesApiHost.AdminToken(), OverrideKindValues.Clear, scenario.Ninth.Value.ToString(), version);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    // ---- DELETE /series/{id}/stages/{stageId}/overrides/{overrideId}?version=n ----

    [Fact(DisplayName = "DELETE overrides returns 204, removes the override and takes the bowler out of the field")]
    public async Task RemoveOverride_ShouldReturn204AndRemoveOverride()
    {
        // Arrange
        var scenario = await SeedAsync(withOverride: true);
        var version = await VersionAsync(scenario.Series.Id);
        (await StoredAsync(scenario.Series.Id)).FieldEntries.Select(entry => entry.BowlerId).ShouldContain(scenario.Tenth);

        // Act
        using var response = await RemoveOverrideAsync(
            scenario.Id, scenario.StageId(1), SeriesApiHost.AdminToken(), scenario.OverrideId!.Value.Value.ToString(), version);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var stored = await StoredAsync(scenario.Series.Id);
        stored.Stages.Single(stage => stage.Order == 1).Overrides.ShouldBeEmpty();
        stored.FieldEntries.Select(entry => entry.BowlerId).ShouldNotContain(scenario.Tenth);
        stored.Version.ShouldBeGreaterThan(version);
    }

    [Fact(DisplayName = "DELETE overrides returns 400 when the version query value is missing")]
    public async Task RemoveOverride_ShouldReturn400_WhenVersionIsMissing()
    {
        // Arrange
        var scenario = await SeedAsync(withOverride: true);

        // Act
        using var response = await RemoveOverrideAsync(
            scenario.Id, scenario.StageId(1), SeriesApiHost.AdminToken(), scenario.OverrideId!.Value.Value.ToString(), null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await StoredAsync(scenario.Series.Id)).Stages.Single(stage => stage.Order == 1).Overrides.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "DELETE overrides returns 400 for a negative version")]
    public async Task RemoveOverride_ShouldReturn400_ForNegativeVersion()
    {
        // Arrange
        var scenario = await SeedAsync(withOverride: true);

        // Act
        using var response = await RemoveOverrideAsync(
            scenario.Id, scenario.StageId(1), SeriesApiHost.AdminToken(), scenario.OverrideId!.Value.Value.ToString(), -1);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory(DisplayName = "DELETE overrides returns 400 when an id is not 26 characters")]
    [InlineData("Id", 25)]
    [InlineData("Id", 27)]
    [InlineData("StageId", 25)]
    [InlineData("StageId", 27)]
    [InlineData("OverrideId", 25)]
    [InlineData("OverrideId", 27)]
    public async Task RemoveOverride_ShouldReturn400_WhenAnIdHasWrongLength(string field, int length)
    {
        // Arrange
        var bad = new string('0', length);
        var good = new string('0', 26);

        // Act
        using var response = await RemoveOverrideAsync(
            field == "Id" ? bad : good, field == "StageId" ? bad : good, SeriesApiHost.AdminToken(), field == "OverrideId" ? bad : good, 0);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "DELETE overrides returns 204 for an override that is already gone, even with a stale version")]
    public async Task RemoveOverride_ShouldReturn204_WhenOverrideIsMissing()
    {
        // Arrange
        var scenario = await SeedAsync(withOverride: true);
        var version = await VersionAsync(scenario.Series.Id);

        // Act
        using var response = await RemoveOverrideAsync(
            scenario.Id, scenario.StageId(1), SeriesApiHost.AdminToken(), StageOverrideId.New().Value.ToString(), version + 5);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await VersionAsync(scenario.Series.Id)).ShouldBe(version);
        (await StoredAsync(scenario.Series.Id)).Stages.Single(stage => stage.Order == 1).Overrides.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "DELETE overrides returns 409 for an official Stage and keeps the override")]
    public async Task RemoveOverride_ShouldReturn409_WhenStageIsOfficial()
    {
        // Arrange
        var scenario = await SeedAsync(withOverride: true);
        await MakeQualifyingStageOfficialAsync(scenario);
        var version = await VersionAsync(scenario.Series.Id);

        // Act
        using var response = await RemoveOverrideAsync(
            scenario.Id, scenario.StageId(1), SeriesApiHost.AdminToken(), scenario.OverrideId!.Value.Value.ToString(), version);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await StoredAsync(scenario.Series.Id)).Stages.Single(stage => stage.Order == 1).Overrides.Count.ShouldBe(1);
    }
}