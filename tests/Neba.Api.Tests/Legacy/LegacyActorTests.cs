using Neba.Api.Identity;
using Neba.Api.Legacy;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Legacy;

[UnitTest]
[Component("Legacy")]
public sealed class LegacyActorTests
{
    [Fact(DisplayName = "EnterActorScope sets the ambient actor until disposed")]
    public void EnterActorScope_SetsActor_UntilDisposed()
    {
        // Act
        using (LegacyActor.EnterActorScope())
        {
            // Assert
            AmbientActorContext.ActorId.ShouldBe(LegacyActor.Id);
        }

        AmbientActorContext.ActorId.ShouldBeNull();
    }
}
