using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

using SharedKernel.Domain.Attributes;

using UseCases.Common.Security.Authentication.Contexts;

namespace Infrastructure.Persistence.Interceptors;

internal sealed class AuditableEntityInterceptor(IUserContext userContext)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context == null || !userContext.IsAuthenticated)
            return;

        var userString = userContext.UserId == null ? "System" : userContext.UserName;

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.MarkAsCreated(userString);
            }
            else if (entry.State == EntityState.Modified || HasChangedOwnedEntities(entry))
            {
                // Prevent updating Created fields
                entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
                entry.Property(nameof(IAuditable.CreatedBy)).IsModified = false;

                entry.Entity.MarkAsUpdated(userString);
            }
        }
    }
    private static bool HasChangedOwnedEntities(EntityEntry entry) =>
    entry.References.Any(r =>
        r.TargetEntry != null &&
        r.TargetEntry.Metadata.IsOwned() &&
        (r.TargetEntry.State == EntityState.Added || r.TargetEntry.State == EntityState.Modified));
}