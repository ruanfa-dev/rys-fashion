using Core.Todos;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace UseCases.Common.Persistence.Context;

public interface IApplicationDbContext
{
    // Fall back to DbContext for generic operations
    DbSet<T> Set<T>() where T : class;

    // Testin purpose only
    DbSet<TodoItem> TodoItems { get; }
    DbSet<TodoList> TodoLists { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    DatabaseFacade Database { get; }
}
