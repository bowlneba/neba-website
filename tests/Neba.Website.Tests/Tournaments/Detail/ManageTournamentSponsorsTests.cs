using Bunit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Sponsors;
using Neba.Api.Contracts.Tournaments;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Sponsors;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Notifications;
using Neba.Website.Server.Services;
using Neba.Website.Server.Tournaments.Detail;

using Refit;
using Refit.Testing;

namespace Neba.Website.Tests.Tournaments.Detail;

[UnitTest]
[Component("Website.Tournaments.Detail.ManageTournamentSponsors")]
public sealed class ManageTournamentSponsorsTests : IDisposable
{
    private const string TournamentId = "01000000000000000000000099";

    private readonly BunitContext _ctx;
    private readonly Mock<ITournamentsApi> _mockTournamentsApi;
    private readonly Mock<ISponsorsApi> _mockSponsorsApi;
    private readonly ToastService _toastService;

    public ManageTournamentSponsorsTests()
    {
        _mockTournamentsApi = new Mock<ITournamentsApi>(MockBehavior.Strict);
        _mockSponsorsApi = new Mock<ISponsorsApi>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _toastService = new ToastService();

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        _ctx.Services.AddSingleton(_mockTournamentsApi.Object);
        _ctx.Services.AddSingleton(_mockSponsorsApi.Object);
        _ctx.Services.AddSingleton(new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance));
        _ctx.Services.AddSingleton(_toastService);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _toastService.Dispose();
    }

    [Fact(DisplayName = "Should show Title Sponsor badge only on the title sponsor")]
    public void Render_ShouldShowTitleSponsorBadge_OnlyOnTitleSponsor()
    {
        // Arrange
        var titleSponsor = TournamentDetailSponsorViewModelFactory.Create(
            sponsorId: "01000000000000000000000001", name: "Kegel", titleSponsor: true, sponsorshipAmount: 2500m);
        var otherSponsor = TournamentDetailSponsorViewModelFactory.Create(
            sponsorId: "01000000000000000000000002", name: "Storm", titleSponsor: false, sponsorshipAmount: 500m);

        // Act
        var cut = _ctx.Render<ManageTournamentSponsors>(p => p
            .Add(x => x.TournamentId, TournamentId)
            .Add(x => x.Sponsors, [titleSponsor, otherSponsor]));

        // Assert
        cut.FindAll(".neba-badge-primary").Count.ShouldBe(1);
        cut.Markup.ShouldContain("$2,500");
        cut.Markup.ShouldContain("$500");
    }

    [Fact(DisplayName = "Should show empty state when no sponsors are attached")]
    public void Render_ShouldShowEmptyState_WhenNoSponsors()
    {
        // Act
        var cut = _ctx.Render<ManageTournamentSponsors>(p => p
            .Add(x => x.TournamentId, TournamentId)
            .Add(x => x.Sponsors, []));

        // Assert
        cut.Markup.ShouldContain("No sponsors attached to this tournament yet.");
    }

    [Fact(DisplayName = "Should filter out already-attached sponsors from the Add modal picker")]
    public void OpenAddModal_ShouldFilterOutAttachedSponsors()
    {
        // Arrange
        var attached = TournamentDetailSponsorViewModelFactory.Create(sponsorId: "01000000000000000000000001", name: "Kegel");
        var available = SponsorSummaryResponseFactory.Create(sponsorId: "01000000000000000000000002", name: "Storm");
        var alreadyAttached = SponsorSummaryResponseFactory.Create(sponsorId: "01000000000000000000000001", name: "Kegel");

        using var listResponse = new StubApiResponse<CollectionResponse<SponsorSummaryResponse>>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new CollectionResponse<SponsorSummaryResponse> { Items = [available, alreadyAttached] }
        };
        _mockSponsorsApi
            .Setup(x => x.ListActiveSponsorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(listResponse);

        var cut = _ctx.Render<ManageTournamentSponsors>(p => p
            .Add(x => x.TournamentId, TournamentId)
            .Add(x => x.Sponsors, [attached]));

        // Act
        cut.Find("button.neba-btn-primary").Click();

        // Assert
        var options = cut.Find("#sponsor-pick").TextContent;
        options.ShouldContain("Storm");
        options.ShouldNotContain("Kegel");
    }

    [Fact(DisplayName = "Should open confirm dialog naming the sponsor when Remove is clicked")]
    public void RequestRemove_ShouldOpenConfirmDialog_NamingSponsor()
    {
        // Arrange
        var sponsor = TournamentDetailSponsorViewModelFactory.Create(name: "Kegel");

        var cut = _ctx.Render<ManageTournamentSponsors>(p => p
            .Add(x => x.TournamentId, TournamentId)
            .Add(x => x.Sponsors, [sponsor]));

        // Act
        cut.Find(".mts-row__remove").Click();

        // Assert
        cut.Markup.ShouldContain("Remove sponsor?");
        cut.Markup.ShouldContain("Remove Kegel as a sponsor of this tournament");
    }

    [Fact(DisplayName = "Should call RemoveTournamentSponsorAsync and notify success when Remove is confirmed")]
    public void ConfirmRemove_ShouldCallApiAndToastSuccess_WhenConfirmed()
    {
        // Arrange
        var sponsor = TournamentDetailSponsorViewModelFactory.Create(sponsorId: "01000000000000000000000001", name: "Kegel");

        using var removeResponse = new StubApiResponse<object>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.NoContent
        };
        _mockTournamentsApi
            .Setup(x => x.RemoveTournamentSponsorAsync(TournamentId, "01000000000000000000000001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(removeResponse);

        var changedCount = 0;

        var cut = _ctx.Render<ManageTournamentSponsors>(p => p
            .Add(x => x.TournamentId, TournamentId)
            .Add(x => x.Sponsors, [sponsor])
            .Add(x => x.OnChanged, () => changedCount++));

        cut.Find(".mts-row__remove").Click();

        // Act
        cut.Find("button.confirm-action-modal-confirm").Click();

        // Assert
        changedCount.ShouldBe(1);
        _toastService.Current.ShouldNotBeNull();
        _toastService.Current.Severity.ShouldBe(NotifySeverity.Success);
    }

    [Fact(DisplayName = "Should show an error message and not invoke OnChanged when Remove fails")]
    public void ConfirmRemove_ShouldShowError_WhenRemoveFails()
    {
        // Arrange
        var sponsor = TournamentDetailSponsorViewModelFactory.Create(sponsorId: "01000000000000000000000001", name: "Kegel");

        using var removeResponse = new StubApiResponse<object>
        {
            IsSuccessStatusCode = false,
            StatusCode = System.Net.HttpStatusCode.Conflict
        };
        _mockTournamentsApi
            .Setup(x => x.RemoveTournamentSponsorAsync(TournamentId, "01000000000000000000000001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(removeResponse);

        var changedCount = 0;

        var cut = _ctx.Render<ManageTournamentSponsors>(p => p
            .Add(x => x.TournamentId, TournamentId)
            .Add(x => x.Sponsors, [sponsor])
            .Add(x => x.OnChanged, () => changedCount++));

        cut.Find(".mts-row__remove").Click();

        // Act
        cut.Find("button.confirm-action-modal-confirm").Click();

        // Assert
        changedCount.ShouldBe(0);
        cut.Find(".neba-alert").ShouldNotBeNull();
    }
}
