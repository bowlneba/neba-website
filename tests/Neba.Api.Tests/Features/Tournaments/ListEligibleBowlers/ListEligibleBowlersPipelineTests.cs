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

using Neba.Api.Contracts.Tournaments.ListEligibleBowlers;
using Neba.Api.Features.Tournaments;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.ListEligibleBowlers;
using Neba.Api.Versioning;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Tournaments.ListEligibleBowlers;

// Runs the endpoint through the real FastEndpoints pipeline (routing, validation, error mapping) with the
// query handler replaced, so it needs no database.
[IntegrationTest]
[Component("Tournaments")]
[SuppressMessage("Design", "CA2213:Disposable fields should be disposed", Justification = "_app is intentionally never disposed - see DisposeAsync.")]
public sealed class ListEligibleBowlersPipelineTests : IAsyncLifetime
{
    private const string TournamentId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2E";

    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private Mock<NebaMessaging.IQueryHandler<ListEligibleBowlersQuery, ErrorOr<EligibleBowlersDto>>> _handler = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseSetting(WebHostDefaults.ServerUrlsKey, "http://127.0.0.1:0");

        _handler = new Mock<NebaMessaging.IQueryHandler<ListEligibleBowlersQuery, ErrorOr<EligibleBowlersDto>>>(MockBehavior.Strict);
        builder.Services.AddSingleton(_handler.Object);

        builder.Services
            .AddFastEndpoints(options =>
            {
                options.Assemblies = [typeof(ListEligibleBowlersEndpoint).Assembly];
                options.Filter = type => type == typeof(ListEligibleBowlersEndpoint) || type == typeof(ListEligibleBowlersRequestValidator);
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

    [Fact(DisplayName = "GET /tournaments/{id}/eligible-bowlers returns 200 with the requirements and bowlers")]
    public async Task Get_ShouldReturn200WithItems_WhenRequestIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        ListEligibleBowlersQuery? captured = null;
        _handler
            .Setup(handler => handler.HandleAsync(It.IsAny<ListEligibleBowlersQuery>(), It.IsAny<CancellationToken>()))
            .Callback<ListEligibleBowlersQuery, CancellationToken>((query, _) => captured = query)
            .ReturnsAsync(EligibleBowlersDtoFactory.Create(bowlers: EligibleBowlerSummaryDtoFactory.Bogus(3)));

        // Act
        using var response = await _client.GetAsync(new Uri($"/tournaments/{TournamentId}/eligible-bowlers", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<EligibleBowlersResponse>(ct)).ShouldNotBeNull();
        body.Requirements.ShouldNotBeNull().MinTournamentsCount.ShouldBe(3);
        body.Bowlers.Count.ShouldBe(3);
        captured.ShouldNotBeNull().Id.Value.ToString().ShouldBe(TournamentId);
    }

    [Fact(DisplayName = "GET /tournaments/{id}/eligible-bowlers returns 400 when the ID is not 26 characters")]
    public async Task Get_ShouldReturn400_WhenIdIsTooShort()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await _client.GetAsync(new Uri("/tournaments/short/eligible-bowlers", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "GET /tournaments/{id}/eligible-bowlers returns 404 when the handler reports a missing tournament")]
    public async Task Get_ShouldReturn404_WhenTournamentIsMissing()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        _handler
            .Setup(handler => handler.HandleAsync(It.IsAny<ListEligibleBowlersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TournamentErrors.TournamentNotFound(new TournamentId(TournamentId)));

        // Act
        using var response = await _client.GetAsync(new Uri($"/tournaments/{TournamentId}/eligible-bowlers", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
