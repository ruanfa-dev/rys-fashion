using Core.Catalog.Properties;

using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Catalogs.Properties.Delete;
public static partial class DeleteProperty
{
    public class EventHandler(ILogger<EventHandler> logger) : IDomainEventHandler<Property.Events.Deleted>
    {
        public Task Handle(Property.Events.Deleted notification, CancellationToken cancellationToken)
        {
            logger.LogInformation("Property with ID '{PropertyId}' has been deleted.", notification.PropertyId);
            // Additional side effects can be handled here
            return Task.CompletedTask;
        }
    }
}
