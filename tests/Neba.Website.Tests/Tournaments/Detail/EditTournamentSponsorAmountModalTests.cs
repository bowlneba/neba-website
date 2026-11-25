using Bunit;

using ErrorOr;

using Microsoft.AspNetCore.Components;

using Neba.Api.Contracts.Tournaments.EditTournamentSponsorAmount;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Tournaments.Detail;

namespace Neba.Website.Tests.Tournaments.Detail;

[UnitTest]
[Component("Website.Tournaments.Detail.EditTournamentSponsorAmountModal")]
public sealed class EditTournamentSponsorAmountModalTests : IDisposable
{
    private readonly BunitContext _ctx;

    public EditTournamentSponsorAmountModalTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose() => _ctx.Dispose();

    private IRenderedComponent<EditTournamentSponsorAmountModal> Render(
        TournamentDetailSponsorViewModel sponsor,
        Func<string, EditTournamentSponsorAmountInput, CancellationToken, Task<ErrorOr<Success>>>? onSubmitRequestedAsync = null,
        Action? onSaved = null)
        => _ctx.Render<EditTournamentSponsorAmountModal>(p =>
        {
            p.Add(x => x.IsOpen, true);
            p.Add(x => x.Sponsor, sponsor);
            p.Add(x => x.OnSubmitRequestedAsync, onSubmitRequestedAsync ?? ((_, _, _) => Task.FromResult<ErrorOr<Success>>(Result.Success)));

            if (onSaved is not null)
            {
                p.Add(x => x.OnSaved, onSaved);
            }
        });

    [Fact(DisplayName = "Should prefill the amount input with the sponsor's current amount")]
    public void Render_ShouldPrefillCurrentAmount()
    {
        // Arrange
        var sponsor = TournamentDetailSponsorViewModelFactory.Create(sponsorshipAmount: 750m);

        // Act
        var cut = Render(sponsor);

        // Assert
        cut.Find("#sponsor-edit-amount").GetAttribute("value").ShouldBe("750");
    }

    [Fact(DisplayName = "Should submit the sponsor id and new amount and raise OnSaved on success")]
    public void Submit_ShouldCallDelegateAndRaiseSaved_WhenSuccessful()
    {
        // Arrange
        var sponsor = TournamentDetailSponsorViewModelFactory.Create(sponsorId: "01000000000000000000000001", sponsorshipAmount: 500m);
        string? submittedId = null;
        decimal? submittedAmount = null;
        var saved = false;
        var cut = Render(
            sponsor,
            (id, input, _) =>
            {
                submittedId = id;
                submittedAmount = input.SponsorshipAmount;
                return Task.FromResult<ErrorOr<Success>>(Result.Success);
            },
            onSaved: () => saved = true);

        // Act
        cut.Find("#sponsor-edit-amount").Change("900.50");
        cut.Find("button.neba-btn-primary").Click();

        // Assert
        submittedId.ShouldBe("01000000000000000000000001");
        submittedAmount.ShouldBe(900.50m);
        saved.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should show the error and not raise OnSaved when the update fails")]
    public void Submit_ShouldShowError_WhenUpdateFails()
    {
        // Arrange
        var sponsor = TournamentDetailSponsorViewModelFactory.Create();
        var saved = false;
        var cut = Render(
            sponsor,
            (_, _, _) => Task.FromResult<ErrorOr<Success>>(Error.Conflict("Tournament.SponsorNotAttached", "The specified sponsor is not attached to this tournament.")),
            onSaved: () => saved = true);

        // Act
        cut.Find("#sponsor-edit-amount").Change("123");
        cut.Find("button.neba-btn-primary").Click();

        // Assert
        cut.Markup.ShouldContain("The specified sponsor is not attached to this tournament.");
        saved.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should disable Save when the amount is unchanged")]
    public void Save_ShouldBeDisabled_WhenAmountUnchanged()
    {
        // Arrange
        var sponsor = TournamentDetailSponsorViewModelFactory.Create(sponsorshipAmount: 750m);

        // Act
        var cut = Render(sponsor);

        // Assert
        cut.Find("button.neba-btn-primary").HasAttribute("disabled").ShouldBeTrue();
    }

    [Theory(DisplayName = "Should disable Save when the amount is empty or above the maximum")]
    [InlineData("")]
    [InlineData("100000")]
    public void Save_ShouldBeDisabled_WhenAmountInvalid(string value)
    {
        // Arrange
        var sponsor = TournamentDetailSponsorViewModelFactory.Create(sponsorshipAmount: 750m);
        var cut = Render(sponsor);

        // Act
        cut.Find("#sponsor-edit-amount").Change(value);

        // Assert
        cut.Find("button.neba-btn-primary").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact(DisplayName = "Should limit the amount input to the maximum")]
    public void Render_ShouldSetMaxOnAmountInput()
    {
        // Arrange
        var sponsor = TournamentDetailSponsorViewModelFactory.Create();

        // Act
        var cut = Render(sponsor);

        // Assert
        cut.Find("#sponsor-edit-amount").GetAttribute("max").ShouldBe("99999.99");
    }
}
