using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using ErrorOr;

using FastEndpoints;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.CreateStageOverride;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Series.CreateStageOverride;

[UnitTest]
[Component("Series")]
public sealed class CreateStageOverrideEndpointTests
{
    private static Action<Microsoft.AspNetCore.Http.HttpContext> AsCaller(Ulid? callerId = null)
        => ctx => ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(JwtRegisteredClaimNames.Sub, (callerId ?? Ulid.NewUlid()).ToString())], "test"));

    private static Mock<NebaMessaging.ICommandHandler<CreateStageOverrideCommand, CreatedStageOverride>> Handler()
        => new(MockBehavior.Strict);

    [Fact(DisplayName = "Configure should register a POST route at /series/{id}/stages/{stageId}/overrides that needs the Series.ManageAdvancement permission")]
    public void Configure_ShouldRegisterPermissionProtectedPostRoute()
    {
        // Arrange & Act
        var endpoint = Factory.Create<CreateStageOverrideEndpoint>(Handler().Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("POST");
        endpoint.Definition.Routes.ShouldContain("/series/{id}/stages/{stageId}/overrides");
        endpoint.Definition.PreBuiltUserPolicies!.ShouldContain("Permission:Series.ManageAdvancement");
        endpoint.Definition.PreBuiltUserPolicies!.Count.ShouldBe(1);
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should return 401 and call no handler when the caller has no user id")]
    public async Task HandleAsync_ShouldReturn401_WhenNoCaller()
    {
        // Arrange
        var endpoint = Factory.Create<CreateStageOverrideEndpoint>(Handler().Object);

        // Act
        await endpoint.HandleAsync(CreateStageOverrideRequestFactory.Create(), TestContext.Current.CancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(401);
    }

    // Send.CreatedAtAsync needs a LinkGenerator that Factory.Create does not provide, so the call throws once the
    // command is dispatched. The 201 body is covered by SeriesAdvancementWriteEndpointsTests over real HTTP.
    [Fact(DisplayName = "HandleAsync should map the request and caller to the command and reach the 201 branch")]
    public async Task HandleAsync_ShouldMapCommandAndTakeCreatedBranch_WhenOverrideIsSaved()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var caller = Ulid.Parse("01M7XPMPJMC55SRJYQBHJPK7M8", CultureInfo.InvariantCulture);
        var request = CreateStageOverrideRequestFactory.Bogus(1, 204).Single();
        CreateStageOverrideCommand? captured = null;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<CreateStageOverrideCommand>(), ct))
            .Callback<CreateStageOverrideCommand, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync(CreatedStageOverrideFactory.Bogus(1, 205).Single());
        var endpoint = Factory.Create<CreateStageOverrideEndpoint>(AsCaller(caller), handler.Object);

        // Act
        Task Act() => endpoint.HandleAsync(request, ct);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(Act);
        exception.Message.ShouldContain("LinkGenerator");
        captured.ShouldNotBeNull();
        captured.SeriesId.ShouldBe(new SeriesId(request.Id));
        captured.StageId.ShouldBe(new SeriesStageId(request.StageId));
        captured.BowlerId.ShouldBe(new BowlerId(request.StageOverride.BowlerId));
        captured.Kind.Value.ShouldBe(request.StageOverride.Kind);
        captured.Reason.ShouldBe(request.StageOverride.Reason);
        captured.Version.ShouldBe(request.StageOverride.Version);
        captured.CallerUserId.ShouldBe(caller);
        await Verify(new { captured, endpoint.Response });
    }

    [Theory(DisplayName = "HandleAsync should map Kind 1 to Add and Kind 2 to Clear")]
    [InlineData(1, "Add")]
    [InlineData(2, "Clear")]
    public async Task HandleAsync_ShouldMapKindValueToOverrideKind(int kind, string expected)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        CreateStageOverrideCommand? captured = null;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<CreateStageOverrideCommand>(), ct))
            .Callback<CreateStageOverrideCommand, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync(Error.Conflict("Series.Conflict", "Conflict."));
        var endpoint = Factory.Create<CreateStageOverrideEndpoint>(AsCaller(), handler.Object);
        var request = CreateStageOverrideRequestFactory.Create(stageOverride: CreateStageOverrideInputFactory.Create(kind: kind));

        // Act
        await endpoint.HandleAsync(request, ct);

        // Assert
        captured!.Kind.Name.ShouldBe(expected);
    }

    [Theory(DisplayName = "HandleAsync should map NotFound, Conflict and Validation errors to 404, 409 and 422")]
    [InlineData("NotFound", 404)]
    [InlineData("Conflict", 409)]
    [InlineData("Validation", 422)]
    public async Task HandleAsync_ShouldMapErrorTypesToStatusCodes(string type, int expected)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var error = type switch
        {
            "NotFound" => Error.NotFound("Series.NotFound", "Not found."),
            "Conflict" => Error.Conflict("Series.Conflict", "Conflict."),
            _ => Error.Validation("Series.Validation", "Invalid."),
        };
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<CreateStageOverrideCommand>(), ct)).ReturnsAsync(error);
        var endpoint = Factory.Create<CreateStageOverrideEndpoint>(AsCaller(), handler.Object);

        // Act
        await endpoint.HandleAsync(CreateStageOverrideRequestFactory.Create(), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(expected);
    }
}