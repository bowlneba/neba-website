using FastEndpoints;

using Neba.Api.Messaging;
using Neba.Api.Security.ListRoles;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

namespace Neba.Api.Tests.Security.ListRoles;

[UnitTest]
[Component("Security")]
public sealed class ListRolesEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with each role and its permissions")]
    public async Task HandleAsync_ShouldReturnOkWithMappedRoles()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        IReadOnlyCollection<RoleDto> roles =
        [
            RoleDtoFactory.Create(name: "Journalist", permissions: ["News.CreateArticle"]),
            RoleDtoFactory.Create(name: "Member", permissions: [])
        ];

        var queryHandlerMock = new Mock<IQueryHandler<ListRolesQuery, IReadOnlyCollection<RoleDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<ListRolesQuery>(), ct))
            .ReturnsAsync(roles);

        var endpoint = Factory.Create<ListRolesEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Roles.Count.ShouldBe(2);
        endpoint.Response.Roles.First().Name.ShouldBe("Journalist");
        endpoint.Response.Roles.First().Permissions.ShouldBe(["News.CreateArticle"]);
        endpoint.Response.Roles.Last().Permissions.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Configure should register a permission-protected GET route under /security")]
    public void Configure_ShouldRegisterPermissionProtectedGetRoute()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<ListRolesQuery, IReadOnlyCollection<RoleDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<ListRolesEndpoint>(queryHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("roles"));
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("security"));
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }
}
