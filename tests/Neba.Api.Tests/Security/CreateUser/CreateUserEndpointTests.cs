using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.Security.CreateUser;
using Neba.Api.Security.CreateUser;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Security.CreateUser;

[UnitTest]
[Component("Security")]
public sealed class CreateUserEndpointTests
{
    [Fact(DisplayName = "HandleAsync should map request fields to command and take the success branch when creation succeeds")]
    public async Task HandleAsync_ShouldMapRequestToCommandAndTakeSuccessBranch_WhenCreationSucceeds()
    {
        // Arrange
        var userId = Ulid.NewUlid();
        var claim = ClaimInputFactory.Create();
        var request = CreateUserRequestFactory.Create(
            roles: ["Webmaster", "Journalist"],
            usbcId: "12345",
            phoneNumber: "555-123-4567",
            claims: [claim]);
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateUserCommand, Ulid>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(
                It.Is<CreateUserCommand>(c =>
                    c.Email == request.User.Email
                    && c.Roles.SequenceEqual(request.User.Roles)
                    && c.UsbcId == request.User.UsbcId
                    && c.PhoneNumber == request.User.PhoneNumber
                    && c.Claims.Single().Type == claim.Type
                    && c.Claims.Single().Value == claim.Value),
                ct))
            .ReturnsAsync(userId);

        var endpoint = Factory.Create<CreateUserEndpoint>(commandHandlerMock.Object);

        // Act — Send.CreatedAtAsync requires LinkGenerator, which Factory.Create does not provide.
        // The strict mock verifies the command mapping; the LinkGenerator exception confirms the success branch was taken.
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => endpoint.HandleAsync(request, ct));

        // Assert
        exception.Message.ShouldContain("LinkGenerator");
    }

    [Fact(DisplayName = "HandleAsync should return 409 Conflict when the email is already registered")]
    public async Task HandleAsync_ShouldReturn409_WhenEmailAlreadyRegistered()
    {
        // Arrange
        var request = CreateUserRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateUserCommand, Ulid>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateUserCommand>(), ct))
            .ReturnsAsync(CreateUserErrors.DuplicateEmail);

        var endpoint = Factory.Create<CreateUserEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(409);
    }

    [Fact(DisplayName = "HandleAsync should return 422 when the command returns validation errors")]
    public async Task HandleAsync_ShouldReturn422_WhenCommandReturnsValidationErrors()
    {
        // Arrange
        var request = CreateUserRequestFactory.Create();
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateUserCommand, Ulid>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateUserCommand>(), ct))
            .ReturnsAsync(Error.Validation("CreateUser.InvalidEmail", "Email is invalid."));

        var endpoint = Factory.Create<CreateUserEndpoint>(commandHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(422);
    }

    [Fact(DisplayName = "Configure should register a permission-protected POST route under /security")]
    public void Configure_ShouldRegisterPermissionProtectedPostRoute_UnderSecurityPath()
    {
        // Arrange
        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateUserCommand, Ulid>>(MockBehavior.Strict);
        var endpoint = Factory.Create<CreateUserEndpoint>(commandHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("POST");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("users"), "should register the users route");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("security"), "should be under the /security path");
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }
}