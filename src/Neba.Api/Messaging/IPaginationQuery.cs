namespace Neba.Api.Messaging;

/// <summary>
/// Defines a contract for pagination queries, which typically include properties for the page number and page size to facilitate retrieving a specific subset of results from a larger dataset.
/// </summary>
public interface IPaginationQuery
{
    /// <summary>
    /// Gets the page number to retrieve. This is typically used in conjunction with the page size to determine which subset of results to return.
    /// </summary>
    int Page { get; init; }

    /// <summary>
    /// Gets the number of items to retrieve per page. This is typically used in conjunction with the page number to determine which subset of results to return.
    /// </summary>
    int PageSize { get; init; }
}