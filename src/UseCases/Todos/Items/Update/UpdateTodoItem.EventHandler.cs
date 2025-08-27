using Core.Todos;

using Serilog;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Todos.Items.Update;

public static partial class UpdateTodoItem
{
    public class EventHandler : IDomainEventHandler<TodoItemUpdatedEvent>
    {
        public Task Handle(TodoItemUpdatedEvent notification, CancellationToken cancellationToken)
        {
            Log.Information("Domain Event: {DomainEvent}", notification.GetType().Name);
            return Task.CompletedTask;
        }
    }
}
