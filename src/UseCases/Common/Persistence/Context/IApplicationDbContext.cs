using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace UseCases.Common.Persistence.Context;

public interface IApplicationDbContext
{
    // Fall back to DbContext for generic operations
    DbSet<T> Set<T>() where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    DatabaseFacade Database { get; }
}
