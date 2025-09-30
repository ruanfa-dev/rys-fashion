using SharedKernel.Models.PagedLists;
using SharedKernel.Models.Queries;
using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Catalogs.Taxonomies.Get.PagedList;
public partial class GetTaxonomyPagedList
{
    public const string Name = "GetTaxonomyPagedList";
    public const string Summary = "Get paged list of taxonomies";
    public const string Description = "Get a paged list of taxonomies with filtering and sorting options";
    public sealed record Param : QueryParams;
    public sealed record Result : Commons.TaxonomyResult.ListItem;
    public sealed record Query(Param Param) : IQuery<PagedList<Result>>;
}
