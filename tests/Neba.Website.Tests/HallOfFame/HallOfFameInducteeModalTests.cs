using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.HallOfFame;
using Neba.Api.Contracts.HallOfFame.GetHallOfFamePointsDetail;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.HallOfFame;
using Neba.Website.Server.Clock;
using Neba.Website.Server.HallOfFame;
using Neba.Website.Server.Services;

using Refit.Testing;

namespace Neba.Website.Tests.HallOfFame;

[UnitTest]
[Component("Website.HallOfFame.HallOfFameInducteeModal")]
public sealed class HallOfFameInducteeModalTests : IDisposable
{
    private const string BowlerId = HallOfFameInductionViewModelFactory.ValidBowlerId;

    private readonly BunitContext _ctx;
    private readonly Mock<IHallOfFameApi> _mockApi;

    public HallOfFameInducteeModalTests()
    {
        _mockApi = new Mock<IHallOfFameApi>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        _ctx.Services.AddSingleton(_mockApi.Object);
        _ctx.Services.AddSingleton(sp => new ApiExecutor(mockStopwatch.Object, sp.GetRequiredService<NavigationManager>(), NullLogger<ApiExecutor>.Instance));
    }

    public void Dispose()
        => _ctx.Dispose();

    private IRenderedComponent<HallOfFameInducteeModal> RenderModal(HallOfFameInductionViewModel? inductee, bool isOpen = true)
        => _ctx.Render<HallOfFameInducteeModal>(p => p
            .Add(x => x.IsOpen, isOpen)
            .Add(x => x.Inductee, inductee)
            .Add(x => x.OnClose, EventCallback.Empty));

    [Fact(DisplayName = "Renders nothing while closed")]
    public void Render_ShouldRenderNothing_WhenClosed()
    {
        // Arrange
        // Act
        var cut = RenderModal(inductee: null, isOpen: false);

        // Assert
        // The ip-root wrapper (scope anchor for ::deep) renders empty; no modal markup inside it.
        cut.Find(".ip-root").InnerHtml.Trim().ShouldBeEmpty();
    }

    [Fact(DisplayName = "Header shows name, class year, categories, and initials when there is no photo")]
    public void Render_ShouldShowHeader_WhenOpen()
    {
        // Arrange
        SetupDetail(HallOfFamePointsDetailResponseFactory.Create());
        var inductee = HallOfFameInductionViewModelFactory.Create(
            bowlerName: "Jane Smith",
            inductionYear: 2024,
            categories: [HallOfFameCategory.SuperiorPerformance, HallOfFameCategory.MeritoriousService]);

        // Act
        var cut = RenderModal(inductee);

        // Assert
        cut.Find(".ip-name").TextContent.ShouldBe("Jane Smith");
        cut.Find(".ip-class").TextContent.ShouldBe("Class of 2024");
        cut.FindAll(".ip-chips li").Select(li => li.TextContent).ShouldBe(["Superior Performance", "Meritorious Service"]);
        cut.Find(".inductee-photo").TextContent.Trim().ShouldBe("JS");
        cut.Find("img.ip-logo").GetAttribute("alt").ShouldBe("NEBA Hall of Fame");
    }

    [Fact(DisplayName = "Header shows the photo when the inductee has one")]
    public void Render_ShouldShowPhoto_WhenInducteeHasPhoto()
    {
        // Arrange
        SetupDetail(HallOfFamePointsDetailResponseFactory.Create());
        var inductee = HallOfFameInductionViewModelFactory.Create(photoUri: new Uri("https://files.example.test/jane.jpg"));

        // Act
        var cut = RenderModal(inductee);

        // Assert
        cut.Find("img.inductee-photo__img").GetAttribute("src").ShouldBe("https://files.example.test/jane.jpg");
    }

    [Fact(DisplayName = "Loads the points by the inductee's bowler id")]
    public void Render_ShouldLoadPoints_ByBowlerId()
    {
        // Arrange
        SetupDetail(HallOfFamePointsDetailResponseFactory.Create());

        // Act
        RenderModal(HallOfFameInductionViewModelFactory.Create());

        // Assert
        _mockApi.Verify(x => x.GetHallOfFamePointsDetailAsync(BowlerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Shows the points as a plain list with no threshold row and no 'of 36' text")]
    public void Render_ShouldShowProfileLayout_WhenDataLoaded()
    {
        // Arrange
        SetupDetail(HallOfFamePointsDetailResponseFactory.Create());

        // Act
        var cut = RenderModal(HallOfFameInductionViewModelFactory.Create());

        // Assert
        cut.FindAll(".pm-table").ShouldBeEmpty();
        cut.FindAll(".pm-reached").ShouldBeEmpty();
        cut.FindAll(".pm-stat").Count.ShouldBe(3);
        cut.Markup.ShouldNotContain(" of 36");
    }

    [Fact(DisplayName = "Shows the empty message for an inductee with no points and no membership years")]
    public void Render_ShouldShowEmptyMessage_WhenNoData()
    {
        // Arrange
        SetupDetail(HallOfFamePointsDetailResponseFactory.Create(
            totalPoints: 0, seasonsWithPoints: 0, breakdown: [], membershipYears: []));

        // Act
        var cut = RenderModal(HallOfFameInductionViewModelFactory.Create(categories: [HallOfFameCategory.MeritoriousService]));

        // Assert
        cut.Find(".ip-empty").TextContent.ShouldContain("not shown yet");
        cut.FindAll(".pm-stat").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Shows a neutral empty message for a Superior Performance inductee with no data")]
    public void Render_ShouldShowNeutralEmptyMessage_WhenSuperiorInducteeHasNoData()
    {
        // Arrange
        SetupDetail(HallOfFamePointsDetailResponseFactory.Create(
            totalPoints: 0, seasonsWithPoints: 0, breakdown: [], membershipYears: []));

        // Act
        var cut = RenderModal(HallOfFameInductionViewModelFactory.Create(categories: [HallOfFameCategory.SuperiorPerformance]));

        // Assert
        var message = cut.Find(".ip-empty").TextContent;
        message.ShouldContain("No points or membership details are on file");
        message.ShouldNotContain("Meritorious Service");
    }

    [Fact(DisplayName = "Shows the data for a Meritorious Service inductee who has points")]
    public void Render_ShouldShowData_WhenMeritoriousInducteeHasPoints()
    {
        // Arrange
        SetupDetail(HallOfFamePointsDetailResponseFactory.Create());

        // Act
        var cut = RenderModal(HallOfFameInductionViewModelFactory.Create(categories: [HallOfFameCategory.MeritoriousService]));

        // Assert
        cut.FindAll(".ip-empty").ShouldBeEmpty();
        cut.FindAll(".pm-stat").Count.ShouldBe(3);
    }

    private void SetupDetail(HallOfFamePointsDetailResponse detail)
    {
        using var response = new StubApiResponse<HallOfFamePointsDetailResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = detail
        };

        _mockApi
            .Setup(x => x.GetHallOfFamePointsDetailAsync(BowlerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }
}