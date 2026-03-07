using Neba.TestFactory.Attributes;

namespace Neba.Architecture.Tests;

[ArchitectureTest]
[Component("Architecture")]
public sealed class DomainBoundaryTests : ArchitectureTestBase
{
    // Bounded contexts within Neba.Domain that must not cross-reference each other.
    // Shared kernel namespaces (Contact, Geography) are intentionally excluded —
    // they are designed to be used by all bounded contexts.
    private static readonly string[] BoundedContextNamespaces =
    [
        "Neba.Domain.BowlingCenters",
        // Uncomment as each bounded context is implemented:
        // "Neba.Domain.Tournaments",
        // "Neba.Domain.Bowlers",
        // "Neba.Domain.Membership",
    ];

    [Fact(DisplayName = "Bounded context should not depend on other bounded contexts")]
    public void BoundedContext_ShouldNotDependOn_OtherBoundedContext()
    {
        for (var i = 0; i < BoundedContextNamespaces.Length; i++)
        {
            for (var j = 0; j < BoundedContextNamespaces.Length; j++)
            {
                if (i == j) continue;

                Types().That().ResideInNamespace(BoundedContextNamespaces[i]).Should()
                    .NotDependOnAnyTypesThat().ResideInNamespace(BoundedContextNamespaces[j])
                    .Check(ArchModel);
            }
        }
    }
}
