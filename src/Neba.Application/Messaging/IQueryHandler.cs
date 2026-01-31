namespace Neba.Application.Messaging;

/// <summary>
/// Represents a handler for processing queries of type <typeparamref name="TQuery"/>
/// and returning a response of type <typeparamref name="TResponse"/>.
/// Used in the CQRS pattern to separate query handling logic.
/// </summary>
/// <typeparam name="TQuery">The type of the query to be handled.</typeparam>
/// <typeparam name="TResponse">The type of the response returned by the handler.</typeparam>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Handles the specified query and returns a response.
    /// </summary>
    /// <param name="query">The query to be handled.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response.</returns>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken);
}