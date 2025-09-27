using Core.Catalog.Properties;
using Core.Identity;
using Core.Identity.Permissions;
using Core.Identity.Roles;
using Core.Identity.Tokens;
using Core.Identity.Users;
using Core.Todos;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace UseCases.Common.Persistence.Context;

public interface IApplicationDbContext
{
    // Fall back to DbContext for generic operations
    DbSet<T> Set<T>() where T : class;

    // Testing purpose only
    DbSet<TodoItem> TodoItems { get; }
    DbSet<TodoList> TodoLists { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<Permission> Permissions { get; }

    // Catalog
    DbSet<Property> Properties { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    DatabaseFacade Database { get; }
}
