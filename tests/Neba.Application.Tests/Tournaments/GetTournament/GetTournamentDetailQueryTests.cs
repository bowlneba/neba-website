using Neba.Application.Tournaments.GetTournament;
using Neba.Domain.Tournaments;
using Neba.TestFactory.Attributes;

namespace Neba.Application.Tests.Tournaments.GetTournament;

[UnitTest]
[Component("Tournaments")]
public sealed class GetTournamentDetailQueryTests
{
    [Fact(DisplayName = "Expiry should be 5 days")]
    public void Expiry_ShouldBe5Days()
    {
        var query = new GetTournamentDetailQuery { Id = TournamentId.New() };

        query.Expiry.ShouldBe(TimeSpan.FromDays(5));
    }

    [Fact(DisplayName = "Cache key should follow neba:tournaments:{id} format")]
    public void Cache_Key_ShouldFollowExpectedFormat()
    {
        var id = TournamentId.New();
        var query = new GetTournamentDetailQuery { Id = id };

        query.Cache.Key.ShouldBe($"neba:tournaments:{id}");
    }

    [Fact(DisplayName = "Cache key should be specific to the tournament")]
    public void Cache_Key_ShouldBeSpecificToTournament()
    {
        var query1 = new GetTournamentDetailQuery { Id = TournamentId.New() };
        var query2 = new GetTournamentDetailQuery { Id = TournamentId.New() };

        query1.Cache.Key.ShouldNotBe(query2.Cache.Key);
    }

    [Fact(DisplayName = "Cache tags should contain neba")]
    public void Cache_Tags_ShouldContainNebaTag()
    {
        var query = new GetTournamentDetailQuery { Id = TournamentId.New() };

        query.Cache.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "Cache tags should contain neba:tournaments")]
    public void Cache_Tags_ShouldContainTournamentsTag()
    {
        var query = new GetTournamentDetailQuery { Id = TournamentId.New() };

        query.Cache.Tags.ShouldContain("neba:tournaments");
    }

    [Fact(DisplayName = "Cache tags should contain tournament-specific tag")]
    public void Cache_Tags_ShouldContainTournamentSpecificTag()
    {
        var id = TournamentId.New();
        var query = new GetTournamentDetailQuery { Id = id };

        query.Cache.Tags.ShouldContain($"neba:tournaments:{id}");
    }

    [Fact(DisplayName = "Cache tags should contain exactly 3 tags")]
    public void Cache_Tags_ShouldContainExactly3Tags()
    {
        var query = new GetTournamentDetailQuery { Id = TournamentId.New() };

        query.Cache.Tags.Count.ShouldBe(3);
    }
}
