using FastEndpoints;

using Neba.Api.Messaging;
using Neba.Api.ReferenceData.ListPhoneNumberTypes;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.ReferenceData.ListPhoneNumberTypes;

[UnitTest]
[Component("ReferenceData")]
public sealed class ListPhoneNumberTypesEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with mapped phone number types when query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedPhoneNumberTypes_WhenQuerySucceeds()
    {
        // Arrange
        IReadOnlyCollection<PhoneNumberTypeDto> dtos =
        [
            new PhoneNumberTypeDto { Name = "Home", Code = "H" },
            new PhoneNumberTypeDto { Name = "Mobile", Code = "M" }
        ];

        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListPhoneNumberTypesQuery, IReadOnlyCollection<PhoneNumberTypeDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.IsAny<ListPhoneNumberTypesQuery>(),
                cancellationToken))
            .ReturnsAsync(dtos);

        var endpoint = Factory.Create<ListPhoneNumberTypesEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.TotalItems.ShouldBe(2);
        endpoint.Response.Items.ShouldContain(r => r.Code == "H" && r.Name == "Home");
        endpoint.Response.Items.ShouldContain(r => r.Code == "M" && r.Name == "Mobile");
    }

    [Fact(DisplayName = "Configure should register anonymous GET route at /reference-data/phone-number-types")]
    public void Configure_ShouldRegisterAnonymousGetRoute_AtExpectedPath()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<ListPhoneNumberTypesQuery, IReadOnlyCollection<PhoneNumberTypeDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<ListPhoneNumberTypesEndpoint>(queryHandlerMock.Object);

        // Assert — route and auth
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("phone-number-types"), "should be under the /reference-data group");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should return OK with empty collection when no phone number types are returned")]
    public async Task HandleAsync_ShouldReturnOkWithEmptyCollection_WhenNoTypesReturned()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListPhoneNumberTypesQuery, IReadOnlyCollection<PhoneNumberTypeDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.IsAny<ListPhoneNumberTypesQuery>(),
                cancellationToken))
            .ReturnsAsync([]);

        var endpoint = Factory.Create<ListPhoneNumberTypesEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.TotalItems.ShouldBe(0);
        endpoint.Response.Items.ShouldBeEmpty();
    }
}
