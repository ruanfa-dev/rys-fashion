using Core.Todos;

using ErrorOr;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Persistence.Context;

namespace UseCases.Todos.Items.Delete;

public partial class DeleteTodoItem
{
    public record Command(int Id) : ICommand<Deleted>;

    internal sealed class Handler(IApplicationDbContext context)
    : ICommandHandler<Command, Deleted>
    {
        public async Task<ErrorOr<Deleted>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Check: Todo item existence
            TodoItem? todoItem = await context.TodoItems
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (todoItem == null)
                return TodoItem.Errors.TodoItemNotFound;

            // Raise: todo item deleted
            todoItem.AddDomainEvent(new TodoItemDeletedEvent(todoItem));

            // Delete: the todo item
            context.TodoItems.Remove(todoItem);
            await context.SaveChangesAsync(cancellationToken);

            return Result.Deleted;
        }
    }

}
