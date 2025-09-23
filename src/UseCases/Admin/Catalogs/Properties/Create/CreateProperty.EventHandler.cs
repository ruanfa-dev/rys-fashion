using Core.Catalogs;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Catalogs.Properties.Create;
public static partial class CreateProperty
{
    public sealed class EventHandler : IDomainEventHandler<Property.Events.Created>
    {
        public Task Handle(Property.Events.Created notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
