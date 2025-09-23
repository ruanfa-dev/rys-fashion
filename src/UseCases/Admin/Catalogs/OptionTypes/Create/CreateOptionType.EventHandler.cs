using Core.Catalogs;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Catalogs.OptionTypes.Create;
public static partial class CreateOptionType
{
    public sealed class EventHandler : IDomainEventHandler<OptionType.Events.Created>
    {
        public Task Handle(OptionType.Events.Created notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
