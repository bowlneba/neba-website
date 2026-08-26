using FastEndpoints;

using Neba.Api.Messaging;
using Neba.Api.Security.ListUsers;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security.ListUsers;

[UnitTest]
[Component("Security")]
public sealed class ListUsersEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with mapped users and pagination when query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedUsersAndPagination_WhenQuerySucceeds()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var userId = Ulid.NewUlid();
        var pagedResult = new PagedResult<UserSummaryDto>(
        [
            new()
            {
                UserId = userId,
                Email = "webmaster@bowlneba.com",
                EmailConfirmed = true,
                Roles = ["Webmaster"]
            }
        ], 1);

        var queryHandlerMock = new Mock<IQueryHandler<ListUsersQuery, PagedResult<UserSummaryDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(It.IsAny<ListUsersQuery>(), ct))
            .ReturnsAsync(pagedResult);

        var endpoint = Factory.Create<ListUsersEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(new ListUsersRequest { Page = 1, PageSize = 20 }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Items.ShouldHaveSingleItem();
        var response = endpoint.Response.Items.Single();
        response.UserId.ShouldBe(userId.ToString());
        response.Email.ShouldBe("webmaster@bowlneba.com");
        response.EmailConfirmed.ShouldBeTrue();
        response.Roles.ShouldBe(["Webmaster"]);
        endpoint.Response.TotalItems.ShouldBe(1);
        endpoint.Response.PageNumber.ShouldBe(1);
        endpoint.Response.PageSize.ShouldBe(20);
    }

    [Fact(DisplayName = "HandleAsync should return OK with empty collection when no users exist")]
    public async Task HandleAsync_ShouldReturnOkWithEmptyCollection_WhenNoUsersExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListUsersQuery, PagedResult<UserSummaryDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(It.IsAny<ListUsersQuery>(), ct))
            .ReturnsAsync(new PagedResult<UserSummaryDto>([], 0));

        var endpoint = Factory.Create<ListUsersEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(new ListUsersRequest { Page = 1, PageSize = 20 }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Items.ShouldBeEmpty();
        endpoint.Response.TotalItems.ShouldBe(0);
    }

    [Fact(DisplayName = "HandleAsync should pass page and pageSize from request to query")]
    public async Task HandleAsync_ShouldPassPageAndPageSize_FromRequestToQuery()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        ListUsersQuery? capturedQuery = null;

        var queryHandlerMock = new Mock<IQueryHandler<ListUsersQuery, PagedResult<UserSummaryDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(It.IsAny<ListUsersQuery>(), ct))
            .Callback<ListUsersQuery, CancellationToken>((q, _) => capturedQuery = q)
            .ReturnsAsync(new PagedResult<UserSummaryDto>([], 0));

        var endpoint = Factory.Create<ListUsersEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(new ListUsersRequest { Page = 2, PageSize = 5 }, ct);

        // Assert
        capturedQuery.ShouldNotBeNull();
        capturedQuery.Page.ShouldBe(2);
        capturedQuery.PageSize.ShouldBe(5);
        endpoint.Response.PageNumber.ShouldBe(2);
        endpoint.Response.PageSize.ShouldBe(5);
    }

    [Fact(DisplayName = "Configure should register authenticated GET route containing 'users'")]
    public void Configure_ShouldRegisterAuthenticatedGetRoute_ContainingUsers()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<ListUsersQuery, PagedResult<UserSummaryDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<ListUsersEndpoint>(queryHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("users"), "should include a 'users' route");
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }
}
