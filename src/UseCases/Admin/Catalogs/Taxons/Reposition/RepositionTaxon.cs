using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Catalogs.Taxons.Commons;

namespace UseCases.Admin.Catalogs.Taxons.Reposition;

public partial class RepositionTaxon
{
    public const string Name = "RepositionTaxon";
    public const string Summary = "Reposition taxon";
    public const string Description = "Change a taxon's parent and position among siblings";

}
