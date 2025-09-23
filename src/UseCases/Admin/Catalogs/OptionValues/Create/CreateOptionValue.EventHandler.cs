using Core.Catalogs;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Catalogs.OptionValues.Create;
public static partial class CreateOptionValue
{
    public sealed class EventHandler : IDomainEventHandler<OptionValue.Events.Created>
    {
        public Task Handle(OptionValue.Events.Created notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
