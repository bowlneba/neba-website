using ErrorOr;

using FastEndpoints;

using Neba.Api.Features.Series.DeleteSeries;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Series.DeleteSeries;

[UnitTest]
[Component("Series")]
public sealed class DeleteSeriesEndpointTests
{
    private static readonly string Id = new('3', 26);

    private static Mock<NebaMessaging.ICommandHandler<DeleteSeriesCommand, Deleted>> Handler()
        => new(MockBehavior.Strict);

    [Fact(DisplayName = "Configure should register a permission-protected DELETE route at /series/{id}")]
    public void Configure_ShouldRegisterPermissionProtectedDeleteRoute()
    {
        // Arrange & Act
        var endpoint = Factory.Create<DeleteSeriesEndpoint>(Handler().Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("DELETE");
        endpoint.Definition.Routes.ShouldContain("/series/{id}");
        endpoint.Definition.PreBuiltUserPolicies!.ShouldContain("Permission:Series.DeleteSeries");
    }

    [Fact(DisplayName = "HandleAsync should map the id to the command and return 204")]
    public async Task HandleAsync_ShouldMapId_AndReturn204()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        DeleteSeriesCommand? captured = null;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<DeleteSeriesCommand>(), ct))
            .Callback<DeleteSeriesCommand, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync(Result.Deleted);
        var endpoint = Factory.Create<DeleteSeriesEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(new DeleteSeriesRequest { Id = Id }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        captured!.SeriesId.ShouldBe(new SeriesId(Id));
    }

    [Fact(DisplayName = "HandleAsync should return 409 when the Series is locked")]
    public async Task HandleAsync_ShouldReturn409_WhenLocked()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<DeleteSeriesCommand>(), ct)).ReturnsAsync(SeriesErrors.Locked);
        var endpoint = Factory.Create<DeleteSeriesEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(new DeleteSeriesRequest { Id = Id }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(409);
    }
}