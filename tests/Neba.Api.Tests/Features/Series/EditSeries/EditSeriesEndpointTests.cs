using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.Series.EditSeries;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.EditSeries;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Series.EditSeries;

[UnitTest]
[Component("Series")]
public sealed class EditSeriesEndpointTests
{
    private static readonly string Id = new('3', 26);

    private static Action<Microsoft.AspNetCore.Http.HttpContext> AsCaller(Ulid? callerId = null)
        => ctx => ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(JwtRegisteredClaimNames.Sub, (callerId ?? Ulid.NewUlid()).ToString())], "test"));

    private static Mock<NebaMessaging.ICommandHandler<EditSeriesCommand, Updated>> Handler()
        => new(MockBehavior.Strict);

    private static EditSeriesRequest Request()
        => new()
        {
            Id = Id,
            Series = new EditSeriesInput
            {
                Name = "Renamed",
                Logo = SeriesLogoInputFactory.Create(),
                Stages = [SeriesStageInputFactory.Create(tournamentId: new string('1', 26))],
                Finale = SeriesFinaleInputFactory.Create(),
            },
        };

    [Fact(DisplayName = "Configure should register a permission-protected PUT route at /series/{id}")]
    public void Configure_ShouldRegisterPermissionProtectedPutRoute()
    {
        // Arrange & Act
        var endpoint = Factory.Create<EditSeriesEndpoint>(Handler().Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("PUT");
        endpoint.Definition.Routes.ShouldContain("/series/{id}");
        endpoint.Definition.PreBuiltUserPolicies!.ShouldContain("Permission:Series.EditSeries");
    }

    [Fact(DisplayName = "HandleAsync should map the request and caller to the command and return 204")]
    public async Task HandleAsync_ShouldMapCommand_AndReturn204()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var caller = Ulid.NewUlid();
        EditSeriesCommand? captured = null;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<EditSeriesCommand>(), ct))
            .Callback<EditSeriesCommand, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync(Result.Updated);
        var endpoint = Factory.Create<EditSeriesEndpoint>(AsCaller(caller), handler.Object);

        // Act
        await endpoint.HandleAsync(Request(), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        captured!.SeriesId.ShouldBe(new SeriesId(Id));
        captured.CallerUserId.ShouldBe(caller);
        captured.Name.ShouldBe("Renamed");
        captured.Logo!.Container.ShouldBe(SeriesLogoInputFactory.ValidContainer);
        captured.Stages.Count.ShouldBe(1);
        captured.Finale!.FinalistCount.ShouldBe(4);
    }

    [Fact(DisplayName = "HandleAsync should return 401 when the caller has no user id")]
    public async Task HandleAsync_ShouldReturn401_WhenNoCaller()
    {
        // Arrange
        var endpoint = Factory.Create<EditSeriesEndpoint>(Handler().Object);

        // Act
        await endpoint.HandleAsync(Request(), TestContext.Current.CancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(401);
    }

    [Theory(DisplayName = "HandleAsync should map NotFound, Forbidden, Validation and Conflict errors to 404, 403, 422 and 409")]
    [InlineData("NotFound", 404)]
    [InlineData("Forbidden", 403)]
    [InlineData("Validation", 422)]
    [InlineData("Conflict", 409)]
    public async Task HandleAsync_ShouldMapErrorTypesToStatusCodes(string type, int expected)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        Error error = type switch
        {
            "NotFound" => SeriesErrors.NotFound(new SeriesId(Id)),
            "Forbidden" => SeriesErrors.LogoChangeDenied,
            "Validation" => SeriesErrors.NameRequired,
            _ => SeriesErrors.Locked,
        };
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<EditSeriesCommand>(), ct)).ReturnsAsync(error);
        var endpoint = Factory.Create<EditSeriesEndpoint>(AsCaller(), handler.Object);

        // Act
        await endpoint.HandleAsync(Request(), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(expected);
    }
}