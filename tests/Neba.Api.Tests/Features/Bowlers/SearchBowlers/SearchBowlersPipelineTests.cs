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
using Neba.Api.Contracts.Bowlers.SearchBowlers;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Bowlers.SearchBowlers;
using Neba.Api.Versioning;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Bowlers.SearchBowlers;

// Runs the endpoint through the real FastEndpoints pipeline (routing, query binding, validation) with the
// query handler replaced, so it needs no database.
[IntegrationTest]
[Component("Bowlers")]
[SuppressMessage("Design", "CA2213:Disposable fields should be disposed", Justification = "_app is intentionally never disposed - see DisposeAsync.")]
public sealed class SearchBowlersPipelineTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private Mock<NebaMessaging.IQueryHandler<SearchBowlersQuery, IReadOnlyCollection<BowlerSearchSummary>>> _handler = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseSetting(WebHostDefaults.ServerUrlsKey, "http://127.0.0.1:0");

        _handler = new Mock<NebaMessaging.IQueryHandler<SearchBowlersQuery, IReadOnlyCollection<BowlerSearchSummary>>>(MockBehavior.Strict);
        builder.Services.AddSingleton(_handler.Object);

        builder.Services
            .AddFastEndpoints(options =>
            {
                options.Assemblies = [typeof(SearchBowlersEndpoint).Assembly];
                options.Filter = type => type == typeof(SearchBowlersEndpoint) || type == typeof(SearchBowlersRequestValidator);
            })
            .AddVersioning();

        _app = builder.Build();
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

    [Fact(DisplayName = "GET /bowlers?name= returns 200 with the matching bowlers")]
    public async Task Get_ShouldReturn200WithResults_WhenNameIsValid()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        SearchBowlersQuery? captured = null;
        IReadOnlyCollection<BowlerSearchSummary> summaries =
        [
            new() { Id = BowlerId.New(), Name = NameFactory.Create(firstName: "David", lastName: "Smith"), UsbcId = "4821-09317" }
        ];
        _handler
            .Setup(handler => handler.HandleAsync(It.IsAny<SearchBowlersQuery>(), It.IsAny<CancellationToken>()))
            .Callback<SearchBowlersQuery, CancellationToken>((query, _) => captured = query)
            .ReturnsAsync(summaries);

        // Act
        using var response = await _client.GetAsync(new Uri("/bowlers?name=smith", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CollectionResponse<BowlerSearchResponse>>(ct);
        body.ShouldNotBeNull().Items.ShouldHaveSingleItem().UsbcId.ShouldBe("4821-09317");
        captured.ShouldNotBeNull().Name.ShouldBe("smith");
    }

    [Theory(DisplayName = "GET /bowlers returns 400 when the name is missing or too short")]
    [InlineData("", TestDisplayName = "No name")]
    [InlineData("?name=ab", TestDisplayName = "Two characters")]
    public async Task Get_ShouldReturn400_WhenNameIsMissingOrShort(string query)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await _client.GetAsync(new Uri("/bowlers" + query, UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}