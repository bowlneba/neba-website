using ErrorOr;

using FastEndpoints;

using Neba.Api.Sponsors.GetSponsorDetail;
using Neba.Application.Messaging;
using Neba.Application.Sponsors.GetSponsorDetail;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Sponsors;

namespace Neba.Api.Tests.Sponsors.GetSponsorDetail;

[UnitTest]
[Component("Sponsors")]
public sealed class GetSponsorDetailEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with mapped sponsor detail when query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedSponsorDetail_WhenQuerySucceeds()
    {
        // Arrange
        var dto = SponsorDetailDtoFactory.Bogus(count: 1, seed: 555).Single();
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<GetSponsorDetailQuery, ErrorOr<SponsorDetailDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetSponsorDetailQuery>(), cancellationToken))
            .ReturnsAsync(dto);

        var endpoint = Factory.Create<GetSponsorDetailEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(new GetSponsorDetailRequest { Slug = dto.Slug }, cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Id.ShouldBe(dto.Id.Value.ToString());
        endpoint.Response.Slug.ShouldBe(dto.Slug);
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "HandleAsync should return 404 Not Found when sponsor does not exist")]
    public async Task HandleAsync_ShouldReturn404_WhenSponsorDoesNotExist()
    {
        // Arrange
        const string slug = "missing-sponsor";
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<GetSponsorDetailQuery, ErrorOr<SponsorDetailDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetSponsorDetailQuery>(), cancellationToken))
            .ReturnsAsync(Error.NotFound());

        var endpoint = Factory.Create<GetSponsorDetailEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(new GetSponsorDetailRequest { Slug = slug }, cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact(DisplayName = "Configure should register anonymous GET route at /sponsors/{slug}")]
    public void Configure_ShouldRegisterAnonymousGetRoute_AtExpectedPath()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<GetSponsorDetailQuery, ErrorOr<SponsorDetailDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<GetSponsorDetailEndpoint>(queryHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("sponsors"), "should be under the /sponsors path");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }
}