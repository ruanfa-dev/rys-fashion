using Core.Catalogs;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Catalogs.OptionValues.Update;
public partial class UpdateOptionValue
{
    public class EventHandler : IDomainEventHandler<OptionValue.Events.Updated>
    {
        public Task Handle(OptionValue.Events.Updated domainEvent, CancellationToken cancellationToken)
        {
            // Implement any side effects or notifications here
            return Task.CompletedTask;
        }
    }
}
