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
        "Neba.Domain.Tournaments",
        "Neba.Domain.Bowlers",
        "Neba.Domain.Membership",
    ];

    [Theory(DisplayName = "Bounded context should not depend on other bounded contexts")]
    [MemberData(nameof(GetBoundedContextPairs))]
    public void BoundedContext_ShouldNotDependOn_OtherBoundedContext(
        string sourceNamespace,
        string targetNamespace)
    {
        Types().That().ResideInNamespace(sourceNamespace).Should()
            .NotDependOnAnyTypesThat().ResideInNamespace(targetNamespace)
            .Check(ArchModel);
    }

    public static TheoryData<string, string> GetBoundedContextPairs()
    {
        var pairs = new TheoryData<string, string>();

        for (var i = 0; i < BoundedContextNamespaces.Length; i++)
        {
            for (var j = 0; j < BoundedContextNamespaces.Length; j++)
            {
                if (i != j)
                {
                    pairs.Add(BoundedContextNamespaces[i], BoundedContextNamespaces[j]);
                }
            }
        }

        return pairs;
    }
}
