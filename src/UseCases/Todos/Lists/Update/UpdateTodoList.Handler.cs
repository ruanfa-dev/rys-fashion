using Core.Todos;

using ErrorOr;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Persistence.Context;
using UseCases.Todos.Lists.Common;

namespace UseCases.Todos.Lists.Update;

public static partial class UpdateTodoList
{
    public record Command(int Id, TodoListParam Param) : ICommand<Updated>;

    internal sealed class Handler(IUnitOfWork unitOfWork)
     : ICommandHandler<Command, Updated>
    {
        public async Task<ErrorOr<Updated>> Handle(Command request, CancellationToken cancellationToken)
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);
            var param = request.Param;
            // Check: todo list existing
            TodoList? todoList = await unitOfWork.Context.TodoLists
               .SingleOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
            if (todoList == null)
                return TodoList.Errors.TodoListNotFound;

            // Check: duplicate title
            var duplicateTittle = await unitOfWork.Context.TodoLists
                .AnyAsync(m => m.Title == todoList.Title && m.Id != request.Id, cancellationToken: cancellationToken);
            if (duplicateTittle)
                return TodoList.Errors.TodoListAlreadyExists(param.Title);

            // Check: colors
            var createColorResult = Colour.Create(param.Colour);
            if (createColorResult.IsError) return createColorResult.Errors;

            // Update: todo list
            todoList.Title = param.Title;
            todoList.Colour = createColorResult.Value;

            // Raise: todo list updated
            todoList.AddDomainEvent(new TodoListUpdatedEvent(todoList));

            unitOfWork.Context.TodoLists.Update(todoList);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            return Result.Updated;
        }
    }
}
