using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts.Series.EditSeriesLogo;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.EditSeriesLogo;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Series.EditSeriesLogo;

[UnitTest]
[Component("Series")]
public sealed class EditSeriesLogoEndpointTests
{
    private static readonly string Id = new('3', 26);

    private static Mock<NebaMessaging.ICommandHandler<EditSeriesLogoCommand, Updated>> Handler()
        => new(MockBehavior.Strict);

    private static EditSeriesLogoRequest Request(bool remove = false)
        => new() { Id = Id, Input = EditSeriesLogoInputFactory.Create(removeLogo: remove) };

    [Fact(DisplayName = "Configure should register a permission-protected PUT route at /series/{id}/logo")]
    public void Configure_ShouldRegisterPermissionProtectedPutRoute()
    {
        // Arrange & Act
        var endpoint = Factory.Create<EditSeriesLogoEndpoint>(Handler().Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("PUT");
        endpoint.Definition.Routes.ShouldContain("/series/{id}/logo");
        endpoint.Definition.PreBuiltUserPolicies!.ShouldContain("Permission:Series.EditSeriesLogo");
    }

    [Theory(DisplayName = "HandleAsync should map the logo (or its removal) to the command and return 204")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HandleAsync_ShouldMapCommand_AndReturn204(bool remove)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        EditSeriesLogoCommand? captured = null;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<EditSeriesLogoCommand>(), ct))
            .Callback<EditSeriesLogoCommand, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync(Result.Updated);
        var endpoint = Factory.Create<EditSeriesLogoEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(Request(remove), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        captured!.SeriesId.ShouldBe(new SeriesId(Id));
        (captured.Logo is null).ShouldBe(remove);
        if (!remove)
        {
            captured.Logo!.Container.ShouldBe(SeriesLogoInputFactory.ValidContainer);
        }
    }

    [Theory(DisplayName = "HandleAsync should map NotFound and Validation errors to 404 and 422")]
    [InlineData("NotFound", 404)]
    [InlineData("Validation", 422)]
    public async Task HandleAsync_ShouldMapErrorTypes(string type, int expected)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        Error error = type == "NotFound" ? SeriesErrors.NotFound(new SeriesId(Id)) : SeriesErrors.LogoNotUploaded;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<EditSeriesLogoCommand>(), ct)).ReturnsAsync(error);
        var endpoint = Factory.Create<EditSeriesLogoEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(Request(), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(expected);
    }
}