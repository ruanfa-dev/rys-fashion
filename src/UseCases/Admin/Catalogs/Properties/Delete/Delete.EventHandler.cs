using Core.Catalog.Properties;

using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Catalogs.Properties.Delete;
public partial class DeleteProperty
{
    public class EventHandler(ILogger<DeleteProperty.EventHandler> logger) : IDomainEventHandler<Property.Events.Deleted>
    {
        private readonly ILogger<EventHandler> _logger = logger;

        public Task Handle(Property.Events.Deleted notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Property with ID '{PropertyId}' has been deleted.", notification.PropertyId);
            // Additional side effects can be handled here
            return Task.CompletedTask;
        }
    }
}
