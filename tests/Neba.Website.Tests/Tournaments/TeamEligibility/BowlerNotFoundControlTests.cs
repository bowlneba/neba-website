using Bunit;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Tournaments.TeamEligibility;

namespace Neba.Website.Tests.Tournaments.TeamEligibility;

[UnitTest]
[Component("Website.Tournaments.BowlerNotFoundControl")]
public sealed class BowlerNotFoundControlTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Theory(DisplayName = "Should read 'Mark as not in our system' until marked, then 'Undo'")]
    [InlineData(false, "Mark as not in our system", TestDisplayName = "Not marked")]
    [InlineData(true, "Undo", TestDisplayName = "Marked")]
    public void Render_ShouldShowLabelForState(bool isMarked, string expected)
    {
        // Arrange & Act
        var cut = _ctx.Render<BowlerNotFoundControl>(p => p
            .Add(x => x.IsMarked, isMarked)
            .Add(x => x.OnToggle, () => { }));

        // Assert
        cut.Find("button").TextContent.Trim().ShouldBe(expected);
    }

    [Fact(DisplayName = "Should raise OnToggle when clicked")]
    public async Task Click_ShouldRaiseOnToggle()
    {
        // Arrange
        var toggles = 0;
        var cut = _ctx.Render<BowlerNotFoundControl>(p => p.Add(x => x.OnToggle, () => toggles++));

        // Act
        await cut.Find("button").ClickAsync();

        // Assert
        toggles.ShouldBe(1);
    }
}
