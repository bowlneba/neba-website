using Bunit;

using Microsoft.AspNetCore.Components;

using Neba.Api.Contracts.BowlingCenters.ListBowlingCenters;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.BowlingCenters;
using Neba.Website.Server.Series;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesFinaleSectionTests : IDisposable
{
    private readonly BunitContext _ctx = new();
    private int _changes;

    public SeriesFinaleSectionTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose() => _ctx.Dispose();

    private IRenderedComponent<SeriesFinaleSection> Render(
        SeriesFormModel model,
        bool isLocked = false,
        string? currentHost = null,
        IReadOnlyCollection<BowlingCenterSummaryResponse>? centers = null)
        => _ctx.Render<SeriesFinaleSection>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.BowlingCenters, centers ?? [])
            .Add(x => x.IsLocked, isLocked)
            .Add(x => x.CurrentHost, currentHost)
            .Add(x => x.OnChanged, EventCallback.Factory.Create(this, () => _changes++)));

    private static SeriesFormModel WithFinale()
        => new() { Finale = new SeriesFinaleFormModel { FinalistCount = 4 } };

    [Fact(DisplayName = "The toggle should show the fields and set Model.Finale, and clear it when turned off")]
    public async Task Toggle_ShouldSetAndClearFinale()
    {
        // Arrange
        var model = new SeriesFormModel();
        var cut = Render(model);
        cut.FindAll("#finale-count").ShouldBeEmpty();

        // Act
        await cut.Find("input[type=checkbox]").ChangeAsync(new ChangeEventArgs { Value = true });

        // Assert
        model.Finale.ShouldNotBeNull();
        cut.FindAll("#finale-count").Count.ShouldBe(1);

        // Act
        await cut.Find("input[type=checkbox]").ChangeAsync(new ChangeEventArgs { Value = false });

        // Assert
        model.Finale.ShouldBeNull();
        cut.FindAll("#finale-count").ShouldBeEmpty();
        _changes.ShouldBe(2);
    }

    [Fact(DisplayName = "Each field should write the model and call OnChanged")]
    public async Task Fields_ShouldWriteModel()
    {
        // Arrange
        var model = WithFinale();
        var cut = Render(model);

        // Act
        await cut.Find("#finale-count").ChangeAsync(new ChangeEventArgs { Value = "6" });
        await cut.Find("#finale-time").ChangeAsync(new ChangeEventArgs { Value = "14:30" });

        // Assert
        model.Finale!.FinalistCount.ShouldBe(6);
        model.Finale.StartTime.ShouldBe(new TimeOnly(14, 30));
        _changes.ShouldBe(2);
    }

    [Fact(DisplayName = "An empty count should stay null")]
    public async Task Count_ShouldBeNull_WhenEmpty()
    {
        // Arrange
        var model = WithFinale();
        var cut = Render(model);

        // Act
        await cut.Find("#finale-count").ChangeAsync(new ChangeEventArgs { Value = string.Empty });

        // Assert
        model.Finale!.FinalistCount.ShouldBeNull();
    }

    [Fact(DisplayName = "With no host the picker should show 'To be announced'")]
    public void Host_ShouldShowToBeAnnounced_WhenNone()
    {
        // Arrange
        var model = WithFinale();

        // Act
        var cut = Render(model, centers: [BowlingCenterSummaryResponseFactory.Create()]);

        // Assert
        cut.Markup.ShouldContain("To be announced");
    }

    [Fact(DisplayName = "A saved host that has left the list should show as the current host")]
    public void Host_ShouldShowCurrentHost_WhenNotListed()
    {
        // Arrange
        var model = WithFinale();
        model.Finale!.BowlingCenterId = "99999";

        // Act
        var cut = Render(model, currentHost: "Old Lanes, Boston, MA", centers: [BowlingCenterSummaryResponseFactory.Create()]);

        // Assert
        cut.Markup.ShouldContain("Current host (no longer listed): Old Lanes, Boston, MA");
    }

    [Fact(DisplayName = "When locked the toggle and the count should be disabled while date, time and host stay editable")]
    public void Locked_ShouldDisableToggleAndCount()
    {
        // Arrange
        var model = WithFinale();

        // Act
        var cut = Render(model, isLocked: true);

        // Assert
        cut.Find("input[type=checkbox]").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("#finale-count").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("#finale-time").HasAttribute("disabled").ShouldBeFalse();
        cut.Find("#finale-date").HasAttribute("disabled").ShouldBeFalse();
        cut.Markup.ShouldContain("fixed once processing begins");
    }

    [Fact(DisplayName = "Finale errors should render under the section")]
    public void Errors_ShouldRender()
    {
        // Arrange
        var model = WithFinale();
        model.Finale!.Errors.Add("The Finale must follow every Stage.");

        // Act
        var cut = Render(model);

        // Assert
        cut.Find(".series-editor__row-error").TextContent.ShouldBe("The Finale must follow every Stage.");
    }
}