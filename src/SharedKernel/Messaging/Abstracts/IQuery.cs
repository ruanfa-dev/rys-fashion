using ErrorOr;

using MediatR;

namespace SharedKernel.Messaging.Abstracts;
/// <summary>
/// Marker interface for base query types.
/// </summary>
public interface IBaseQuery;
/// <summary>
/// Represents a query that returns a result of type <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TResult">The type of the result returned by the query.</typeparam>
public interface IQuery<TResult> : IRequest<ErrorOr<TResult>>, IBaseQuery;
/// <summary>
/// Defines a handler for a query of type <typeparamref name="TQuery"/> that returns a response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TQuery">The type of the query being handled.</typeparam>
/// <typeparam name="TResponse">The type of the response returned by the handler.</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, ErrorOr<TResponse>>
    where TQuery : IQuery<TResponse>;
