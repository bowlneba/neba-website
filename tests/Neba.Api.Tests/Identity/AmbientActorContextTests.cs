using Neba.Api.Identity;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Identity;

[UnitTest]
[Component("Identity")]
public sealed class AmbientActorContextTests
{
    [Fact(DisplayName = "ActorId returns null when no actor has been set")]
    public void ActorId_WhenNoActorSet_ReturnsNull()
    {
        // Arrange & Act
        var actorId = AmbientActorContext.ActorId;

        // Assert
        actorId.ShouldBeNull();
    }

    [Fact(DisplayName = "SetActor makes ActorId return the given value until disposed")]
    public void SetActor_MakesActorIdReturnGivenValue_UntilDisposed()
    {
        // Arrange & Act
        using (AmbientActorContext.SetActor("software-sync"))
        {
            // Assert
            AmbientActorContext.ActorId.ShouldBe("software-sync");
        }

        AmbientActorContext.ActorId.ShouldBeNull();
    }
}