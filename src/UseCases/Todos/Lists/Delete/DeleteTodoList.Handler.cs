using Core.Todos;

using ErrorOr;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Persistence.Context;

namespace UseCases.Todos.Lists.Delete;

public static partial class DeleteTodoList
{
    public record Command(int Id) : ICommand<Deleted>;

    internal sealed class Handler(IUnitOfWork unitOfWork)
        : ICommandHandler<Command, Deleted>
    {
        public async Task<ErrorOr<Deleted>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Start: transaction
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            // Check: todo list exists
            TodoList? todoList = await unitOfWork.Context.TodoLists
                .Include(m => m.Items)
                .SingleOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (todoList == null)
                return TodoList.Errors.TodoListNotFound;

            // Remove: all items in the lists
            var items = todoList.Items;
            foreach (var item in items)
            {
                item.AddDomainEvent(new TodoItemDeletedEvent(item));
                unitOfWork.Context.TodoItems.Remove(item);
            }

            // Raise: todo list deleted
            todoList.AddDomainEvent(new TodoListDeletedEvent(todoList));

            // Delete: the todo list
            unitOfWork.Context.TodoLists.Remove(todoList);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            return Result.Deleted;
        }
    }
}
