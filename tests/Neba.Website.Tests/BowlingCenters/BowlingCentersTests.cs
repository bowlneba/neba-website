using Bunit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

using Neba.Api.Contracts;
using Neba.Api.Contracts.BowlingCenters;
using Neba.Api.Contracts.BowlingCenters.ListBowlingCenters;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.BowlingCenters;
using Neba.TestFactory.Contact;
using Neba.Website.Server.BowlingCenters;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Maps;
using Neba.Website.Server.Services;

using Refit;

using BowlingCentersPage = Neba.Website.Server.BowlingCenters.BowlingCenters;

namespace Neba.Website.Tests.BowlingCenters;

[UnitTest]
[Component("Website.BowlingCenters.BowlingCenters")]
public sealed class BowlingCentersTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<IBowlingCentersApi> _mockApi;

    public BowlingCentersTests()
    {
        _mockApi = new Mock<IBowlingCentersApi>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        _ctx.Services.AddSingleton(_mockApi.Object);
        _ctx.Services.AddSingleton(new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance));
        _ctx.Services.AddSingleton(new AzureMapsSettings());
        _ctx.Services.AddSingleton<ILogger<NebaMap>>(NullLogger<NebaMap>.Instance);

        _ctx.ComponentFactories.AddStub<DirectionsModal>();
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render page title")]
    public void Render_ShouldShowPageTitle_WhenRendered()
    {
        SetupSuccessResponse([BowlingCenterSummaryResponseFactory.Create()]);

        var cut = _ctx.Render<BowlingCentersPage>();

        cut.Markup.ShouldContain("Bowling Centers");
    }

    [Fact(DisplayName = "Should call ListBowlingCentersAsync on initialization")]
    public void OnInit_ShouldCallListBowlingCentersApi()
    {
        SetupSuccessResponse([]);

        _ctx.Render<BowlingCentersPage>();

        _mockApi.Verify(
            x => x.ListBowlingCentersAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Should show center name when API call succeeds")]
    public void Render_ShouldShowCenterName_WhenApiSucceeds()
    {
        var center = BowlingCenterSummaryResponseFactory.Create(name: "Spare Time Lanes");
        SetupSuccessResponse([center]);

        var cut = _ctx.Render<BowlingCentersPage>();

        cut.Markup.ShouldContain("Spare Time Lanes");
    }

    [Fact(DisplayName = "Should show USBC certification number prefix on each center card")]
    public void Render_ShouldShowCertificationNumberWithPrefix_WhenApiSucceeds()
    {
        var center = BowlingCenterSummaryResponseFactory.Create(certificationNumber: "12345");
        SetupSuccessResponse([center]);

        var cut = _ctx.Render<BowlingCentersPage>();

        cut.Markup.ShouldContain("USBC Cert #12345");
    }

    [Fact(DisplayName = "Should show city and state on each center card")]
    public void Render_ShouldShowCityAndState_WhenApiSucceeds()
    {
        var center = BowlingCenterSummaryResponseFactory.Create();
        SetupSuccessResponse([center]);

        var cut = _ctx.Render<BowlingCentersPage>();

        cut.Markup.ShouldContain(center.City);
        cut.Markup.ShouldContain(center.State);
    }

    [Fact(DisplayName = "Should show state filter buttons when centers exist")]
    public void Render_ShouldShowStateFilterButtons_WhenCentersExist()
    {
        SetupSuccessResponse([BowlingCenterSummaryResponseFactory.Create()]);

        var cut = _ctx.Render<BowlingCentersPage>();

        cut.FindAll(".state-btn").Count.ShouldBeGreaterThan(0);
    }

    [Fact(DisplayName = "Should show search input when centers exist")]
    public void Render_ShouldShowSearchInput_WhenCentersExist()
    {
        SetupSuccessResponse([BowlingCenterSummaryResponseFactory.Create()]);

        var cut = _ctx.Render<BowlingCentersPage>();

        cut.Find("input[placeholder='Search centers...']").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should show error alert when API call fails")]
    public void Render_ShouldShowErrorAlert_WhenApiFails()
    {
        SetupFailureResponse(System.Net.HttpStatusCode.InternalServerError);

        var cut = _ctx.Render<BowlingCentersPage>();

        cut.Markup.ShouldContain("Error Loading Centers");
    }

    [Fact(DisplayName = "Should filter centers by selected state")]
    public async Task FilterByState_ShouldShowOnlyCentersMatchingState()
    {
        // Default address has state "CT"; override to "MA" for the first center
        var maCenter = BowlingCenterSummaryResponseFactory.Create(
            name: "MA Lanes",
            address: AddressDtoFactory.Create(region: "MA"));
        var ctCenter = BowlingCenterSummaryResponseFactory.Create(name: "CT Lanes");
        SetupSuccessResponse([maCenter, ctCenter]);

        var cut = _ctx.Render<BowlingCentersPage>();

        var ctButton = cut.FindAll(".state-btn").First(b => b.TextContent.Trim() == "CT");
        await cut.InvokeAsync(() => ctButton.Click());

        cut.Markup.ShouldContain("CT Lanes");
        cut.Markup.ShouldNotContain("MA Lanes");
    }

    [Fact(DisplayName = "Should render centers in alphabetical order by name")]
    public void Render_ShouldOrderCentersByName_WhenApiSucceeds()
    {
        SetupSuccessResponse([
            BowlingCenterSummaryResponseFactory.Create(name: "Zebra Lanes"),
            BowlingCenterSummaryResponseFactory.Create(name: "Alpha Lanes"),
            BowlingCenterSummaryResponseFactory.Create(name: "Middle Lanes"),
        ]);

        var cut = _ctx.Render<BowlingCentersPage>();
        var markup = cut.Markup;

        markup.IndexOf("Alpha Lanes", StringComparison.Ordinal)
            .ShouldBeLessThan(markup.IndexOf("Middle Lanes", StringComparison.Ordinal));
        markup.IndexOf("Middle Lanes", StringComparison.Ordinal)
            .ShouldBeLessThan(markup.IndexOf("Zebra Lanes", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "BuildCenterDescription should join street and city line with newline when no unit")]
    public void BuildCenterDescription_ShouldUseNewlineSeparator_WhenNoUnit()
    {
        var center = BowlingCenterSummaryViewModelFactory.Create(
            street: "1450 Elmwood Avenue",
            unit: null,
            city: "Cranston",
            state: "RI",
            postalCode: "02910-3847");

        var description = BowlingCentersPage.BuildCenterDescription(center);

        description.ShouldBe("1450 Elmwood Avenue\nCranston, RI 02910-3847");
    }

    [Fact(DisplayName = "BuildCenterDescription should place unit on its own line between street and city")]
    public void BuildCenterDescription_ShouldPlaceUnit_OnSeparateLine()
    {
        var center = BowlingCenterSummaryViewModelFactory.Create(
            street: "100 Main St",
            unit: "Suite 200",
            city: "Boston",
            state: "MA",
            postalCode: "02101");

        var description = BowlingCentersPage.BuildCenterDescription(center);

        description.ShouldBe("100 Main St\nSuite 200\nBoston, MA 02101");
    }

    [Fact(DisplayName = "BuildCenterDescription should not contain html br tags")]
    public void BuildCenterDescription_ShouldNotContainBrTags()
    {
        var center = BowlingCenterSummaryViewModelFactory.Create();

        var description = BowlingCentersPage.BuildCenterDescription(center);

        description.ShouldNotContain("<br");
    }

    [Fact(DisplayName = "Should filter centers by name search query")]
    public async Task SearchFilter_ShouldShowOnlyCentersMatchingName()
    {
        var matching = BowlingCenterSummaryResponseFactory.Create(name: "Spare Time Lanes");
        var nonMatching = BowlingCenterSummaryResponseFactory.Create(name: "Lucky Strike Bowl");
        SetupSuccessResponse([matching, nonMatching]);

        var cut = _ctx.Render<BowlingCentersPage>();
        await cut.InvokeAsync(() => cut.Find("input[placeholder='Search centers...']").Input("Spare"));

        cut.Markup.ShouldContain("Spare Time Lanes");
        cut.Markup.ShouldNotContain("Lucky Strike Bowl");
    }

    [Fact(DisplayName = "Should filter centers by city search query")]
    public async Task SearchFilter_ShouldShowOnlyCentersMatchingCity()
    {
        var bostonCenter = BowlingCenterSummaryResponseFactory.Create(
            name: "Fenway Bowl",
            address: AddressDtoFactory.Create(city: "Boston"));
        var hartfordCenter = BowlingCenterSummaryResponseFactory.Create(
            name: "Capitol Lanes",
            address: AddressDtoFactory.Create(city: "Hartford"));
        SetupSuccessResponse([bostonCenter, hartfordCenter]);

        var cut = _ctx.Render<BowlingCentersPage>();
        await cut.InvokeAsync(() => cut.Find("input[placeholder='Search centers...']").Input("Boston"));

        cut.Markup.ShouldContain("Fenway Bowl");
        cut.Markup.ShouldNotContain("Capitol Lanes");
    }

    [Fact(DisplayName = "Should change map style when satellite button is clicked")]
    public async Task HandleMapStyleChange_ShouldUpdateStyle_WhenSatelliteSelected()
    {
        SetupSuccessResponse([BowlingCenterSummaryResponseFactory.Create()]);

        var cut = _ctx.Render<BowlingCentersPage>();
        await cut.InvokeAsync(() => cut.FindAll("button").First(b => b.TextContent.Trim() == "Satellite").Click());

        var satelliteButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "Satellite");
        satelliteButton.ClassList.ShouldContain("bg-[var(--neba-blue-600)]");
    }

    [Fact(DisplayName = "Should invoke directions flow when Get Directions button is clicked")]
    public async Task HandleDirectionsClick_ShouldStartDirectionsFlow_WhenClicked()
    {
        var center = BowlingCenterSummaryResponseFactory.Create(name: "Directions Bowl");
        SetupSuccessResponse([center]);

        var cut = _ctx.Render<BowlingCentersPage>();
        await cut.InvokeAsync(() =>
            cut.FindAll("button").First(b => b.TextContent.Trim() == "Get Directions").Click());

        cut.Markup.ShouldContain("Directions Bowl");
    }

    [Fact(DisplayName = "Should focus map location when center card is clicked")]
    public async Task HandleCenterCardClick_ShouldFocusMapLocation_WhenCardClicked()
    {
        var center = BowlingCenterSummaryResponseFactory.Create(name: "Focus Bowl");
        SetupSuccessResponse([center]);

        var cut = _ctx.Render<BowlingCentersPage>();
        await cut.InvokeAsync(() => cut.Find(".neba-card").Click());

        cut.Markup.ShouldContain("Focus Bowl");
    }

    private void SetupSuccessResponse(IReadOnlyCollection<BowlingCenterSummaryResponse> centers)
    {
        var response = new Mock<IApiResponse<CollectionResponse<BowlingCenterSummaryResponse>>>(MockBehavior.Strict);
        response.Setup(r => r.IsSuccessStatusCode).Returns(true);
        response.Setup(r => r.StatusCode).Returns(System.Net.HttpStatusCode.OK);
        response.Setup(r => r.Content).Returns(new CollectionResponse<BowlingCenterSummaryResponse>
        {
            Items = centers,
        });

        _mockApi
            .Setup(x => x.ListBowlingCentersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response.Object);
    }

    private void SetupFailureResponse(System.Net.HttpStatusCode statusCode)
    {
        var response = new Mock<IApiResponse<CollectionResponse<BowlingCenterSummaryResponse>>>(MockBehavior.Strict);
        response.Setup(r => r.IsSuccessStatusCode).Returns(false);
        response.Setup(r => r.StatusCode).Returns(statusCode);
        response.Setup(r => r.Content).Returns((CollectionResponse<BowlingCenterSummaryResponse>?)null);

        _mockApi
            .Setup(x => x.ListBowlingCentersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response.Object);
    }
}