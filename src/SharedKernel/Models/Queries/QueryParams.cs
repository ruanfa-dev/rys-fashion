using SharedKernel.Models.Filter;
using SharedKernel.Models.Paging;
using SharedKernel.Models.Search;
using SharedKernel.Models.Sort;

namespace SharedKernel.Models.Queries;

public record QueryParams
{
    public SearchParams Search { get; set; } = new();
    public QueryFilterParams Filter { get; set; } = new();
    public SortParams Sort { get; set; } = new();
    public PagingParams Pagination { get; set; } = new();
}
