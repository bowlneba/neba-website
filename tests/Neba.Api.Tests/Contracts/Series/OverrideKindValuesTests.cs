using Neba.Api.Contracts.Series;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Contracts.Series;

[UnitTest]
[Component("Series")]
public sealed class OverrideKindValuesTests
{
    [Fact(DisplayName = "OverrideKindValues.Add should equal the value of OverrideKind.Add")]
    public void Add_ShouldEqualOverrideKindAddValue()
    {
        // Arrange & Act
        const int contractValue = OverrideKindValues.Add;

        // Assert
        contractValue.ShouldBe(OverrideKind.Add.Value);
    }

    [Fact(DisplayName = "OverrideKindValues.Clear should equal the value of OverrideKind.Clear")]
    public void Clear_ShouldEqualOverrideKindClearValue()
    {
        // Arrange & Act
        const int contractValue = OverrideKindValues.Clear;

        // Assert
        contractValue.ShouldBe(OverrideKind.Clear.Value);
    }
}