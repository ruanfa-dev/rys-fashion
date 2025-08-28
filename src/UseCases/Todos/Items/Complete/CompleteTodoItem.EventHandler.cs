using Core.Todos;

using Serilog;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Todos.Items.Complete;

public static partial class CompleteTodoItem
{
    internal class EventHandler : IDomainEventHandler<TodoItemCompletedEvent>
    {
        public async Task Handle(TodoItemCompletedEvent notification, CancellationToken cancellationToken)
        {
            Log.Information("Stellar Domain Event: {DomainEvent}", notification.GetType().Name);
            await Task.CompletedTask;
        }
    }
}
