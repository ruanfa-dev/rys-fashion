using Core.Catalog.Options;

using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Catalogs.Options.Delete;
public static partial class DeleteOptionType
{
    public class EventHandler(ILogger<EventHandler> logger) : IDomainEventHandler<OptionType.Events.Deleted>
    {
        public Task Handle(OptionType.Events.Deleted notification, CancellationToken cancellationToken)
        {
            logger.LogInformation("Option type with ID '{OptionTypeId}' has been deleted.", notification.OptionTypeId);
            // Additional side effects can be handled here
            return Task.CompletedTask;
        }
    }
}
