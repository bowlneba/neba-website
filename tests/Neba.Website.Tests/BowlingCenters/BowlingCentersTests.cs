using Bunit;

using Microsoft.Extensions.DependencyInjection;
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

using BowlingCentersPage = Neba.Website.Server.BowlingCenters.BowlingCenters;

using Refit;

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

        _ctx.ComponentFactories.AddStub<NebaMap>();
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

    private void SetupSuccessResponse(IReadOnlyCollection<BowlingCenterSummaryResponse> centers)
    {
        var response = new Mock<IApiResponse<CollectionResponse<BowlingCenterSummaryResponse>>>(MockBehavior.Strict);
        response.Setup(r => r.IsSuccessStatusCode).Returns(true);
        response.Setup(r => r.StatusCode).Returns(System.Net.HttpStatusCode.OK);
        response.Setup(r => r.Content).Returns(new CollectionResponse<BowlingCenterSummaryResponse>
        {
            Items = centers,
            TotalItems = centers.Count
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
