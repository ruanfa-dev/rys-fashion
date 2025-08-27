using Core.Todos;

using MediatR;

using Serilog;

namespace UseCases.Todos.Lists.Delete;
public static partial class DeleteTodoList
{
    public sealed class EventHandler : INotificationHandler<TodoListDeletedEvent>
    {
        public async Task Handle(TodoListDeletedEvent notification, CancellationToken cancellationToken)
        {
            Log.Information("Domain Event: {DomainEvent}", notification.GetType().Name);
            await Task.CompletedTask;
        }
    }
}
