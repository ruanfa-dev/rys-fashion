using Core.Todos;

using Serilog;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Todos.Lists.Update;

public static partial class UpdateTodoList
{
    internal class EventHandler : IDomainEventHandler<TodoListUpdatedEvent>
    {
        public async Task Handle(TodoListUpdatedEvent notification, CancellationToken cancellationToken)
        {
            Log.Information("Domain Event: {DomainEvent}", notification.GetType().Name);
            await Task.CompletedTask;
        }
    }
}
