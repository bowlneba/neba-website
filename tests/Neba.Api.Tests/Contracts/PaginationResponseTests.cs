using Neba.Api.Contracts;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Contracts;

[UnitTest]
[Component("Api.Contracts")]
public sealed class PaginationResponseTests
{
    [Fact(DisplayName = "PaginationResponse should be assignable to CollectionResponse")]
    public void PaginationResponse_ShouldBeAssignableToCollectionResponse()
    {
        // Arrange & Act
        var response = new PaginationResponse<string>
        {
            Items = ["a", "b"],
            PageNumber = 1,
            PageSize = 10
        };

        // Assert
        response.ShouldBeAssignableTo<CollectionResponse<string>>();
    }

    [Theory(DisplayName = "TotalPages should calculate correctly")]
    [InlineData(10, 5, 2, TestDisplayName = "10 items with page size 5 yields 2 pages")]
    [InlineData(10, 10, 1, TestDisplayName = "10 items with page size 10 yields 1 page")]
    [InlineData(11, 5, 3, TestDisplayName = "11 items with page size 5 yields 3 pages (rounds up)")]
    [InlineData(0, 10, 0, TestDisplayName = "0 items yields 0 pages")]
    [InlineData(1, 10, 1, TestDisplayName = "1 item with page size 10 yields 1 page")]
    public void TotalPages_ShouldCalculateCorrectly(int totalItems, int pageSize, int expectedPages)
    {
        // Arrange
        var items = Enumerable.Range(0, totalItems).ToList();
        var response = new PaginationResponse<int>
        {
            Items = items,
            PageNumber = 1,
            PageSize = pageSize
        };

        // Act
        var totalPages = response.TotalPages;

        // Assert
        totalPages.ShouldBe(expectedPages);
    }

    [Theory(DisplayName = "HasPreviousPage should return correct value")]
    [InlineData(1, false, TestDisplayName = "Page 1 has no previous page")]
    [InlineData(2, true, TestDisplayName = "Page 2 has previous page")]
    [InlineData(5, true, TestDisplayName = "Page 5 has previous page")]
    public void HasPreviousPage_ShouldReturnCorrectValue(int pageNumber, bool expected)
    {
        // Arrange
        var response = new PaginationResponse<string>
        {
            Items = ["a", "b", "c", "d", "e"],
            PageNumber = pageNumber,
            PageSize = 1
        };

        // Act
        var hasPreviousPage = response.HasPreviousPage;

        // Assert
        hasPreviousPage.ShouldBe(expected);
    }

    [Theory(DisplayName = "HasNextPage should return correct value")]
    [InlineData(1, 5, true, TestDisplayName = "Page 1 of 5 has next page")]
    [InlineData(5, 5, false, TestDisplayName = "Page 5 of 5 has no next page")]
    [InlineData(3, 5, true, TestDisplayName = "Page 3 of 5 has next page")]
    [InlineData(1, 1, false, TestDisplayName = "Single page has no next page")]
    public void HasNextPage_ShouldReturnCorrectValue(int pageNumber, int totalItems, bool expected)
    {
        // Arrange
        var items = Enumerable.Range(0, totalItems).ToList();
        var response = new PaginationResponse<int>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = 1
        };

        // Act
        var hasNextPage = response.HasNextPage;

        // Assert
        hasNextPage.ShouldBe(expected);
    }

    [Fact(DisplayName = "TotalCount should return items count (inherited from CollectionResponse)")]
    public void TotalCount_ShouldReturnItemsCount_WhenInheritedFromCollectionResponse()
    {
        // Arrange
        var items = new[] { "a", "b", "c" };
        var response = new PaginationResponse<string>
        {
            Items = items,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var totalCount = response.TotalCount;

        // Assert
        totalCount.ShouldBe(3);
    }
}
