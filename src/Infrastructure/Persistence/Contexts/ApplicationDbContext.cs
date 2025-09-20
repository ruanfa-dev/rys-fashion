using System.Reflection;

using Core.Identity;
using Core.Todos;

using Infrastructure.Persistence.Constants;
using Infrastructure.Persistence.Converters;

using Microsoft.EntityFrameworkCore;

using UseCases.Common.Persistence.Context;

namespace Infrastructure.Persistence.Contexts;

/// <summary>
/// Application database context - now uses Keycloak for authentication instead of EF Identity
/// </summary>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(Schema.Default);
        
        // Apply all entity configurations
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        
        // Apply UTC DateTime converter
        builder.ApplyUtcDateTimeConverter();
    }

    // Core application entities (kept for data persistence)
    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<TodoList> TodoLists { get; set; } = null!;
    public DbSet<TodoItem> TodoItems { get; set; } = null!;

    // Note: User and Role entities are now managed by Keycloak
    // These DbSets are kept for reference and migration purposes only
    // In production, you might want to remove these after data migration
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
}