using Neba.Api.Ambient;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Ambient;

[UnitTest]
[Component("Ambient")]
public sealed class AmbientValueTests
{
    [Fact(DisplayName = "Current returns null when no value has been set")]
    public void Current_WhenNoValueSet_ReturnsNull()
    {
        // Arrange
        var ambient = new AmbientValue<string>();

        // Act
        var current = ambient.Current;

        // Assert
        current.ShouldBeNull();
    }

    [Fact(DisplayName = "Set makes Current return the given value until disposed")]
    public void Set_MakesCurrentReturnGivenValue_UntilDisposed()
    {
        // Arrange
        var ambient = new AmbientValue<string>();

        // Act
        using (ambient.Set("value-1"))
        {
            // Assert
            ambient.Current.ShouldBe("value-1");
        }

        ambient.Current.ShouldBeNull();
    }

    [Fact(DisplayName = "Disposing a nested Set restores the outer value, not null")]
    public void Set_DisposingNestedScope_RestoresOuterValue()
    {
        // Arrange
        var ambient = new AmbientValue<string>();

        // Act & Assert
        using (ambient.Set("outer"))
        {
            using (ambient.Set("inner"))
            {
                ambient.Current.ShouldBe("inner");
            }

            ambient.Current.ShouldBe("outer");
        }

        ambient.Current.ShouldBeNull();
    }
}