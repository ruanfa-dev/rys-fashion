using ErrorOr;

using MediatR;

namespace SharedKernel.Messaging.Abstracts;

/// <summary>
/// Marker interface for base command types.
/// </summary>
public interface IBaseCommand;

/// <summary>
/// Represents a command that returns a result of type <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TResult">The type of the result returned by the command.</typeparam>
public interface ICommand<TResult> : IRequest<ErrorOr<TResult>>, IBaseCommand;

/// <summary>
/// Represents a command that does not return a result.
/// </summary>
public interface ICommand : IRequest<ErrorOr<Unit>>, IBaseCommand;

/// <summary>
/// Defines a handler for a command that does not return a result.
/// </summary>
/// <typeparam name="TCommand">The type of the command being handled.</typeparam>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, ErrorOr<Unit>>
    where TCommand : ICommand;

/// <summary>
/// Defines a handler for a command that returns a result of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TCommand">The type of the command being handled.</typeparam>
/// <typeparam name="TResponse">The type of the result returned by the command.</typeparam>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, ErrorOr<TResponse>>
    where TCommand : ICommand<TResponse>;
