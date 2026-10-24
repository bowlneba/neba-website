using Bunit;
using Bunit.TestDoubles;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts;
using Neba.Api.Contracts.HallOfFame;
using Neba.Api.Contracts.HallOfFame.ListHallOfFameInductions;
using Neba.Api.Contracts.Security;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.HallOfFame;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Notifications;
using Neba.Website.Server.Services;

using Refit;
using Refit.Testing;

using HallOfFamePage = Neba.Website.Server.HallOfFame.HallOfFame;

namespace Neba.Website.Tests.HallOfFame;

[UnitTest]
[Component("Website.HallOfFame.HallOfFame")]
public sealed class HallOfFameTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<IHallOfFameApi> _mockApi;
    private readonly BunitAuthorizationContext _authContext;
    private readonly ToastService _toastService = new();

    public HallOfFameTests()
    {
        _mockApi = new Mock<IHallOfFameApi>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        _authContext = _ctx.AddAuthorization();
        _ctx.Services.AddSingleton(_toastService);
        _ctx.Services.AddSingleton(_mockApi.Object);
        _ctx.Services.AddSingleton(sp => new ApiExecutor(mockStopwatch.Object, sp.GetRequiredService<NavigationManager>(), NullLogger<ApiExecutor>.Instance));
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _toastService.Dispose();
    }

    [Fact(DisplayName = "Should render page title")]
    public void Render_ShouldShowPageTitle_WhenRendered()
    {
        // Arrange
        SetupSuccessResponse([HallOfFameInductionResponseFactory.Create()]);

        // Act
        var cut = _ctx.Render<HallOfFamePage>();

        // Assert
        cut.Markup.ShouldContain("Hall of Fame");
    }

    [Fact(DisplayName = "Should call ListHallOfFameInductionsAsync on initialization")]
    public void OnInit_ShouldCallListHallOfFameInductionsApi()
    {
        // Arrange
        SetupSuccessResponse([]);

        // Act
        _ctx.Render<HallOfFamePage>();

        // Assert
        _mockApi.Verify(
            x => x.ListHallOfFameInductionsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Should show bowler name when API call succeeds")]
    public void Render_ShouldShowBowlerName_WhenApiSucceeds()
    {
        // Arrange
        var induction = HallOfFameInductionResponseFactory.Create(bowlerName: "Jane Smith");
        SetupSuccessResponse([induction]);

        // Act
        var cut = _ctx.Render<HallOfFamePage>();

        // Assert
        cut.Markup.ShouldContain("Jane Smith");
    }

    [Fact(DisplayName = "Should show Class of year header when API call succeeds")]
    public void Render_ShouldShowClassOfYearHeader_WhenApiSucceeds()
    {
        // Arrange
        var induction = HallOfFameInductionResponseFactory.Create(year: 2019);
        SetupSuccessResponse([induction]);

        // Act
        var cut = _ctx.Render<HallOfFamePage>();

        // Assert
        cut.Markup.ShouldContain("Class of 2019");
    }

    [Fact(DisplayName = "Should show category name for single-category inductee")]
    public void Render_ShouldShowCategoryName_WhenSingleCategoryInductee()
    {
        // Arrange
        var induction = HallOfFameInductionResponseFactory.Create(
            categories: [HallOfFameCategory.SuperiorPerformance.Name]);
        SetupSuccessResponse([induction]);

        // Act
        var cut = _ctx.Render<HallOfFamePage>();

        // Assert
        cut.Markup.ShouldContain(HallOfFameCategory.SuperiorPerformance.Name);
    }

    [Fact(DisplayName = "Should show Combined section for multi-category inductee")]
    public void Render_ShouldShowCombinedSection_WhenMultiCategoryInductee()
    {
        // Default factory creates inductees with two categories
        // Arrange
        var induction = HallOfFameInductionResponseFactory.Create();
        SetupSuccessResponse([induction]);

        // Act
        var cut = _ctx.Render<HallOfFamePage>();

        // Assert
        cut.Markup.ShouldContain("Combined");
    }

    [Fact(DisplayName = "Should display newest year before older year")]
    public void Render_ShouldOrderYearsDescending_WhenMultipleYears()
    {
        // Arrange
        SetupSuccessResponse([
            HallOfFameInductionResponseFactory.Create(year: 2010, bowlerName: "Old Timer",
                categories: [HallOfFameCategory.SuperiorPerformance.Name]),
            HallOfFameInductionResponseFactory.Create(year: 2023, bowlerName: "Recent Star",
                categories: [HallOfFameCategory.SuperiorPerformance.Name]),
        ]);

        // Act
        var cut = _ctx.Render<HallOfFamePage>();
        var markup = cut.Markup;

        // Assert
        markup.IndexOf("Class of 2023", StringComparison.Ordinal)
            .ShouldBeLessThan(markup.IndexOf("Class of 2010", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Should show error alert when API call fails")]
    public void Render_ShouldShowErrorAlert_WhenApiFails()
    {
        // Arrange
        SetupFailureResponse(System.Net.HttpStatusCode.InternalServerError);

        // Act
        var cut = _ctx.Render<HallOfFamePage>();

        // Assert
        cut.Markup.ShouldContain("Error Loading Hall of Fame");
    }

    [Fact(DisplayName = "Should show eligibility criteria section while loading")]
    public void Render_ShouldShowCriteriaSection_WhileLoading()
    {
        // Arrange — never resolve — page stays in loading state
        _mockApi
            .Setup(x => x.ListHallOfFameInductionsAsync(It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<IApiResponse<CollectionResponse<HallOfFameInductionResponse>>>().Task);

        // Act
        var cut = _ctx.Render<HallOfFamePage>();

        // Assert
        cut.Markup.ShouldContain("Eligibility");
        cut.Markup.ShouldContain("Superior Performance Category");
    }

    [Fact(DisplayName = "Should show eligibility criteria section after data loads")]
    public void Render_ShouldShowCriteriaSection_WhenApiSucceeds()
    {
        // Arrange
        SetupSuccessResponse([HallOfFameInductionResponseFactory.Create()]);

        // Act
        var cut = _ctx.Render<HallOfFamePage>();

        // Assert
        cut.Markup.ShouldContain("Eligibility");
        cut.Markup.ShouldContain("Superior Performance Category");
    }

    [Fact(DisplayName = "Should show eligibility criteria section when API fails")]
    public void Render_ShouldShowCriteriaSection_WhenApiFails()
    {
        // Arrange
        SetupFailureResponse(System.Net.HttpStatusCode.InternalServerError);

        // Act
        var cut = _ctx.Render<HallOfFamePage>();

        // Assert
        cut.Markup.ShouldContain("Eligibility");
        cut.Markup.ShouldContain("Superior Performance Category");
    }

    [Fact(DisplayName = "Should show initials when inductee has no photo URI")]
    public void Render_ShouldShowInitials_WhenNoPhotoUri()
    {
        // Arrange
        var induction = HallOfFameInductionResponseFactory.Create(
            bowlerName: "Jane Smith",
            categories: [HallOfFameCategory.SuperiorPerformance.Name],
            photoUri: null);
        SetupSuccessResponse([induction]);

        // Act
        var cut = _ctx.Render<HallOfFamePage>();

        // Assert
        cut.Markup.ShouldContain("JS");
    }

    [Fact(DisplayName = "Should render the photo with its saved focal point and zoom when the inductee has a photo")]
    public void Render_ShouldRenderPhotoWithFraming_WhenPhotoUriProvided()
    {
        // Arrange
        var induction = HallOfFameInductionResponseFactory.Create(
            categories: [HallOfFameCategory.SuperiorPerformance.Name],
            photoUri: new Uri("https://example.com/photo.jpg"),
            photoFocalX: 31,
            photoFocalY: 36,
            photoZoom: 150);
        SetupSuccessResponse([induction]);

        // Act
        var cut = _ctx.Render<HallOfFamePage>();

        // Assert
        var image = cut.Find("img.inductee-photo__img");
        image.GetAttribute("src").ShouldBe("https://example.com/photo.jpg");
        image.GetAttribute("style").ShouldBe("object-position:31% 36%;transform-origin:31% 36%;transform:scale(1.5)");
    }

    [Fact(DisplayName = "Should center the photo with no zoom when no framing is saved")]
    public void Render_ShouldCenterPhoto_WhenNoFramingSaved()
    {
        // Arrange
        var induction = HallOfFameInductionResponseFactory.Create(
            categories: [HallOfFameCategory.SuperiorPerformance.Name],
            photoUri: new Uri("https://example.com/photo.jpg"));
        SetupSuccessResponse([induction]);

        // Act
        var cut = _ctx.Render<HallOfFamePage>();

        // Assert
        cut.Find("img.inductee-photo__img").GetAttribute("style")
            .ShouldBe("object-position:50% 50%;transform-origin:50% 50%;transform:scale(1)");
    }

    [Fact(DisplayName = "Should render an avatar for single-category and combined inductees")]
    public void Render_ShouldRenderAvatar_ForSingleCategoryAndCombinedInductees()
    {
        // Arrange
        var single = HallOfFameInductionResponseFactory.Create(
            bowlerName: "Jane Smith",
            categories: [HallOfFameCategory.SuperiorPerformance.Name]);
        var combined = HallOfFameInductionResponseFactory.Create(
            bowlerName: "Tom Davis",
            categories:
            [
                HallOfFameCategory.SuperiorPerformance.Name,
                HallOfFameCategory.MeritoriousService.Name
            ]);
        SetupSuccessResponse([single, combined]);

        // Act
        var cut = _ctx.Render<HallOfFamePage>();

        // Assert
        cut.FindAll(".inductee-item .inductee-photo").Count.ShouldBe(2);
        cut.FindAll(".inductee-photo").Select(photo => photo.TextContent.Trim()).ShouldBe(["JS", "TD"], ignoreOrder: true);
    }

    [Fact(DisplayName = "Should not show edit-photo buttons to a user without the permission")]
    public void Render_ShouldHideEditButtons_WithoutPermission()
    {
        // Arrange
        _authContext.SetAuthorized("member");
        SetupSuccessResponse([HallOfFameInductionResponseFactory.Create()]);

        // Act
        var cut = _ctx.Render<HallOfFamePage>();

        // Assert
        cut.FindAll("button.hof-photo-edit").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should show an edit-photo button for every inductee to a user with the permission")]
    public void Render_ShouldShowEditButtons_WithPermission()
    {
        // Arrange
        _authContext.SetAuthorized("editor");
        _authContext.SetPolicies(Permissions.EditHallOfFamePhoto.PolicyName);
        var single = HallOfFameInductionResponseFactory.Create(
            bowlerName: "Jane Smith",
            categories: [HallOfFameCategory.SuperiorPerformance.Name]);
        var combined = HallOfFameInductionResponseFactory.Create(
            bowlerName: "Tom Davis",
            categories: [HallOfFameCategory.SuperiorPerformance.Name, HallOfFameCategory.MeritoriousService.Name]);
        SetupSuccessResponse([single, combined]);

        // Act
        var cut = _ctx.Render<HallOfFamePage>();

        // Assert
        cut.FindAll("button.hof-photo-edit").Select(button => button.GetAttribute("aria-label"))
            .ShouldBe(["Edit photo for Jane Smith", "Edit photo for Tom Davis"], ignoreOrder: true);
    }

    [Fact(DisplayName = "Should open the photo editor for the clicked inductee and close it on cancel")]
    public void Click_ShouldOpenPhotoEditorForInductee_AndCloseOnCancel()
    {
        // Arrange
        _authContext.SetAuthorized("editor");
        _authContext.SetPolicies(Permissions.EditHallOfFamePhoto.PolicyName);
        SetupSuccessResponse([HallOfFameInductionResponseFactory.Create(bowlerName: "Jane Smith")]);
        var cut = _ctx.Render<HallOfFamePage>();
        cut.FindAll(".ehp-subtitle").ShouldBeEmpty();

        // Act
        cut.Find("button.hof-photo-edit").Click();

        // Assert
        cut.Find(".ehp-subtitle").TextContent.ShouldBe("Jane Smith");

        // Act
        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Cancel").Click();

        // Assert
        cut.FindAll(".ehp-subtitle").ShouldBeEmpty();
    }

    private void SetupSuccessResponse(IReadOnlyCollection<HallOfFameInductionResponse> inductions)
    {
        using var response = new StubApiResponse<CollectionResponse<HallOfFameInductionResponse>>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new CollectionResponse<HallOfFameInductionResponse>
            {
                Items = inductions,
            }
        };

        _mockApi
            .Setup(x => x.ListHallOfFameInductionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupFailureResponse(System.Net.HttpStatusCode statusCode)
    {
        using var response = new StubApiResponse<CollectionResponse<HallOfFameInductionResponse>>
        {
            IsSuccessStatusCode = false,
            StatusCode = statusCode,
            Content = (CollectionResponse<HallOfFameInductionResponse>?)null
        };

        _mockApi
            .Setup(x => x.ListHallOfFameInductionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }
}