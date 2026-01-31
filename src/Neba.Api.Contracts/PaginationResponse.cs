namespace Neba.Api.Contracts;

/// <summary>
/// Represents a paginated collection response.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
public sealed record PaginationResponse<T>
    : ICollectionResponse<T>
{
    /// <inheritdoc />
    public required IReadOnlyCollection<T> Items { get; init; }

    /// <inheritdoc />
    public required int TotalItems { get; init; }

    /// <summary>
    /// The current page number.
    /// </summary>
    /// <example>1</example>
    public required int PageNumber { get; init; }

    /// <summary>
    /// The size of each page.
    /// </summary>
    /// <example>10</example>
    public required int PageSize { get; init; }

    /// <summary>
    /// The total number of pages available.
    /// </summary>
    /// <example>5</example>
    public int TotalPages
        => PageSize <= 0
            ? 0
            : (int)Math.Ceiling(TotalItems / (double)PageSize);

    /// <summary>
    /// Indicates if there is a previous page.
    /// </summary>
    /// <example>true</example>
    public bool HasPreviousPage
        => PageNumber > 1;

    /// <summary>
    /// Indicates if there is a next page.
    /// </summary>
    /// <example>true</example>
    public bool HasNextPage
        => PageNumber < TotalPages;
}