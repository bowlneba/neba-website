using Neba.Api.Contracts;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Contracts;

[UnitTest]
[Component("Api.Contracts")]
public sealed class CollectionResponseTests
{
    [Fact(DisplayName = "TotalCount should return items count")]
    public void TotalCount_ShouldReturnItemsCount()
    {
        // Arrange
        var items = new[] { "a", "b", "c" };
        var response = new CollectionResponse<string> { Items = items };

        // Act
        var totalCount = response.TotalCount;

        // Assert
        totalCount.ShouldBe(3);
    }

    [Fact(DisplayName = "TotalCount should return zero when collection is empty")]
    public void TotalCount_ShouldReturnZero_WhenCollectionIsEmpty()
    {
        // Arrange
        var response = new CollectionResponse<int> { Items = [] };

        // Act
        var totalCount = response.TotalCount;

        // Assert
        totalCount.ShouldBe(0);
    }

    [Fact(DisplayName = "Items should return the provided collection")]
    public void Items_ShouldReturnProvidedCollection()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };

        // Act
        var response = new CollectionResponse<int> { Items = items };

        // Assert
        response.Items.ShouldBe(items);
    }
}
