using Neba.Api.Auditing;
using Neba.Api.Identity;
using Neba.Api.Legacy;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Legacy;

[UnitTest]
[Component("Legacy")]
public sealed class LegacyActorTests
{
    [Fact(DisplayName = "EnterAmbientContext sets both the ambient actor and correlation id until disposed")]
    public void EnterAmbientContext_SetsActorAndCorrelationId_UntilDisposed()
    {
        // Act
        using (LegacyActor.EnterAmbientContext("correlation-1"))
        {
            // Assert
            AmbientActorContext.ActorId.ShouldBe(LegacyActor.Id);
            AmbientCorrelationContext.CorrelationId.ShouldBe("correlation-1");
        }

        AmbientActorContext.ActorId.ShouldBeNull();
        AmbientCorrelationContext.CorrelationId.ShouldBeNull();
    }
}
