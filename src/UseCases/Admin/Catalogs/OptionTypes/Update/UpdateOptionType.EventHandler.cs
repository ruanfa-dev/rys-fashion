using Core.Catalogs;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Catalogs.OptionTypes.Update;
public partial class UpdateOptionType
{
    public class EventHandler : IDomainEventHandler<OptionType.Events.Updated>
    {
        public Task Handle(OptionType.Events.Updated domainEvent, CancellationToken cancellationToken)
        {
            // Implement any side effects or notifications here
            return Task.CompletedTask;
        }
    }
}
