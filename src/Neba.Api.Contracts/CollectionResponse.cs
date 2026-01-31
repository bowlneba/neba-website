namespace Neba.Api.Contracts;

/// <inheritdoc />
public sealed record CollectionResponse<T>
    : ICollectionResponse<T>
{
    /// <inheritdoc />
    public required IReadOnlyCollection<T> Items { get; init; }

    /// <inheritdoc />
    public int TotalItems
        => Items.Count;
}