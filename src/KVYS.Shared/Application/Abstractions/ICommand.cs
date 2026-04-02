using MediatR;

namespace KVYS.Shared.Application.Abstractions;

/// <summary>
/// Marker interface for commands (write operations).
/// </summary>
public interface ICommand : IRequest<Result>
{
}

/// <summary>
/// Command with a return value.
/// </summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}

/// <summary>
/// Handler for commands without return value.
/// </summary>
public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand
{
}

/// <summary>
/// Handler for commands with return value.
/// </summary>
public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}
