using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests;

[UnitTest]
[Component("Api")]
public sealed class ApiTestPlaceholder
{
    [Fact]
    public void PlaceholderTest()
    {
        Assert.True(true);
    }
}