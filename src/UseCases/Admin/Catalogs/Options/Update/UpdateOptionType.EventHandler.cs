using Core.Catalog.Options;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Catalogs.Options.Update;
public static partial class UpdateOptionType
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
