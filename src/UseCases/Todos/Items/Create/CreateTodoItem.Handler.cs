using Core.Todos;

using ErrorOr;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Persistence.Context;
using UseCases.Todos.Items.Common;

namespace UseCases.Todos.Items.Create;

public partial class CreateTodoItem
{
    public record Command(TodoItemParam Param) : ICommand<int>;
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Param)
                .SetValidator(new TodoItemParamValidator());
        }
    }
    internal sealed class Handler(IApplicationDbContext context) : ICommandHandler<Command, int>
    {
        public async Task<ErrorOr<int>> Handle(Command request, CancellationToken cancellationToken)
        {
            var param = request.Param;
            // Check: todo list exist
            var todoList = await context.TodoLists
                .FirstOrDefaultAsync(m => m.Id == param.ListId, cancellationToken: cancellationToken);
            if (todoList is null)
                return TodoList.Errors.TodoListNotFound;

            // Create: new todo item
            TodoItem todoItem = TodoItem.Create(
                param.Title,
                param.Note,
                param.Priority ?? PriorityLevel.Low,
                param.Reminder,
                todoList.Id);

            // Raise: new todo item events
            todoItem.AddDomainEvent(new TodoItemCreatedEvent(todoItem));

            // Save: new todo item
            context.TodoItems.Add(todoItem);
            await context.SaveChangesAsync(cancellationToken);

            return todoItem.Id;
        }
    }

}
