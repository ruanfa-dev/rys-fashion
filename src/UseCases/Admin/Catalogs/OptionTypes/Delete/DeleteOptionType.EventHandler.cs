
using Core.Catalogs;

using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Catalogs.OptionTypes.Delete;
public partial class DeleteOptionType
{
    public class EventHandler(ILogger<DeleteOptionType.EventHandler> logger) : IDomainEventHandler<OptionType.Events.Deleted>
    {

        public Task Handle(OptionType.Events.Deleted notification, CancellationToken cancellationToken)
        {
            logger.LogInformation("OptionType with ID '{OptionTypeId}' has been deleted.", notification.OptionTypeId);
            // Additional side effects can be handled here
            return Task.CompletedTask;
        }
    }
}
