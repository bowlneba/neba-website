using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.ListTournamentsInSeason;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.ListTournamentsInSeason;

[UnitTest]
[Component("Tournaments")]
public sealed class ListTournamentsInSeasonQueryTests
{
    [Fact(DisplayName = "Expiry should be 14 days")]
    public void Expiry_ShouldBe14Days()
    {
        // Act
        var query = new ListTournamentsInSeasonQuery { SeasonId = SeasonId.New(), CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true };

        // Assert
        query.Expiry.ShouldBe(TimeSpan.FromDays(14));
    }

    [Fact(DisplayName = "Cache key should follow neba:tournaments:{seasonId}:list:scope:{scope} format")]
    public void Cache_Key_ShouldFollowExpectedFormat()
    {
        // Arrange
        var seasonId = SeasonId.New();

        // Act
        var query = new ListTournamentsInSeasonQuery { SeasonId = seasonId, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true };

        // Assert
        query.Cache.Key.ShouldBe($"neba:tournaments:{seasonId}:list:scope:management");
    }

    [Fact(DisplayName = "Cache key should be specific to the season")]
    public void Cache_Key_ShouldBeSpecificToSeason()
    {
        // Act
        var query1 = new ListTournamentsInSeasonQuery { SeasonId = SeasonId.New(), CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true };
        var query2 = new ListTournamentsInSeasonQuery { SeasonId = SeasonId.New(), CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true };

        // Assert
        query1.Cache.Key.ShouldNotBe(query2.Cache.Key);
    }

    [Fact(DisplayName = "Cache key should differ by caller scope")]
    public void Cache_Key_ShouldDifferByCallerScope()
    {
        // Arrange
        var seasonId = SeasonId.New();

        // Act
        var anonymous = new ListTournamentsInSeasonQuery { SeasonId = seasonId, CallerIsAuthenticated = false, CallerHasTournamentManagementPermission = false };
        var authenticated = new ListTournamentsInSeasonQuery { SeasonId = seasonId, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = false };
        var management = new ListTournamentsInSeasonQuery { SeasonId = seasonId, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true };

        // Assert
        anonymous.Cache.Key.ShouldBe($"neba:tournaments:{seasonId}:list:scope:public");
        authenticated.Cache.Key.ShouldBe($"neba:tournaments:{seasonId}:list:scope:authenticated");
        management.Cache.Key.ShouldBe($"neba:tournaments:{seasonId}:list:scope:management");
    }

    [Fact(DisplayName = "Cache tags should contain neba")]
    public void Cache_Tags_ShouldContainNebaTag()
    {
        // Act
        var query = new ListTournamentsInSeasonQuery { SeasonId = SeasonId.New(), CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true };

        // Assert
        query.Cache.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "Cache tags should contain neba:tournaments")]
    public void Cache_Tags_ShouldContainTournamentsTag()
    {
        // Act
        var query = new ListTournamentsInSeasonQuery { SeasonId = SeasonId.New(), CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true };

        // Assert
        query.Cache.Tags.ShouldContain("neba:tournaments");
    }

    [Fact(DisplayName = "Cache tags should contain season-specific tag")]
    public void Cache_Tags_ShouldContainSeasonSpecificTag()
    {
        // Arrange
        var seasonId = SeasonId.New();

        // Act
        var query = new ListTournamentsInSeasonQuery { SeasonId = seasonId, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true };

        // Assert
        query.Cache.Tags.ShouldContain($"neba:tournaments:{seasonId}");
    }
}