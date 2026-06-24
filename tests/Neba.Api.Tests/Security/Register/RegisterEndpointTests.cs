using ErrorOr;

using FastEndpoints;

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
                It.Is<RegisterCommand>(c => c.Email == request.Email && c.Password == request.Password),
                ct))
            .ReturnsAsync(userId);

        var endpoint = Factory.Create<RegisterEndpoint>(commandHandlerMock.Object);

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

        var endpoint = Factory.Create<RegisterEndpoint>(commandHandlerMock.Object);

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

        var endpoint = Factory.Create<RegisterEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(422);
    }

    [Fact(DisplayName = "Configure should register anonymous POST route containing 'register'")]
    public void Configure_ShouldRegisterAnonymousPostRoute_ContainingRegister()
    {
        // Arrange
        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<RegisterCommand, Ulid>>(MockBehavior.Strict);
        var endpoint = Factory.Create<RegisterEndpoint>(commandHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("POST");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("register"), "should include a 'register' route");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }
}
