using ErrorOr;

using FastEndpoints;

using Microsoft.FeatureManagement;

using Neba.Api.Contracts.FeatureManagement;
using Neba.Api.Contracts.Security.Register;
using Neba.Api.Security.Register;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Security.Register;

[UnitTest]
[Component("Security")]
public sealed class RegisterEndpointTests
{
    [Fact(DisplayName = "HandleAsync should map request fields to command and take the success branch when registration succeeds")]
    public async Task HandleAsync_ShouldMapRequestToCommandAndTakeSuccessBranch_WhenRegistrationSucceeds()
    {
        // Arrange
        var userId = Ulid.NewUlid();
        var request = RegisterRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<RegisterCommand, Ulid>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(
                It.Is<RegisterCommand>(c => c.Email == request.Input.Email && c.Password == request.Input.Password),
                ct))
            .ReturnsAsync(userId);

        var featureManagerMock = CreateFeatureManagerMock(isEnabled: true);

        var endpoint = Factory.Create<RegisterEndpoint>(commandHandlerMock.Object, featureManagerMock.Object);

        // Act — Send.CreatedAtAsync requires LinkGenerator, which Factory.Create does not provide.
        // The strict mock verifies the command mapping; the LinkGenerator exception confirms the success branch was taken.
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => endpoint.HandleAsync(request, ct));

        // Assert
        exception.Message.ShouldContain("LinkGenerator");
    }

    [Fact(DisplayName = "HandleAsync should return 409 Conflict when email is already registered")]
    public async Task HandleAsync_ShouldReturn409_WhenEmailAlreadyRegistered()
    {
        // Arrange
        var request = RegisterRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<RegisterCommand, Ulid>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<RegisterCommand>(), ct))
            .ReturnsAsync(RegisterErrors.DuplicateEmail);

        var featureManagerMock = CreateFeatureManagerMock(isEnabled: true);

        var endpoint = Factory.Create<RegisterEndpoint>(commandHandlerMock.Object, featureManagerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(409);
    }

    [Fact(DisplayName = "HandleAsync should return 422 when command returns validation errors")]
    public async Task HandleAsync_ShouldReturn422_WhenCommandReturnsValidationErrors()
    {
        // Arrange
        var request = RegisterRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<RegisterCommand, Ulid>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<RegisterCommand>(), ct))
            .ReturnsAsync(Error.Validation("Register.WeakPassword", "Password does not meet requirements."));

        var featureManagerMock = CreateFeatureManagerMock(isEnabled: true);

        var endpoint = Factory.Create<RegisterEndpoint>(commandHandlerMock.Object, featureManagerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(422);
    }

    [Fact(DisplayName = "HandleAsync should return 404 and not invoke the command handler when the UserRegistration feature is disabled")]
    public async Task HandleAsync_ShouldReturn404AndNotInvokeCommandHandler_WhenFeatureIsDisabled()
    {
        // Arrange
        var request = RegisterRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<RegisterCommand, Ulid>>(MockBehavior.Strict);

        var featureManagerMock = CreateFeatureManagerMock(isEnabled: false);

        var endpoint = Factory.Create<RegisterEndpoint>(commandHandlerMock.Object, featureManagerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert — strict command handler mock has no setups, so any invocation would throw
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact(DisplayName = "Configure should register anonymous POST route containing 'register'")]
    public void Configure_ShouldRegisterAnonymousPostRoute_ContainingRegister()
    {
        // Arrange
        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<RegisterCommand, Ulid>>(MockBehavior.Strict);
        var featureManagerMock = CreateFeatureManagerMock(isEnabled: true);
        var endpoint = Factory.Create<RegisterEndpoint>(commandHandlerMock.Object, featureManagerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("POST");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("register"), "should include a 'register' route");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }

    private static Mock<IFeatureManager> CreateFeatureManagerMock(bool isEnabled)
    {
        var featureManagerMock = new Mock<IFeatureManager>(MockBehavior.Strict);
        featureManagerMock
            .Setup(f => f.IsEnabledAsync(FeatureFlags.UserRegistration, It.IsAny<AllowedEmailContext>()))
            .ReturnsAsync(isEnabled);

        return featureManagerMock;
    }
}