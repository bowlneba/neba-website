using Bunit;

using ErrorOr;

using Microsoft.AspNetCore.Components;

using Neba.Api.Contracts.Tournaments.AddTournamentSponsor;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Sponsors;
using Neba.Website.Server.Sponsors;
using Neba.Website.Server.Tournaments.Detail;

namespace Neba.Website.Tests.Tournaments.Detail;

[UnitTest]
[Component("Website.Tournaments.Detail.AddTournamentSponsorModal")]
public sealed class AddTournamentSponsorModalTests : IDisposable
{
    private readonly BunitContext _ctx;

    public AddTournamentSponsorModalTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose() => _ctx.Dispose();

    private IRenderedComponent<AddTournamentSponsorModal> Render(
        IReadOnlyCollection<SponsorSummaryViewModel> availableSponsors,
        Func<AddTournamentSponsorInput, CancellationToken, Task<ErrorOr<Success>>>? onSubmitRequestedAsync = null,
        Action? onAdded = null)
        => _ctx.Render<AddTournamentSponsorModal>(p =>
        {
            p.Add(x => x.IsOpen, true);
            p.Add(x => x.AvailableSponsors, availableSponsors);
            p.Add(x => x.OnSubmitRequestedAsync, onSubmitRequestedAsync ?? ((_, _) => Task.FromResult<ErrorOr<Success>>(Result.Success)));

            if (onAdded is not null)
            {
                p.Add(x => x.OnAdded, onAdded);
            }
        });

    [Fact(DisplayName = "Should disable the Add Sponsor button when no sponsor is selected")]
    public void Render_ShouldDisableSubmit_WhenNoSponsorSelected()
    {
        // Arrange
        var sponsor = SponsorSummaryResponseFactory.Create().ToViewModel();

        // Act
        var cut = Render([sponsor]);

        // Assert
        cut.Find("button.neba-btn-primary").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact(DisplayName = "Should enable the Add Sponsor button once a sponsor is selected")]
    public void SelectSponsor_ShouldEnableSubmit()
    {
        // Arrange
        var sponsor = SponsorSummaryResponseFactory.Create(sponsorId: "01000000000000000000000001", name: "Kegel").ToViewModel();

        var cut = Render([sponsor]);

        // Act
        cut.Find("#sponsor-pick").Change("01000000000000000000000001");

        // Assert
        cut.Find("button.neba-btn-primary").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact(DisplayName = "Should submit the selected sponsor, amount, and title flag when Add Sponsor is clicked")]
    public void SubmitAsync_ShouldCallDelegateWithSelectedFields()
    {
        // Arrange
        var sponsor = SponsorSummaryResponseFactory.Create(sponsorId: "01000000000000000000000001", name: "Kegel").ToViewModel();

        AddTournamentSponsorInput? capturedInput = null;
        var addedCount = 0;

        var cut = Render(
            [sponsor],
            onSubmitRequestedAsync: (input, _) =>
            {
                capturedInput = input;
                return Task.FromResult<ErrorOr<Success>>(Result.Success);
            },
            onAdded: () => addedCount++);

        cut.Find("#sponsor-pick").Change("01000000000000000000000001");
        cut.Find("#sponsor-amount").Change("1500");
        cut.Find("#title-sponsor").Change(true);

        // Act
        cut.Find("button.neba-btn-primary").Click();

        // Assert
        addedCount.ShouldBe(1);
        capturedInput.ShouldNotBeNull();
        capturedInput.SponsorId.ShouldBe("01000000000000000000000001");
        capturedInput.SponsorshipAmount.ShouldBe(1500m);
        capturedInput.TitleSponsor.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should show an error message and not invoke OnAdded when the submit delegate fails")]
    public void SubmitAsync_ShouldShowError_WhenSubmitFails()
    {
        // Arrange
        var sponsor = SponsorSummaryResponseFactory.Create(sponsorId: "01000000000000000000000001", name: "Kegel").ToViewModel();
        var addedCount = 0;

        var cut = Render(
            [sponsor],
            onSubmitRequestedAsync: (_, _) => Task.FromResult<ErrorOr<Success>>(Error.Conflict("Sponsor.Conflict", "Could not add sponsor.")),
            onAdded: () => addedCount++);

        cut.Find("#sponsor-pick").Change("01000000000000000000000001");

        // Act
        cut.Find("button.neba-btn-primary").Click();

        // Assert
        addedCount.ShouldBe(0);
        cut.Find(".neba-alert").ShouldNotBeNull();
    }
}
