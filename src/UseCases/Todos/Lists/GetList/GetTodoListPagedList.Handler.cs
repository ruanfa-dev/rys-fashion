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
    public sealed record Param : QueryParams;
    public sealed record Result : TodoListResult;
    public sealed record Query(Param Param): IQuery<PagedList<Result>>;

    internal sealed class Handler(IApplicationDbContext context, IMapper mapper)
        : IQueryHandler<Query, PagedList<Result>>
    {
        public async Task<ErrorOr<PagedList<Result>>> Handle(Query query, CancellationToken cancellationToken)
        {
            Param param = query.Param;
            PagedList<Result> paginatedList = await context.TodoLists
              .AsQueryable()
              .AsNoTracking()
              .ApplyFilters(param.Filter)
              .ApplySearch(param.Search)
              .ApplySort(param.Sort)
              .ProjectToType<Result>(mapper.Config)
              .ToPagedListAsync(
                  param.Paging,
                  cancellationToken: cancellationToken);

            return paginatedList;
        }
    }
}
