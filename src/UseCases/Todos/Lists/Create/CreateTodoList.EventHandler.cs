using Core.Todos;

using MediatR;

using Serilog;

namespace UseCases.Todos.Lists.Create;

public static partial class CreateTodoList
{
    public sealed class EventHandler : INotificationHandler<TodoListCreatedEvent>
    {
        public async Task Handle(TodoListCreatedEvent notification, CancellationToken cancellationToken)
        {
            Log.Information("Domain Event: {DomainEvent}", notification.GetType().Name);
            await Task.CompletedTask;
        }
    }
}
