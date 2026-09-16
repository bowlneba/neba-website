using ErrorOr;

using FastEndpoints;

using Microsoft.Extensions.Hosting;

using Neba.Api.Contracts.Security;
using Neba.Api.Features.Cache.ClearCache;
using Neba.TestFactory.Attributes;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Cache.ClearCache;

[UnitTest]
[Component("Cache")]
public sealed class ClearCacheEndpointTests
{
    private static Mock<IHostEnvironment> CreateEnvironmentMock(string environmentName)
    {
        var mock = new Mock<IHostEnvironment>(MockBehavior.Strict);
        mock.SetupGet(e => e.EnvironmentName).Returns(environmentName);
        return mock;
    }

    [Fact(DisplayName = "HandleAsync should return 204 NoContent when the cache is cleared")]
    public async Task HandleAsync_ShouldReturn204_WhenCacheIsCleared()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<ClearCacheCommand>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<ClearCacheCommand>(), ct))
            .ReturnsAsync(Result.Success);

        var endpoint = Factory.Create<ClearCacheEndpoint>(
            commandHandlerMock.Object,
            CreateEnvironmentMock(Environments.Production).Object);

        // Act
        await endpoint.HandleAsync(ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
    }

    [Fact(DisplayName = "Configure should require the Cache.Clear permission when not running in Development")]
    public void Configure_ShouldRequireClearCachePermission_WhenNotDevelopment()
    {
        // Arrange
        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<ClearCacheCommand>>(MockBehavior.Strict);
        var endpoint = Factory.Create<ClearCacheEndpoint>(
            commandHandlerMock.Object,
            CreateEnvironmentMock(Environments.Production).Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("DELETE");
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
        endpoint.Definition.PreBuiltUserPolicies.ShouldNotBeNull();
        endpoint.Definition.PreBuiltUserPolicies.ShouldContain(Permissions.ClearCache.PolicyName);
    }

    [Fact(DisplayName = "Configure should allow anonymous access when running in Development")]
    public void Configure_ShouldAllowAnonymousAccess_WhenDevelopment()
    {
        // Arrange
        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<ClearCacheCommand>>(MockBehavior.Strict);
        var endpoint = Factory.Create<ClearCacheEndpoint>(
            commandHandlerMock.Object,
            CreateEnvironmentMock(Environments.Development).Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("DELETE");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeNull();
        endpoint.Definition.AnonymousVerbs.ShouldContain("DELETE");
    }
}