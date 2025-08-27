using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using SharedKernel.Domain.Attributes;

namespace Infrastructure.Persistence.Interceptors;

internal class DispatchDomainEventsInterceptor(IMediator mediator) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        DispatchDomainEvents(eventData.Context).GetAwaiter().GetResult();
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        await DispatchDomainEvents(eventData.Context, cancellationToken);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchDomainEvents(DbContext? context, CancellationToken cancellationToken = default)
    {
        if (context == null) return;

        // Get all entities with domain events
        var entitiesWithDomainEvents = context.ChangeTracker
            .Entries<IHasDomainEvent>()
            .Where(e => e.Entity.GetDomainEvents().Count != 0)
            .Select(e => e.Entity)
            .ToList();

        if (entitiesWithDomainEvents.Count == 0) return;

        // Get all domain events
        var domainEvents = entitiesWithDomainEvents
            .SelectMany(e => e.GetDomainEvents())
            .ToList();

        // Clear domain events after dispatch
        entitiesWithDomainEvents.ForEach(e => e.ClearDomainEvents());

        // Dispatch each domain event asynchronously
        foreach (var domainEvent in domainEvents)
        {
            await mediator.Publish(domainEvent, cancellationToken);
        }
    }
}
