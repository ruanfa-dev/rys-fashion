using Core.Todos;

using ErrorOr;

using FluentValidation;

using Mapster;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Persistence.Context;
using UseCases.Todos.Lists.Common;

namespace UseCases.Todos.Lists.Create;

public static partial class CreateTodoList
{
    public record Command(TodoListParam Param) : ICommand<TodoListResult>;
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Param)
                .SetValidator(new TodoListParamValidator());
        }
    }
    internal sealed class Handler(IUnitOfWork unitOfWork) : ICommandHandler<Command, TodoListResult>
    {
        public async Task<ErrorOr<TodoListResult>> Handle(Command request, CancellationToken cancellationToken)
        {
            var param = request.Param;
            // Validate: colour is supported
            var colourOrError = Colour.Create(param.Colour);
            if (colourOrError.IsError)
                return colourOrError.Errors;

            // Check: todo title duplicate
            var existing = await unitOfWork.Context.TodoLists
                .AsNoTracking()
                .AnyAsync(t => t.Title == request.Param.Title, cancellationToken);

            if (existing)
                return TodoList.Errors.TodoListAlreadyExists(param.Title);

            // Create: todo list
            TodoList todoList = TodoList.Create(
                title: param.Title,
                colour: colourOrError.Value);

            // Save: changes
            await unitOfWork.Context.TodoLists.AddAsync(todoList, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return todoList.Adapt<TodoListResult>();
        }
    }
}
