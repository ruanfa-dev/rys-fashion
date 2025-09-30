using System.Reflection;

using Core.Catalog.Properties;
using Core.Catalog.Taxonomies;
using Core.Identity.Permissions;
using Core.Identity.Roles;
using Core.Identity.Tokens;
using Core.Identity.Users;
using Core.Todos;

using Infrastructure.Persistence.Constants;
using Infrastructure.Persistence.Converters;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using UseCases.Common.Persistence.Context;
using Infrastructure.Persistence.Configurations.Common;
using SharedKernel.Domain.Attributes.Metadata;

namespace Infrastructure.Persistence.Contexts;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<
        User, Role, Guid,
        UserClaim, UserRole, IdentityUserLogin<Guid>,
        RoleClaim, IdentityUserToken<Guid>>(options),
      IApplicationDbContext
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(Schema.Default);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        builder.ApplyUtcDateTimeConverter();
        builder.ApplyMetadataSupportConversions();
        builder.ApplyTranslationEntityConfigurations();
    }

    // Define DbSets for your entities here
    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<TodoList> TodoLists { get; set; } = null!;
    public DbSet<TodoItem> TodoItems { get; set; } = null!;

    // Catalog
    public DbSet<Property> Properties { get; set; } = null!;

    // Taxonomies
    public DbSet<Taxonomy> Taxonomies { get; set; } = null!;
    public DbSet<Taxon> Taxons { get; set; } = null!;
    public DbSet<TaxonomyTranslation> TaxonomyTranslations { get; set; } = null!;
}