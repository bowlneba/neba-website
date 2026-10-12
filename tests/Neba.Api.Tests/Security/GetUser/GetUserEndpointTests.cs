using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.Security.GetUser;
using Neba.Api.Messaging;
using Neba.Api.Security.GetUser;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.GetUser;

[UnitTest]
[Component("Security")]
public sealed class GetUserEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with the mapped user when the query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedUser_WhenQuerySucceeds()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var dto = UserDetailDtoFactory.Create();

        var queryHandlerMock = new Mock<IQueryHandler<GetUserQuery, ErrorOr<UserDetailDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.Is<GetUserQuery>(q => q.UserId == dto.UserId), ct))
            .ReturnsAsync(dto);

        var endpoint = Factory.Create<GetUserEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(new GetUserRequest { UserId = dto.UserId.ToString() }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.UserId.ShouldBe(dto.UserId.ToString());
        endpoint.Response.Email.ShouldBe(dto.Email);
        endpoint.Response.EmailConfirmed.ShouldBe(dto.EmailConfirmed);
        endpoint.Response.Roles.ShouldBe(dto.Roles);
        endpoint.Response.AddedPermissions.ShouldBe(dto.AddedPermissions);
        endpoint.Response.RemovedPermissions.ShouldBe(dto.RemovedPermissions);
    }

    [Fact(DisplayName = "HandleAsync should return 404 when the user does not exist")]
    public async Task HandleAsync_ShouldReturn404_WhenUserNotFound()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<GetUserQuery, ErrorOr<UserDetailDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetUserQuery>(), ct))
            .ReturnsAsync(GetUserErrors.UserNotFound);

        var endpoint = Factory.Create<GetUserEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(new GetUserRequest { UserId = Ulid.NewUlid().ToString() }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact(DisplayName = "Configure should register a permission-protected GET route under /security")]
    public void Configure_ShouldRegisterPermissionProtectedGetRoute()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<GetUserQuery, ErrorOr<UserDetailDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<GetUserEndpoint>(queryHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("users/{userId}"));
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("security"));
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }
}