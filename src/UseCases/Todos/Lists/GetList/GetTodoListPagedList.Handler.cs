using ErrorOr;

using Mapster;

using MapsterMapper;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Messaging.Abstracts;
using SharedKernel.Models.Filter;
using SharedKernel.Models.PagedLists;
using SharedKernel.Models.Queries;
using SharedKernel.Models.Search;
using SharedKernel.Models.Sort;

using UseCases.Common.Persistence.Context;
using UseCases.Todos.Lists.Common;

namespace UseCases.Todos.Lists.GetList;

public static partial class GetTodoListPagedList
{
    public sealed record Query : QueryParams, IQuery<PagedList<TodoListResult>>;

    internal sealed class Handler(IApplicationDbContext context, IMapper mapper)
        : IQueryHandler<Query, PagedList<TodoListResult>>
    {
        public async Task<ErrorOr<PagedList<TodoListResult>>> Handle(Query param, CancellationToken cancellationToken)
        {
            var paginatedList = await context.TodoLists
              .AsQueryable()
              .AsNoTracking()
              .ApplyFilters(param.Filter)
              .ApplySearch(param.Search)
              .ApplySort(param.Sort)
              .ProjectToType<TodoListResult>(mapper.Config)
              .ToPagedListAsync(
                  param.Pagination,
                  cancellationToken: cancellationToken);

            return paginatedList;
        }
    }
}
