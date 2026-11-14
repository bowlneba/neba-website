using FastEndpoints;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Bowlers.SearchBowlers;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;

namespace Neba.Api.Tests.Features.Bowlers.SearchBowlers;

[UnitTest]
[Component("Bowlers")]
public sealed class SearchBowlersEndpointTests
{
    [Fact(DisplayName = "HandleAsync should map each bowler to an ID, display name and USBC ID")]
    public async Task HandleAsync_ShouldMapResults()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var id = BowlerId.New();
        IReadOnlyCollection<BowlerSearchSummary> summaries =
        [
            new() { Id = id, Name = NameFactory.Create(firstName: "David", lastName: "Smith", nickname: "Dave"), UsbcId = "4821-09317" },
            new() { Id = BowlerId.New(), Name = NameFactory.Create(firstName: "Dana", lastName: "Smith") }
        ];
        SearchBowlersQuery? captured = null;
        var handler = new Mock<IQueryHandler<SearchBowlersQuery, IReadOnlyCollection<BowlerSearchSummary>>>(MockBehavior.Strict);
        handler.Setup(h => h.HandleAsync(It.IsAny<SearchBowlersQuery>(), ct))
            .Callback<SearchBowlersQuery, CancellationToken>((query, _) => captured = query)
            .ReturnsAsync(summaries);
        var endpoint = Factory.Create<SearchBowlersEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(new SearchBowlersRequest { Name = "smith" }, ct);

        // Assert
        captured.ShouldNotBeNull().Name.ShouldBe("smith");
        endpoint.Response.Items.Count.ShouldBe(2);
        var first = endpoint.Response.Items.First();
        first.BowlerId.ShouldBe(id.Value.ToString());
        first.Name.ShouldBe("Dave Smith");
        first.UsbcId.ShouldBe("4821-09317");
        endpoint.Response.Items.Last().UsbcId.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should return an empty list when nobody matches")]
    public async Task HandleAsync_ShouldReturnEmptyList_WhenNobodyMatches()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = new Mock<IQueryHandler<SearchBowlersQuery, IReadOnlyCollection<BowlerSearchSummary>>>(MockBehavior.Strict);
        handler.Setup(h => h.HandleAsync(It.IsAny<SearchBowlersQuery>(), ct)).ReturnsAsync([]);
        var endpoint = Factory.Create<SearchBowlersEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(new SearchBowlersRequest { Name = "zzz" }, ct);

        // Assert
        endpoint.Response.Items.ShouldBeEmpty();
        endpoint.Response.TotalItems.ShouldBe(0);
    }

    [Fact(DisplayName = "Configure should register an anonymous GET route at /bowlers")]
    public void Configure_ShouldRegisterAnonymousGetRoute()
    {
        // Arrange
        var handler = new Mock<IQueryHandler<SearchBowlersQuery, IReadOnlyCollection<BowlerSearchSummary>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<SearchBowlersEndpoint>(handler.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain(route => route.Contains("bowlers", StringComparison.Ordinal), string.Join(",", endpoint.Definition.Routes));
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }
}