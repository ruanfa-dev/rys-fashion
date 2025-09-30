using UseCases.Admin.Catalogs.Taxonomies.Commons;

namespace UseCases.Admin.Catalogs.Taxonomies.Update;
public partial class UpdateTaxonomy
{
    public const string Name = "UpdateTaxonomy";
    public const string Summary = "Update taxonomy";
    public const string Description = "Update an existing taxonomy";

    public sealed record Param : TaxonomyParam;
    public sealed record Result : TaxonomyResult.ListItem;
    public sealed record Command(Guid Id, Param Param) : SharedKernel.Messaging.Abstracts.ICommand<Result>;
}
