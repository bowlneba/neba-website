using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.BowlingCenters;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.Domain;

[UnitTest]
[Component("Series")]
public sealed class SeriesFinaleTests
{
    [Fact(DisplayName = "Describe should copy every field from the definition")]
    public void Describe_ShouldCopyEveryField()
    {
        // Arrange
        var center = CertificationNumberFactory.Create();
        var definition = SeriesFinaleDefinitionFactory.Create(6, new DateOnly(2025, 10, 4), new TimeOnly(18, 30), center);

        // Act
        var finale = SeriesFinale.Describe(definition);

        // Assert
        finale.FinalistCount.ShouldBe(6);
        finale.Date.ShouldBe(new DateOnly(2025, 10, 4));
        finale.StartTime.ShouldBe(new TimeOnly(18, 30));
        finale.BowlingCenterId.ShouldBe(center);
    }

    [Fact(DisplayName = "Describe should leave the center null when the definition has none")]
    public void Describe_ShouldLeaveCenterNull()
    {
        // Arrange & Act
        var finale = SeriesFinale.Describe(SeriesFinaleDefinitionFactory.Create());

        // Assert
        finale.BowlingCenterId.ShouldBeNull();
    }

    [Fact(DisplayName = "Two Finales with the same values should be equal")]
    public void Finales_ShouldBeEqual_WhenValuesMatch()
    {
        // Arrange
        var definition = SeriesFinaleDefinitionFactory.Create();

        // Act
        var a = SeriesFinale.Describe(definition);
        var b = SeriesFinale.Describe(definition);

        // Assert
        a.ShouldBe(b);
    }
}