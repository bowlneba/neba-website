using Bunit;

using ErrorOr;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Sponsors;
using Neba.Api.Contracts.Tournaments.AddTournamentSponsor;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Sponsors;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Notifications;
using Neba.Website.Server.Tournaments.Detail;

namespace Neba.Website.Tests.Tournaments.Detail;

[UnitTest]
[Component("Website.Tournaments.Detail.ManageTournamentSponsors")]
public sealed class ManageTournamentSponsorsTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly ToastService _toastService;

    public ManageTournamentSponsorsTests()
    {
        _toastService = new ToastService();

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        _ctx.Services.AddSingleton(_toastService);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _toastService.Dispose();
    }

    private static Task<ErrorOr<CollectionResponse<SponsorSummaryResponse>>> ListActiveSponsorsAsync(
        IReadOnlyCollection<SponsorSummaryResponse> items) =>
        Task.FromResult<ErrorOr<CollectionResponse<SponsorSummaryResponse>>>(new CollectionResponse<SponsorSummaryResponse> { Items = items });

    private IRenderedComponent<ManageTournamentSponsors> Render(
        IReadOnlyCollection<TournamentDetailSponsorViewModel> sponsors,
        Action? onChanged = null,
        Func<CancellationToken, Task<ErrorOr<CollectionResponse<SponsorSummaryResponse>>>>? onListActiveSponsorsRequestedAsync = null,
        Func<AddTournamentSponsorInput, CancellationToken, Task<ErrorOr<Success>>>? onAddSponsorRequestedAsync = null,
        Func<string, CancellationToken, Task<ErrorOr<Success>>>? onRemoveSponsorRequestedAsync = null)
        => _ctx.Render<ManageTournamentSponsors>(p =>
        {
            p.Add(x => x.Sponsors, sponsors);
            p.Add(x => x.OnListActiveSponsorsRequestedAsync, onListActiveSponsorsRequestedAsync ?? ((_) => ListActiveSponsorsAsync([])));
            p.Add(x => x.OnAddSponsorRequestedAsync, onAddSponsorRequestedAsync ?? ((_, _) => Task.FromResult<ErrorOr<Success>>(Result.Success)));
            p.Add(x => x.OnRemoveSponsorRequestedAsync, onRemoveSponsorRequestedAsync ?? ((_, _) => Task.FromResult<ErrorOr<Success>>(Result.Success)));

            if (onChanged is not null)
            {
                p.Add(x => x.OnChanged, onChanged);
            }
        });

    [Fact(DisplayName = "Should show Title Sponsor badge only on the title sponsor")]
    public void Render_ShouldShowTitleSponsorBadge_OnlyOnTitleSponsor()
    {
        // Arrange
        var titleSponsor = TournamentDetailSponsorViewModelFactory.Create(
            sponsorId: "01000000000000000000000001", name: "Kegel", titleSponsor: true, sponsorshipAmount: 2500m);
        var otherSponsor = TournamentDetailSponsorViewModelFactory.Create(
            sponsorId: "01000000000000000000000002", name: "Storm", titleSponsor: false, sponsorshipAmount: 500m);

        // Act
        var cut = Render([titleSponsor, otherSponsor]);

        // Assert
        cut.FindAll(".neba-badge-primary").Count.ShouldBe(1);
        cut.Markup.ShouldContain("$2,500");
        cut.Markup.ShouldContain("$500");
    }

    [Fact(DisplayName = "Should show empty state when no sponsors are attached")]
    public void Render_ShouldShowEmptyState_WhenNoSponsors()
    {
        // Act
        var cut = Render([]);

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

        var cut = Render(
            [attached],
            onListActiveSponsorsRequestedAsync: _ => ListActiveSponsorsAsync([available, alreadyAttached]));

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

        var cut = Render([sponsor]);

        // Act
        cut.Find(".mts-row__remove").Click();

        // Assert
        cut.Markup.ShouldContain("Remove sponsor?");
        cut.Markup.ShouldContain("Remove Kegel as a sponsor of this tournament");
    }

    [Fact(DisplayName = "Should call the remove delegate and notify success when Remove is confirmed")]
    public void ConfirmRemove_ShouldCallDelegateAndToastSuccess_WhenConfirmed()
    {
        // Arrange
        var sponsor = TournamentDetailSponsorViewModelFactory.Create(sponsorId: "01000000000000000000000001", name: "Kegel");
        var changedCount = 0;
        var removeCalledWithSponsorId = string.Empty;

        var cut = Render(
            [sponsor],
            onChanged: () => changedCount++,
            onRemoveSponsorRequestedAsync: (sponsorId, _) =>
            {
                removeCalledWithSponsorId = sponsorId;
                return Task.FromResult<ErrorOr<Success>>(Result.Success);
            });

        cut.Find(".mts-row__remove").Click();

        // Act
        cut.Find("button.confirm-action-modal-confirm").Click();

        // Assert
        removeCalledWithSponsorId.ShouldBe("01000000000000000000000001");
        changedCount.ShouldBe(1);
        _toastService.Current.ShouldNotBeNull();
        _toastService.Current.Severity.ShouldBe(NotifySeverity.Success);
    }

    [Fact(DisplayName = "Should show an error message and not invoke OnChanged when Remove fails")]
    public void ConfirmRemove_ShouldShowError_WhenRemoveFails()
    {
        // Arrange
        var sponsor = TournamentDetailSponsorViewModelFactory.Create(sponsorId: "01000000000000000000000001", name: "Kegel");
        var changedCount = 0;

        var cut = Render(
            [sponsor],
            onChanged: () => changedCount++,
            onRemoveSponsorRequestedAsync: (_, _) => Task.FromResult<ErrorOr<Success>>(Error.Conflict("Sponsor.Conflict", "Could not remove sponsor.")));

        cut.Find(".mts-row__remove").Click();

        // Act
        cut.Find("button.confirm-action-modal-confirm").Click();

        // Assert
        changedCount.ShouldBe(0);
        cut.Find(".neba-alert").ShouldNotBeNull();
    }
}
