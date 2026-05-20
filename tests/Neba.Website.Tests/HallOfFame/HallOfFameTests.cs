using Bunit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts;
using Neba.Api.Contracts.HallOfFame;
using Neba.Api.Contracts.HallOfFame.ListHallOfFameInductions;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.HallOfFame;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Services;

using Refit;

using HallOfFamePage = Neba.Website.Server.HallOfFame.HallOfFame;

namespace Neba.Website.Tests.HallOfFame;

[UnitTest]
[Component("Website.HallOfFame.HallOfFame")]
public sealed class HallOfFameTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<IHallOfFameApi> _mockApi;

    public HallOfFameTests()
    {
        _mockApi = new Mock<IHallOfFameApi>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        _ctx.Services.AddSingleton(_mockApi.Object);
        _ctx.Services.AddSingleton(new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance));
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render page title")]
    public void Render_ShouldShowPageTitle_WhenRendered()
    {
        SetupSuccessResponse([HallOfFameInductionResponseFactory.Create()]);

        var cut = _ctx.Render<HallOfFamePage>();

        cut.Markup.ShouldContain("Hall of Fame");
    }

    [Fact(DisplayName = "Should call ListHallOfFameInductionsAsync on initialization")]
    public void OnInit_ShouldCallListHallOfFameInductionsApi()
    {
        SetupSuccessResponse([]);

        _ctx.Render<HallOfFamePage>();

        _mockApi.Verify(
            x => x.ListHallOfFameInductionsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Should show bowler name when API call succeeds")]
    public void Render_ShouldShowBowlerName_WhenApiSucceeds()
    {
        var induction = HallOfFameInductionResponseFactory.Create(bowlerName: "Jane Smith");
        SetupSuccessResponse([induction]);

        var cut = _ctx.Render<HallOfFamePage>();

        cut.Markup.ShouldContain("Jane Smith");
    }

    [Fact(DisplayName = "Should show Class of year header when API call succeeds")]
    public void Render_ShouldShowClassOfYearHeader_WhenApiSucceeds()
    {
        var induction = HallOfFameInductionResponseFactory.Create(year: 2019);
        SetupSuccessResponse([induction]);

        var cut = _ctx.Render<HallOfFamePage>();

        cut.Markup.ShouldContain("Class of 2019");
    }

    [Fact(DisplayName = "Should show category name for single-category inductee")]
    public void Render_ShouldShowCategoryName_WhenSingleCategoryInductee()
    {
        var induction = HallOfFameInductionResponseFactory.Create(
            categories: [HallOfFameCategory.SuperiorPerformance.Name]);
        SetupSuccessResponse([induction]);

        var cut = _ctx.Render<HallOfFamePage>();

        cut.Markup.ShouldContain(HallOfFameCategory.SuperiorPerformance.Name);
    }

    [Fact(DisplayName = "Should show Combined section for multi-category inductee")]
    public void Render_ShouldShowCombinedSection_WhenMultiCategoryInductee()
    {
        // Default factory creates inductees with two categories
        var induction = HallOfFameInductionResponseFactory.Create();
        SetupSuccessResponse([induction]);

        var cut = _ctx.Render<HallOfFamePage>();

        cut.Markup.ShouldContain("Combined");
    }

    [Fact(DisplayName = "Should display newest year before older year")]
    public void Render_ShouldOrderYearsDescending_WhenMultipleYears()
    {
        SetupSuccessResponse([
            HallOfFameInductionResponseFactory.Create(year: 2010, bowlerName: "Old Timer",
                categories: [HallOfFameCategory.SuperiorPerformance.Name]),
            HallOfFameInductionResponseFactory.Create(year: 2023, bowlerName: "Recent Star",
                categories: [HallOfFameCategory.SuperiorPerformance.Name]),
        ]);

        var cut = _ctx.Render<HallOfFamePage>();
        var markup = cut.Markup;

        markup.IndexOf("Class of 2023", StringComparison.Ordinal)
            .ShouldBeLessThan(markup.IndexOf("Class of 2010", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Should show error alert when API call fails")]
    public void Render_ShouldShowErrorAlert_WhenApiFails()
    {
        SetupFailureResponse(System.Net.HttpStatusCode.InternalServerError);

        var cut = _ctx.Render<HallOfFamePage>();

        cut.Markup.ShouldContain("Error Loading Hall of Fame");
    }

    [Fact(DisplayName = "Should show eligibility criteria section while loading")]
    public void Render_ShouldShowCriteriaSection_WhileLoading()
    {
        // Never resolve — page stays in loading state
        _mockApi
            .Setup(x => x.ListHallOfFameInductionsAsync(It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<IApiResponse<CollectionResponse<HallOfFameInductionResponse>>>().Task);

        var cut = _ctx.Render<HallOfFamePage>();

        cut.Markup.ShouldContain("Eligibility");
        cut.Markup.ShouldContain("Superior Performance Category");
    }

    [Fact(DisplayName = "Should show eligibility criteria section after data loads")]
    public void Render_ShouldShowCriteriaSection_WhenApiSucceeds()
    {
        SetupSuccessResponse([HallOfFameInductionResponseFactory.Create()]);

        var cut = _ctx.Render<HallOfFamePage>();

        cut.Markup.ShouldContain("Eligibility");
        cut.Markup.ShouldContain("Superior Performance Category");
    }

    [Fact(DisplayName = "Should show eligibility criteria section when API fails")]
    public void Render_ShouldShowCriteriaSection_WhenApiFails()
    {
        SetupFailureResponse(System.Net.HttpStatusCode.InternalServerError);

        var cut = _ctx.Render<HallOfFamePage>();

        cut.Markup.ShouldContain("Eligibility");
        cut.Markup.ShouldContain("Superior Performance Category");
    }

    [Fact(DisplayName = "Should show initials when inductee has no photo URI")]
    public void Render_ShouldShowInitials_WhenNoPhotoUri()
    {
        var induction = HallOfFameInductionResponseFactory.Create(
            bowlerName: "Jane Smith",
            categories: [HallOfFameCategory.SuperiorPerformance.Name],
            photoUri: null);
        SetupSuccessResponse([induction]);

        var cut = _ctx.Render<HallOfFamePage>();

        cut.Markup.ShouldContain("JS");
    }

    [Fact(DisplayName = "Should apply background-image style when inductee has a photo URI")]
    public void Render_ShouldApplyPhotoStyle_WhenPhotoUriProvided()
    {
        var photoUri = new Uri("https://example.com/photo.jpg");
        var induction = HallOfFameInductionResponseFactory.Create(
            categories: [HallOfFameCategory.SuperiorPerformance.Name],
            photoUri: photoUri);
        SetupSuccessResponse([induction]);

        var cut = _ctx.Render<HallOfFamePage>();

        cut.Markup.ShouldContain("background-image");
        cut.Markup.ShouldContain("https://example.com/photo.jpg");
    }

    [Fact(DisplayName = "GetInitials should return first and last initials for two-part name")]
    public void GetInitials_ShouldReturnFirstAndLastInitials_ForTwoPartName()
    {
        HallOfFamePage.GetInitials("Jane Smith").ShouldBe("JS");
    }

    [Fact(DisplayName = "GetInitials should return first and last initials for multi-part name")]
    public void GetInitials_ShouldReturnFirstAndLastInitials_ForMultiPartName()
    {
        HallOfFamePage.GetInitials("Mary Jo Johnson").ShouldBe("MJ");
    }

    [Fact(DisplayName = "GetInitials should return single initial for one-part name")]
    public void GetInitials_ShouldReturnSingleInitial_ForOnePartName()
    {
        HallOfFamePage.GetInitials("Cher").ShouldBe("C");
    }

    [Fact(DisplayName = "GetInitials should return question mark for empty name")]
    public void GetInitials_ShouldReturnQuestionMark_ForEmptyName()
    {
        HallOfFamePage.GetInitials(string.Empty).ShouldBe("?");
    }

    private void SetupSuccessResponse(IReadOnlyCollection<HallOfFameInductionResponse> inductions)
    {
        var response = new Mock<IApiResponse<CollectionResponse<HallOfFameInductionResponse>>>(MockBehavior.Strict);
        response.Setup(r => r.IsSuccessStatusCode).Returns(true);
        response.Setup(r => r.StatusCode).Returns(System.Net.HttpStatusCode.OK);
        response.Setup(r => r.Content).Returns(new CollectionResponse<HallOfFameInductionResponse>
        {
            Items = inductions,
        });

        _mockApi
            .Setup(x => x.ListHallOfFameInductionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response.Object);
    }

    private void SetupFailureResponse(System.Net.HttpStatusCode statusCode)
    {
        var response = new Mock<IApiResponse<CollectionResponse<HallOfFameInductionResponse>>>(MockBehavior.Strict);
        response.Setup(r => r.IsSuccessStatusCode).Returns(false);
        response.Setup(r => r.StatusCode).Returns(statusCode);
        response.Setup(r => r.Content).Returns((CollectionResponse<HallOfFameInductionResponse>?)null);

        _mockApi
            .Setup(x => x.ListHallOfFameInductionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response.Object);
    }
}