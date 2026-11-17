using Neba.Api.Caching;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments;

[UnitTest]
[Component("Tournaments")]
public sealed class TournamentCacheTagTests
{
    // The Series write handlers clear this tag so a renamed Series or a new logo shows on the tournament pages
    // at once. If a tournament read ever stops carrying it, that eviction silently stops working.
    [Fact(DisplayName = "The cached tournament list and detail should both carry the tag the Series writes clear")]
    public void TournamentReads_ShouldCarryTheSeriesEvictionTag()
    {
        // Arrange & Act
        var list = CacheDescriptors.Tournaments.ListForSeason(SeasonId.New(), callerIsAuthenticated: false, callerHasTournamentManagementPermission: false);
        var detail = CacheDescriptors.Tournaments.TournamentDetail(TournamentId.New(), callerIsAuthenticated: true, callerHasTournamentManagementPermission: true);

        // Assert
        CacheDescriptors.Tournaments.Tag.ShouldBe("neba:tournaments");
        list.Tags.ShouldContain(CacheDescriptors.Tournaments.Tag);
        detail.Tags.ShouldContain(CacheDescriptors.Tournaments.Tag);
    }
}
