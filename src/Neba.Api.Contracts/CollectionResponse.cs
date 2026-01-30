namespace Neba.Api.Contracts;

/// <summary>
/// Represents a paginated collection response.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
public record CollectionResponse<T>
{
    /// <summary>
    /// The items in the collection.
    /// </summary>
    public required IReadOnlyCollection<T> Items { get; init; }

    /// <summary>
    /// The total count of items available.
    /// </summary>
    public int TotalCount
        => Items.Count;
}