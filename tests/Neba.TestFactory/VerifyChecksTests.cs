using Neba.TestFactory.Attributes;

namespace Neba.TestFactory;

[UnitTest]
[Component("Verify")]
public sealed class VerifyChecksTests
{
    [Fact(DisplayName = "Run verify convention checks")]
    public Task Run() => VerifyChecks.Run();
}
