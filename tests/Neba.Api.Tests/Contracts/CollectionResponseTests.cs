using Neba.Api.Contracts;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Contracts;

[UnitTest]
[Component("Api.Contracts")]
public sealed class CollectionResponseTests
{
    [Fact(DisplayName = "TotalItems should return items count")]
    public void TotalItems_ShouldReturnItemsCount()
    {
        // Arrange
        var items = new[] { "a", "b", "c" };
        var response = new CollectionResponse<string> { Items = items };

        // Act
        var totalItems = response.TotalItems;

        // Assert
        totalItems.ShouldBe(3);
    }

    [Fact(DisplayName = "TotalItems should return zero when collection is empty")]
    public void TotalItems_ShouldReturnZero_WhenCollectionIsEmpty()
    {
        // Arrange
        var response = new CollectionResponse<int> { Items = [] };

        // Act
        var totalItems = response.TotalItems;

        // Assert
        totalItems.ShouldBe(0);
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