
using Core.Catalogs;

using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Catalogs.OptionValues.Delete;
public partial class DeleteOptionValue
{
    public class EventHandler(ILogger<DeleteOptionValue.EventHandler> logger) : IDomainEventHandler<OptionValue.Events.Deleted>
    {

        public Task Handle(OptionValue.Events.Deleted notification, CancellationToken cancellationToken)
        {
            logger.LogInformation("OptionValue with ID '{OptionValueId}' has been deleted.", notification.OptionValueId);
            // Additional side effects can be handled here
            return Task.CompletedTask;
        }
    }
}
