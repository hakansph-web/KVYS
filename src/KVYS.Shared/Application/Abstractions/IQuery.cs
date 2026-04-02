using MediatR;

namespace KVYS.Shared.Application.Abstractions;

/// <summary>
/// Marker interface for queries (read operations).
/// </summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}

/// <summary>
/// Handler for queries.
/// </summary>
public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}
