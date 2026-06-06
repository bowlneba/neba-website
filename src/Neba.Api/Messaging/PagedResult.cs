namespace Neba.Api.Messaging;

/// <summary>
/// Represents a paginated result set returned from a query handler.
/// </summary>
/// <typeparam name="T">The type of items in the result.</typeparam>
/// <param name="Items">The items on the current page.</param>
/// <param name="TotalItems">The total number of items across all pages.</param>
public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int TotalItems);