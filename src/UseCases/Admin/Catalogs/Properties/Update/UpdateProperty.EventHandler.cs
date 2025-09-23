using Core.Catalogs;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Catalogs.Properties.Update;
public partial class UpdateProperty
{
    public class EventHandler : IDomainEventHandler<Property.Events.Updated>
    {
        public Task Handle(Property.Events.Updated domainEvent, CancellationToken cancellationToken)
        {
            // Implement any side effects or notifications here
            return Task.CompletedTask;
        }
    }
}
