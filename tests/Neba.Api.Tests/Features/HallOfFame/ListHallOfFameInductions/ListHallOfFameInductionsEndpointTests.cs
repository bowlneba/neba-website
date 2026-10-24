using FastEndpoints;

using Neba.Api.Features.HallOfFame.ListHallOfFameInductions;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.HallOfFame;

namespace Neba.Api.Tests.Features.HallOfFame.ListHallOfFameInductions;

[UnitTest]
[Component("HallOfFame")]
public sealed class ListHallOfFameInductionsEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with mapped inductions when query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedInductions_WhenQuerySucceeds()
    {
        // Arrange
        var dtos = HallOfFameInductionDtoFactory.Bogus(3, 11);

        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListHallOfFameInductionsQuery, IReadOnlyCollection<HallOfFameInductionDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.IsAny<ListHallOfFameInductionsQuery>(),
                cancellationToken))
            .ReturnsAsync(dtos);

        var endpoint = Factory.Create<ListHallOfFameInductionsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "HandleAsync should map the id, focal point, and zoom into the response")]
    public async Task HandleAsync_ShouldMapIdFocalPointAndZoom_WhenInductionHasPhoto()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var dto = HallOfFameInductionDtoFactory.Create(
            id: "01JZ8K3M4N5P6Q7R8S9T0V1W3Y",
            photoUri: new Uri("https://files.example.test/jane.jpg"),
            photoFocalX: 31,
            photoFocalY: 36,
            photoZoom: 175);

        var queryHandlerMock = new Mock<IQueryHandler<ListHallOfFameInductionsQuery, IReadOnlyCollection<HallOfFameInductionDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(It.IsAny<ListHallOfFameInductionsQuery>(), cancellationToken))
            .ReturnsAsync([dto]);

        var endpoint = Factory.Create<ListHallOfFameInductionsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        var item = endpoint.Response.Items.Single();
        item.Id.ShouldBe("01JZ8K3M4N5P6Q7R8S9T0V1W3Y");
        item.PhotoFocalX.ShouldBe(31);
        item.PhotoFocalY.ShouldBe(36);
        item.PhotoZoom.ShouldBe(175);
    }

    [Fact(DisplayName = "Configure should register anonymous GET route at /hall-of-fame/inductions")]
    public void Configure_ShouldRegisterAnonymousGetRoute_AtExpectedPath()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<ListHallOfFameInductionsQuery, IReadOnlyCollection<HallOfFameInductionDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<ListHallOfFameInductionsEndpoint>(queryHandlerMock.Object);

        // Assert — route and auth
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain("/hall-of-fame/inductions");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();

    }

    [Fact(DisplayName = "HandleAsync should return OK with empty collection when no inductions exist")]
    public async Task HandleAsync_ShouldReturnOkWithEmptyCollection_WhenNoInductionsExist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListHallOfFameInductionsQuery, IReadOnlyCollection<HallOfFameInductionDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.IsAny<ListHallOfFameInductionsQuery>(),
                cancellationToken))
            .ReturnsAsync([]);

        var endpoint = Factory.Create<ListHallOfFameInductionsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.TotalItems.ShouldBe(0);
        endpoint.Response.Items.ShouldBeEmpty();
    }
}