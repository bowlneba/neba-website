using FastEndpoints;

using Neba.Api.Security.ListUsers;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security.ListUsers;

[UnitTest]
[Component("Security")]
public sealed class ListUsersEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with mapped users when query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedUsers_WhenQuerySucceeds()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var userId = Ulid.NewUlid();
        var dtos = new List<UserSummaryDto>
        {
            new()
            {
                UserId = userId,
                Email = "webmaster@bowlneba.com",
                EmailConfirmed = true,
                Roles = ["Webmaster"]
            }
        };

        var queryHandlerMock = new Mock<IQueryHandler<ListUsersQuery, IReadOnlyCollection<UserSummaryDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(It.IsAny<ListUsersQuery>(), ct))
            .ReturnsAsync(dtos);

        var endpoint = Factory.Create<ListUsersEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Items.ShouldHaveSingleItem();
        var response = endpoint.Response.Items.Single();
        response.UserId.ShouldBe(userId.ToString());
        response.Email.ShouldBe("webmaster@bowlneba.com");
        response.EmailConfirmed.ShouldBeTrue();
        response.Roles.ShouldBe(["Webmaster"]);
    }

    [Fact(DisplayName = "HandleAsync should return OK with empty collection when no users exist")]
    public async Task HandleAsync_ShouldReturnOkWithEmptyCollection_WhenNoUsersExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListUsersQuery, IReadOnlyCollection<UserSummaryDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(It.IsAny<ListUsersQuery>(), ct))
            .ReturnsAsync([]);

        var endpoint = Factory.Create<ListUsersEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Items.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Configure should register authenticated GET route containing 'users'")]
    public void Configure_ShouldRegisterAuthenticatedGetRoute_ContainingUsers()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<ListUsersQuery, IReadOnlyCollection<UserSummaryDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<ListUsersEndpoint>(queryHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("users"), "should include a 'users' route");
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }
}
