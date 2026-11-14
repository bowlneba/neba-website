using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;

using ErrorOr;

using FastEndpoints;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Contracts.Tournaments.CheckTeamEligibility;
using Neba.Api.Features.Tournaments;
using Neba.Api.Features.Tournaments.CheckTeamEligibility;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Versioning;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Tournaments.CheckTeamEligibility;

// Runs the endpoint through the real FastEndpoints pipeline (routing, query binding, validation, error
// mapping) with the query handler replaced, so it needs no database.
[IntegrationTest]
[Component("Tournaments")]
[SuppressMessage("Design", "CA2213:Disposable fields should be disposed", Justification = "_app is intentionally never disposed - see DisposeAsync.")]
public sealed class CheckTeamEligibilityPipelineTests : IAsyncLifetime
{
    private const string TournamentId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2E";
    private const string FirstBowlerId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2F";
    private const string SecondBowlerId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2G";

    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private Mock<NebaMessaging.IQueryHandler<CheckTeamEligibilityQuery, ErrorOr<TeamEligibilityDto>>> _handler = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseSetting(WebHostDefaults.ServerUrlsKey, "http://127.0.0.1:0");

        _handler = new Mock<NebaMessaging.IQueryHandler<CheckTeamEligibilityQuery, ErrorOr<TeamEligibilityDto>>>(MockBehavior.Strict);
        builder.Services.AddSingleton(_handler.Object);

        builder.Services
            .AddFastEndpoints(options =>
            {
                options.Assemblies = [typeof(CheckTeamEligibilityEndpoint).Assembly];
                options.Filter = type => type == typeof(CheckTeamEligibilityEndpoint) || type == typeof(CheckTeamEligibilityRequestValidator);
            })
            .AddVersioning();

        _app = builder.Build();

        // Same reason as the other pipeline tests: FastEndpoints' validator options are process-wide.
        _app.UseFastEndpoints(config => config.Validation.UsePropertyNamingPolicy = false);

        await _app.StartAsync(TestContext.Current.CancellationToken);

        var address = _app.Services
            .GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses
            .First();

        _client = new HttpClient { BaseAddress = new Uri(address) };
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.StopAsync();

        // Deliberately not disposing _app: UseFastEndpoints() points FastEndpoints' process-wide static
        // service resolver at this host's provider. Disposing it would break unrelated tests that run later.
    }

    [Fact(DisplayName = "GET /tournaments/{id}/team-eligibility returns 200 and binds repeated bowlerIds and unknownBowlerCount")]
    public async Task Get_ShouldReturn200AndBindQueryValues_WhenRequestIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        CheckTeamEligibilityQuery? captured = null;
        _handler
            .Setup(handler => handler.HandleAsync(It.IsAny<CheckTeamEligibilityQuery>(), It.IsAny<CancellationToken>()))
            .Callback<CheckTeamEligibilityQuery, CancellationToken>((query, _) => captured = query)
            .ReturnsAsync(TeamEligibilityDtoFactory.Create());

        // Act
        using var response = await _client.GetAsync(new Uri($"/tournaments/{TournamentId}/team-eligibility?bowlerIds={FirstBowlerId}&bowlerIds={SecondBowlerId}&unknownBowlerCount=1", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TeamEligibilityResponse>(ct);
        body.ShouldNotBeNull().Outcome.ShouldBe("Eligible");
        captured.ShouldNotBeNull();
        captured.TournamentId.Value.ToString().ShouldBe(TournamentId);
        captured.BowlerIds.Select(id => id.Value.ToString()).ShouldBe([FirstBowlerId, SecondBowlerId]);
        captured.UnknownBowlerCount.ShouldBe(1);
    }

    [Fact(DisplayName = "GET /tournaments/{id}/team-eligibility returns 400 when a bowler ID is not a 26-character ULID")]
    public async Task Get_ShouldReturn400_WhenBowlerIdIsInvalid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await _client.GetAsync(new Uri($"/tournaments/{TournamentId}/team-eligibility?bowlerIds=short&unknownBowlerCount=1", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "GET /tournaments/{id}/team-eligibility returns 404 when the handler reports a missing tournament")]
    public async Task Get_ShouldReturn404_WhenTournamentIsMissing()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        _handler
            .Setup(handler => handler.HandleAsync(It.IsAny<CheckTeamEligibilityQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.NotFound("Tournament.NotFound", "Tournament was not found."));

        // Act
        using var response = await _client.GetAsync(new Uri($"/tournaments/{TournamentId}/team-eligibility?unknownBowlerCount=2", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "GET /tournaments/{id}/team-eligibility returns 422 when the handler reports a team size mismatch")]
    public async Task Get_ShouldReturn422_WhenTeamSizeMismatches()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        _handler
            .Setup(handler => handler.HandleAsync(It.IsAny<CheckTeamEligibilityQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TeamEligibilityErrors.TeamSizeMismatch(2));

        // Act
        using var response = await _client.GetAsync(new Uri($"/tournaments/{TournamentId}/team-eligibility?unknownBowlerCount=1", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }
}