using Bunit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Tournaments;
using Neba.Api.Contracts.Tournaments.AddTournamentSponsor;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Sponsors;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Services;
using Neba.Website.Server.Sponsors;
using Neba.Website.Server.Tournaments.Detail;

using Refit;
using Refit.Testing;

namespace Neba.Website.Tests.Tournaments.Detail;

[UnitTest]
[Component("Website.Tournaments.Detail.AddTournamentSponsorModal")]
public sealed class AddTournamentSponsorModalTests : IDisposable
{
    private const string TournamentId = "01000000000000000000000099";

    private readonly BunitContext _ctx;
    private readonly Mock<ITournamentsApi> _mockApi;

    public AddTournamentSponsorModalTests()
    {
        _mockApi = new Mock<ITournamentsApi>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        _ctx.Services.AddSingleton(_mockApi.Object);
        _ctx.Services.AddSingleton(new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance));
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should disable the Add Sponsor button when no sponsor is selected")]
    public void Render_ShouldDisableSubmit_WhenNoSponsorSelected()
    {
        // Arrange
        var sponsor = SponsorSummaryResponseFactory.Create().ToViewModel();

        // Act
        var cut = _ctx.Render<AddTournamentSponsorModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.TournamentId, TournamentId)
            .Add(x => x.AvailableSponsors, [sponsor]));

        // Assert
        cut.Find("button.neba-btn-primary").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact(DisplayName = "Should enable the Add Sponsor button once a sponsor is selected")]
    public void SelectSponsor_ShouldEnableSubmit()
    {
        // Arrange
        var sponsor = SponsorSummaryResponseFactory.Create(sponsorId: "01000000000000000000000001", name: "Kegel").ToViewModel();

        var cut = _ctx.Render<AddTournamentSponsorModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.TournamentId, TournamentId)
            .Add(x => x.AvailableSponsors, [sponsor]));

        // Act
        cut.Find("#sponsor-pick").Change("01000000000000000000000001");

        // Assert
        cut.Find("button.neba-btn-primary").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact(DisplayName = "Should submit the selected sponsor, amount, and title flag when Add Sponsor is clicked")]
    public void SubmitAsync_ShouldCallApiWithSelectedFields()
    {
        // Arrange
        var sponsor = SponsorSummaryResponseFactory.Create(sponsorId: "01000000000000000000000001", name: "Kegel").ToViewModel();

        using var response = new StubApiResponse<object>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.NoContent
        };

        AddTournamentSponsorRequest? capturedRequest = null;
        _mockApi
            .Setup(x => x.AddTournamentSponsorAsync(TournamentId, It.IsAny<AddTournamentSponsorRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, AddTournamentSponsorRequest, CancellationToken>((_, req, _) => capturedRequest = req)
            .ReturnsAsync(response);

        var addedCount = 0;

        var cut = _ctx.Render<AddTournamentSponsorModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.TournamentId, TournamentId)
            .Add(x => x.AvailableSponsors, [sponsor])
            .Add(x => x.OnAdded, () => addedCount++));

        cut.Find("#sponsor-pick").Change("01000000000000000000000001");
        cut.Find("#sponsor-amount").Change("1500");
        cut.Find("#title-sponsor").Change(true);

        // Act
        cut.Find("button.neba-btn-primary").Click();

        // Assert
        addedCount.ShouldBe(1);
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Sponsor.SponsorId.ShouldBe("01000000000000000000000001");
        capturedRequest.Sponsor.SponsorshipAmount.ShouldBe(1500m);
        capturedRequest.Sponsor.TitleSponsor.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should show an error message and not invoke OnAdded when the API call fails")]
    public void SubmitAsync_ShouldShowError_WhenApiFails()
    {
        // Arrange
        var sponsor = SponsorSummaryResponseFactory.Create(sponsorId: "01000000000000000000000001", name: "Kegel").ToViewModel();

        using var response = new StubApiResponse<object>
        {
            IsSuccessStatusCode = false,
            StatusCode = System.Net.HttpStatusCode.Conflict
        };
        _mockApi
            .Setup(x => x.AddTournamentSponsorAsync(TournamentId, It.IsAny<AddTournamentSponsorRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var addedCount = 0;

        var cut = _ctx.Render<AddTournamentSponsorModal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.TournamentId, TournamentId)
            .Add(x => x.AvailableSponsors, [sponsor])
            .Add(x => x.OnAdded, () => addedCount++));

        cut.Find("#sponsor-pick").Change("01000000000000000000000001");

        // Act
        cut.Find("button.neba-btn-primary").Click();

        // Assert
        addedCount.ShouldBe(0);
        cut.Find(".neba-alert").ShouldNotBeNull();
    }
}