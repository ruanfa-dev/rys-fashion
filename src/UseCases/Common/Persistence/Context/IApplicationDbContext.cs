using Core.Identity;
using Core.Todos;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace UseCases.Common.Persistence.Context;

/// <summary>
/// Application database context interface - Updated for Keycloak migration
/// Users and roles are now managed in Keycloak instead of EF Core Identity
/// </summary>
public interface IApplicationDbContext
{
    // Generic operations
    DbSet<T> Set<T>() where T : class;

    // Core application entities
    DbSet<TodoItem> TodoItems { get; }
    DbSet<TodoList> TodoLists { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Permission> Permissions { get; }

    // Legacy entities (kept for migration purposes)
    // These will be removed after complete migration to Keycloak
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }

    // Database operations
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    DatabaseFacade Database { get; }
}
