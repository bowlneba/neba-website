using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.CheckTeamEligibility;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.CheckTeamEligibility;

[UnitTest]
[Component("Tournaments")]
public sealed class ResolveInRequestedOrderTests
{
    [Fact(DisplayName = "ResolveInRequestedOrder returns the loaded bowlers in the order they were requested")]
    public void ResolveInRequestedOrder_ShouldReturnRequestedOrder_WhenEveryIdIsLoaded()
    {
        // Arrange
        var first = BowlerId.New();
        var second = BowlerId.New();
        var third = BowlerId.New();
        var loaded = new Dictionary<BowlerId, string>
        {
            [first] = "first",
            [second] = "second",
            [third] = "third"
        };

        // Act
        var ordered = CheckTeamEligibilityQueryHandler.ResolveInRequestedOrder([third, first, second], loaded);

        // Assert
        ordered.ShouldBe(["third", "first", "second"]);
    }

    [Fact(DisplayName = "ResolveInRequestedOrder throws and names the bowler when a requested ID was not loaded")]
    public void ResolveInRequestedOrder_ShouldThrowNamingTheBowler_WhenARequestedIdWasNotLoaded()
    {
        // Arrange
        var loadedId = BowlerId.New();
        var missingId = BowlerId.New();
        var loaded = new Dictionary<BowlerId, string> { [loadedId] = "loaded" };

        // Act
        void Act() => CheckTeamEligibilityQueryHandler.ResolveInRequestedOrder([loadedId, missingId], loaded);

        // Assert
        var exception = Should.Throw<InvalidOperationException>(Act);
        exception.Message.ShouldContain(missingId.ToString());
        exception.Message.ShouldContain("not-found check must return before the members are built");
    }
}