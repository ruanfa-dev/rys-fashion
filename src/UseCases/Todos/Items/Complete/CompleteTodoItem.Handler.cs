using Core.Todos;

using ErrorOr;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Persistence.Context;

namespace UseCases.Todos.Items.Complete;

public static partial class CompleteTodoItem
{
    public sealed record Command(int Id) : ICommand<Updated>;

    public sealed class Handler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork)
        : ICommandHandler<Command, Updated>
    {
        public async Task<ErrorOr<Updated>> Handle(Command request, CancellationToken cancellationToken)
        {

            // Check: Todo item existence
            var todoItem = await context.TodoItems
                .Include(m => m.List)
                .SingleOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (todoItem is null)
                return TodoItem.Errors.TodoItemNotFound;

            // Guard: check if the todo item already done
            var result = todoItem.MarkAsDone();
            if (result.IsError)
                return TodoItem.Errors.TodoItemAlreadyCompleted;

            // Publish: todo item completed
            todoItem.AddDomainEvent(new TodoItemCompletedEvent(todoItem));
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Updated;
        }
    }
}
