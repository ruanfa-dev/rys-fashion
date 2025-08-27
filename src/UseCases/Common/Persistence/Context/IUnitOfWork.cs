
using System.Data;

namespace UseCases.Common.Persistence.Context;

public interface IUnitOfWork
{
    IApplicationDbContext Context { get; }

    // Transaction management
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    Task<int> ExecuteSqlAsync(string sql, params object[] parameters);
    Task<int> ExecuteSqlAsync(string sql, CancellationToken cancellationToken, params object[] parameters);
    bool HasActiveTransaction { get; }
    Guid? CurrentTransactionId { get; }
}