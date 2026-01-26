using ErrorOr;

namespace Neba.Application.Messaging;

/// <summary>
/// Represents a handler for processing commands of type <typeparamref name="TCommand"/>
/// Used in the CQRS pattern to separate command handling logic.
/// </summary>
/// <typeparam name="TCommand">The type of the command to be handled.</typeparam>
public interface ICommandHandler<in TCommand>
    : ICommandHandler<TCommand, Success>
    where TCommand : ICommand;

/// <summary>
/// Represents a handler for processing commands of type <typeparamref name="TCommand"/>
/// and returning a response of type <typeparamref name="TResponse"/>.
/// Used in the CQRS pattern to separate command handling logic.
/// </summary>
/// <typeparam name="TCommand">The type of the command to be handled.</typeparam>
/// <typeparam name="TResponse">The type of the response returned by the handler.</typeparam>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// Handles the specified command and returns a response.
    /// </summary>
    /// <param name="command">The command to be handled.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response.</returns>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    Task<ErrorOr<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}