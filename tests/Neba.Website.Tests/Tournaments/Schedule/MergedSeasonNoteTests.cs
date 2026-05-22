using Bunit;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Tests.Tournaments.Schedule;

[UnitTest]
[Component("Website.Tournaments.Schedule.MergedSeasonNote")]
public sealed class MergedSeasonNoteTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render merged season explanatory note")]
    public void Render_ShouldShowMergedSeasonMessage_WhenRendered()
    {
        // Act
        var cut = _ctx.Render<MergedSeasonNote>();

        // Assert
        cut.Markup.ShouldContain("Combined Season");
        cut.Markup.ShouldContain("merged into a single combined schedule");
    }
}
