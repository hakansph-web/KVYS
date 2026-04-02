namespace KVYS.Shared.Application.Abstractions;

/// <summary>
/// Unit of work interface for coordinating changes across repositories.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
