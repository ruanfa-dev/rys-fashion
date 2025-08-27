using Core.Todos;

using Serilog;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Todos.Items.Create;

public partial class CreateTodoItem
{
    public sealed class EventHandler : IDomainEventHandler<TodoItemCreatedEvent>
    {
        public async Task Handle(TodoItemCreatedEvent notification, CancellationToken cancellationToken)
        {
            Log.Information("Domain Event: {DomainEvent}", notification.GetType().Name);
            await Task.CompletedTask;
        }
    }
}
