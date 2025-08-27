using Core.Todos;

using ErrorOr;

using MapsterMapper;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Persistence.Context;
using UseCases.Todos.Lists.Common;

namespace UseCases.Todos.Lists.GetById;

public partial class GetTodoListById
{
    public sealed record Query(int Id) : IQuery<TodoListResult>;

    internal sealed class Handler(IApplicationDbContext context, IMapper mapper)
        : IQueryHandler<Query, TodoListResult>
    {
        public async Task<ErrorOr<TodoListResult>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Check: todo list existence
            var todoList = await context.TodoLists
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (todoList == null)
                return TodoList.Errors.TodoListNotFound;

            var todoListResult = mapper.Map<TodoListResult>(todoList);

            return todoListResult;
        }
    }
}
