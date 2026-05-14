using Neba.Application.Caching;
using Neba.Domain.Seasons;
using Neba.Domain.Tournaments;
using Neba.TestFactory.Attributes;

namespace Neba.Application.Tests.Tournaments;

[UnitTest]
[Component("Tournaments")]
public sealed class TournamentsCacheDescriptorsTests
{
    [Fact(DisplayName = "ListForSeason key should follow neba:tournaments:{id}:list format")]
    public void ListForSeason_Key_ShouldFollowExpectedFormat()
    {
        var seasonId = SeasonId.New();

        var descriptor = CacheDescriptors.Tournaments.ListForSeason(seasonId);

        descriptor.Key.ShouldBe($"neba:tournaments:{seasonId}:list");
    }

    [Fact(DisplayName = "ListForSeason key should be specific to the season")]
    public void ListForSeason_Key_ShouldBeSpecificToSeason()
    {
        var season1 = SeasonId.New();
        var season2 = SeasonId.New();

        CacheDescriptors.Tournaments.ListForSeason(season1).Key
            .ShouldNotBe(CacheDescriptors.Tournaments.ListForSeason(season2).Key);
    }

    [Fact(DisplayName = "ListForSeason tags should contain neba")]
    public void ListForSeason_Tags_ShouldContainNebaTag()
    {
        var descriptor = CacheDescriptors.Tournaments.ListForSeason(SeasonId.New());

        descriptor.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "ListForSeason tags should contain neba:tournaments")]
    public void ListForSeason_Tags_ShouldContainTournamentsTag()
    {
        var descriptor = CacheDescriptors.Tournaments.ListForSeason(SeasonId.New());

        descriptor.Tags.ShouldContain("neba:tournaments");
    }

    [Fact(DisplayName = "ListForSeason tags should contain season-specific tag")]
    public void ListForSeason_Tags_ShouldContainSeasonSpecificTag()
    {
        var seasonId = SeasonId.New();

        var descriptor = CacheDescriptors.Tournaments.ListForSeason(seasonId);

        descriptor.Tags.ShouldContain($"neba:tournaments:{seasonId}");
    }

    [Fact(DisplayName = "ListForSeason tags should contain exactly 3 tags")]
    public void ListForSeason_Tags_ShouldContainExactly3Tags()
    {
        var descriptor = CacheDescriptors.Tournaments.ListForSeason(SeasonId.New());

        descriptor.Tags.Count.ShouldBe(3);
    }

    [Fact(DisplayName = "TournamentDetail key should follow neba:tournaments:{id} format")]
    public void TournamentDetail_Key_ShouldFollowExpectedFormat()
    {
        var id = TournamentId.New();

        var descriptor = CacheDescriptors.Tournaments.TournamentDetail(id);

        descriptor.Key.ShouldBe($"neba:tournaments:{id}");
    }

    [Fact(DisplayName = "TournamentDetail key should be specific to the tournament")]
    public void TournamentDetail_Key_ShouldBeSpecificToTournament()
    {
        var id1 = TournamentId.New();
        var id2 = TournamentId.New();

        CacheDescriptors.Tournaments.TournamentDetail(id1).Key
            .ShouldNotBe(CacheDescriptors.Tournaments.TournamentDetail(id2).Key);
    }

    [Fact(DisplayName = "TournamentDetail tags should contain neba")]
    public void TournamentDetail_Tags_ShouldContainNebaTag()
    {
        var descriptor = CacheDescriptors.Tournaments.TournamentDetail(TournamentId.New());

        descriptor.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "TournamentDetail tags should contain neba:tournaments")]
    public void TournamentDetail_Tags_ShouldContainTournamentsTag()
    {
        var descriptor = CacheDescriptors.Tournaments.TournamentDetail(TournamentId.New());

        descriptor.Tags.ShouldContain("neba:tournaments");
    }

    [Fact(DisplayName = "TournamentDetail tags should contain tournament-specific tag")]
    public void TournamentDetail_Tags_ShouldContainTournamentSpecificTag()
    {
        var id = TournamentId.New();

        var descriptor = CacheDescriptors.Tournaments.TournamentDetail(id);

        descriptor.Tags.ShouldContain($"neba:tournaments:{id}");
    }

    [Fact(DisplayName = "TournamentDetail tags should contain exactly 3 tags")]
    public void TournamentDetail_Tags_ShouldContainExactly3Tags()
    {
        var descriptor = CacheDescriptors.Tournaments.TournamentDetail(TournamentId.New());

        descriptor.Tags.Count.ShouldBe(3);
    }
}