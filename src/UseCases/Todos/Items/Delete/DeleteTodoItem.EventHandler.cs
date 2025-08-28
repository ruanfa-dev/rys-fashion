using Core.Todos;

using Serilog;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Todos.Items.Delete;

public partial class DeleteTodoItem
{
    internal class EventHandler : IDomainEventHandler<TodoItemDeletedEvent>
    {
        public async Task Handle(TodoItemDeletedEvent notification, CancellationToken cancellationToken)
        {
            Log.Information("Stellar Domain Event: {DomainEvent}", notification.GetType().Name);
            await Task.CompletedTask;
        }
    }
}
