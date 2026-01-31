using ErrorOr;

namespace Neba.Application.Messaging;

/// <summary>
/// Represents a command that returns a response of type <typeparamref name="TResponse"/>.
/// Used in the CQRS pattern to encapsulate a request for an action to be performed.
/// </summary>
/// <typeparam name="TResponse">The type of the response returned by the command.</typeparam>
public interface ICommand<out TResponse>;

/// <summary>
/// Represents a command that does not return any data upon completion.
/// Used in the CQRS pattern to encapsulate a request for an action to be performed.
/// </summary>
public interface ICommand
    : ICommand<Success>;