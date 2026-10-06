using Bunit;

using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Tournaments.Detail;

namespace Neba.Website.Tests.Tournaments.Detail;

[UnitTest]
[Component("Website.Tournaments.Detail.TournamentAddedMoneyBreakdown")]
public sealed class TournamentAddedMoneyBreakdownTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render one line per contributor with name and formatted amount")]
    public void Render_ShouldRenderOneLinePerContributor_WhenLinesProvided()
    {
        // Arrange
        IReadOnlyList<AddedMoneyLineViewModel> lines =
        [
            AddedMoneyLineViewModelFactory.Create(name: "Sponsor A", amount: 1000m),
            AddedMoneyLineViewModelFactory.Create(name: "Sponsor B", amount: 700m)
        ];

        // Act
        var cut = _ctx.Render<TournamentAddedMoneyBreakdown>(p => p.Add(x => x.Lines, lines));

        // Assert
        var items = cut.FindAll("li");
        items.Count.ShouldBe(2);
        items[0].QuerySelector(".tamb__name")!.TextContent.ShouldBe("Sponsor A");
        items[0].QuerySelector(".tamb__amount")!.TextContent.ShouldBe("$1,000");
        items[1].QuerySelector(".tamb__amount")!.TextContent.ShouldBe("$700");
    }

    [Fact(DisplayName = "Should render nothing when there are no lines")]
    public void Render_ShouldRenderNothing_WhenLinesEmpty()
    {
        // Arrange
        IReadOnlyList<AddedMoneyLineViewModel> lines = [];

        // Act
        var cut = _ctx.Render<TournamentAddedMoneyBreakdown>(p => p.Add(x => x.Lines, lines));

        // Assert
        cut.FindAll("ul").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should mark the NEBA line so it can be spaced from the sponsor lines")]
    public void Render_ShouldMarkNebaLine_WhenLineIsNeba()
    {
        // Arrange
        IReadOnlyList<AddedMoneyLineViewModel> lines =
        [
            AddedMoneyLineViewModelFactory.Create(name: "Sponsor A", amount: 1000m),
            AddedMoneyLineViewModelFactory.Create(name: "NEBA", amount: 300m, isNeba: true)
        ];

        // Act
        var cut = _ctx.Render<TournamentAddedMoneyBreakdown>(p => p.Add(x => x.Lines, lines));

        // Assert
        var items = cut.FindAll("li");
        items[0].ClassList.ShouldNotContain("tamb__line--neba");
        items[1].ClassList.ShouldContain("tamb__line--neba");
    }

    [Fact(DisplayName = "Should render names as plain text with no links")]
    public void Render_ShouldNotRenderLinks_WhenLinesProvided()
    {
        // Arrange
        IReadOnlyList<AddedMoneyLineViewModel> lines = [AddedMoneyLineViewModelFactory.Create()];

        // Act
        var cut = _ctx.Render<TournamentAddedMoneyBreakdown>(p => p.Add(x => x.Lines, lines));

        // Assert
        cut.FindAll("a").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render a long sponsor name in full")]
    public void Render_ShouldRenderLongNameInFull_WhenNameIsLong()
    {
        // Arrange
        const string longName = "Greater Boston Area Bowling Proprietors Association & Friends of NEBA";
        IReadOnlyList<AddedMoneyLineViewModel> lines = [AddedMoneyLineViewModelFactory.Create(name: longName)];

        // Act
        var cut = _ctx.Render<TournamentAddedMoneyBreakdown>(p => p.Add(x => x.Lines, lines));

        // Assert
        cut.Find(".tamb__name").TextContent.ShouldBe(longName);
    }
}
