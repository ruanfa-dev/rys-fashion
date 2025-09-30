using SharedKernel.Models.PagedLists;
using SharedKernel.Models.Queries;
using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Catalogs.Taxonomies.Get.OptionList;
public partial class GetTaxonomyOptionList
{
    public const string Name = "GetTaxonomyOptionList";
    public const string Summary = "Get taxonomy option list";
    public const string Description = "Get a lightweight option list of taxonomies for selects";
    public sealed record Param : QueryParams;
    public sealed record Result : Commons.TaxonomyResult.ListItem;
    public sealed record Query(Param Param) : IQuery<PagedList<Result>>;
}
