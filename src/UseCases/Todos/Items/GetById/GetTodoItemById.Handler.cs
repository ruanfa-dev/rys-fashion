using Core.Todos;

using ErrorOr;

using MapsterMapper;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Persistence.Context;
using UseCases.Todos.Items.Common;

namespace UseCases.Todos.Items.GetById;

public partial class GetTodoItemById
{
    public record Query(int Id) : IQuery<TodoItemResult>;

    internal sealed class Handler(IApplicationDbContext context, IMapper mapper)
    : IQueryHandler<Query, TodoItemResult>
    {
        public async Task<ErrorOr<TodoItemResult>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Check: todo item existing
            var todoItem = await context.TodoItems
                .Include(m => m.List)
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (todoItem == null)
                return TodoItem.Errors.TodoItemNotFound;

            // Map: item to detail result
            var todoItemResult = mapper.Map<TodoItemResult>(todoItem);

            return todoItemResult;
        }
    }
}
