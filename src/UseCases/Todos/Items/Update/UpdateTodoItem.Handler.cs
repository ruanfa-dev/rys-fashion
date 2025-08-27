using Core.Todos;

using ErrorOr;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Persistence.Context;
using UseCases.Todos.Items.Common;

namespace UseCases.Todos.Items.Update;

public static partial class UpdateTodoItem
{
    public record Command(int Id, TodoItemParam Param) : ICommand<Updated>;
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Param)
                .SetValidator(new TodoItemParamValidator());
        }
    }

    internal sealed class Handler(IUnitOfWork unitOfWork) : ICommandHandler<Command, Updated>
    {
        public async Task<ErrorOr<Updated>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Check: if the TodoItem exists
            var todoItem = await unitOfWork.Context.TodoItems
                .Include(m => m.List)
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (todoItem == null)
                return TodoItem.Errors.TodoItemNotFound;

            // Update: the todo item
            TodoItemParam param = request.Param;
            todoItem.ListId = param.ListId;
            todoItem.Title = param.Title;
            todoItem.Note = param.Note;
            todoItem.Priority = param.Priority ?? PriorityLevel.Low;
            todoItem.Reminder = param.Reminder;

            // Raise: todo item event updated
            todoItem.AddDomainEvent(new TodoItemCompletedEvent(todoItem));

            // Save: todo item
            unitOfWork.Context.TodoItems.Update(todoItem);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Updated;
        }
    }

}
