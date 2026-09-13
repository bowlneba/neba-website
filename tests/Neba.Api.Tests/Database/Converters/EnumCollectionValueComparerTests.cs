using Neba.Api.Database.Converters;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Database.Converters;

[UnitTest]
[Component("Database.Converters")]
public sealed class EnumCollectionValueComparerTests
{
    [Fact(DisplayName = "Should return true when collections contain the same elements in the same order")]
    public void Equals_ShouldReturnTrue_WhenCollectionsContainSameElements()
    {
        // Arrange
        var comparer = EnumCollectionValueComparer.Create<HallOfFameCategory>();
        IReadOnlyCollection<HallOfFameCategory> first = [HallOfFameCategory.SuperiorPerformance, HallOfFameCategory.FriendOfNeba];
        IReadOnlyCollection<HallOfFameCategory> second = [HallOfFameCategory.SuperiorPerformance, HallOfFameCategory.FriendOfNeba];

        // Act
        var result = comparer.Equals(first, second);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should return false when collections differ")]
    public void Equals_ShouldReturnFalse_WhenCollectionsDiffer()
    {
        // Arrange
        var comparer = EnumCollectionValueComparer.Create<HallOfFameCategory>();
        IReadOnlyCollection<HallOfFameCategory> first = [HallOfFameCategory.SuperiorPerformance];
        IReadOnlyCollection<HallOfFameCategory> second = [HallOfFameCategory.MeritoriousService];

        // Act
        var result = comparer.Equals(first, second);

        // Assert
        result.ShouldBeFalse();
    }

#nullable disable
    [Fact(DisplayName = "Should not throw when either collection is null")]
    public void Equals_ShouldNotThrow_WhenEitherCollectionIsNull()
    {
        // Arrange
        var comparer = EnumCollectionValueComparer.Create<HallOfFameCategory>();
        IReadOnlyCollection<HallOfFameCategory> populated = [HallOfFameCategory.SuperiorPerformance];

        // Act
        var result = Should.NotThrow(() => comparer.Equals(null, null));
        var resultAgainstPopulated = Should.NotThrow(() => comparer.Equals(null, populated));

        // Assert
        result.ShouldBeTrue();
        resultAgainstPopulated.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should not throw when hashing a null collection")]
    public void GetHashCode_ShouldNotThrow_WhenCollectionIsNull()
    {
        // Arrange
        var comparer = EnumCollectionValueComparer.Create<HallOfFameCategory>();

        // Act
        var result = Should.NotThrow(() => comparer.GetHashCode(null));

        // Assert
        result.ShouldBe(0);
    }

    [Fact(DisplayName = "Should not throw when snapshotting a null collection")]
    public void Snapshot_ShouldNotThrow_WhenCollectionIsNull()
    {
        // Arrange
        var comparer = EnumCollectionValueComparer.Create<HallOfFameCategory>();

        // Act
        var result = Should.NotThrow(() => comparer.Snapshot(null));

        // Assert
        result.ShouldBeEmpty();
    }
#nullable enable

    [Fact(DisplayName = "Should return equal hash codes for collections with the same elements")]
    public void GetHashCode_ShouldReturnSameValue_WhenCollectionsContainSameElements()
    {
        // Arrange
        var comparer = EnumCollectionValueComparer.Create<HallOfFameCategory>();
        IReadOnlyCollection<HallOfFameCategory> first = [HallOfFameCategory.SuperiorPerformance, HallOfFameCategory.FriendOfNeba];
        IReadOnlyCollection<HallOfFameCategory> second = [HallOfFameCategory.SuperiorPerformance, HallOfFameCategory.FriendOfNeba];

        // Act
        var firstHash = comparer.GetHashCode(first);
        var secondHash = comparer.GetHashCode(second);

        // Assert
        firstHash.ShouldBe(secondHash);
    }
}