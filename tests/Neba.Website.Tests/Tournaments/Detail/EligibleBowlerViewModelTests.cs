using Neba.TestFactory.Attributes;
using Neba.Website.Server.Tournaments.Detail;

namespace Neba.Website.Tests.Tournaments.Detail;

[UnitTest]
[Component("Website.Tournaments.Detail.EligibleBowlerViewModel")]
public sealed class EligibleBowlerViewModelTests
{
#nullable disable
    [Fact(DisplayName = "Should throw when the name is null")]
    public void Name_ShouldThrow_WhenNull()
    {
        // Arrange
        const string name = null;

        // Act
        var create = () => new EligibleBowlerViewModel { Name = name };

        // Assert
        create.ShouldThrow<ArgumentNullException>();
    }
#nullable enable
}