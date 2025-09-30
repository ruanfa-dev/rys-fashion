using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Catalogs.Taxonomies.Commons;

namespace UseCases.Admin.Catalogs.Taxonomies.Create;

public static partial class CreateTaxonomy
{
    public const string Name = "CreateTaxonomy";
    public const string Summary = "Create taxonomy";
    public const string Description = "Create a new taxonomy";

    public sealed record Param : TaxonomyParam;
    public sealed record Result : TaxonomyResult.ListItem;
    public sealed record Command(Param Param) : ICommand<Result>;

}
