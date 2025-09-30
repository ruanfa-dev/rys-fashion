namespace UseCases.Admin.Catalogs.Taxonomies.Delete;
public partial class DeleteTaxonomy
{
    public const string Name = "DeleteTaxonomy";
    public const string Summary = "Delete taxonomy";
    public const string Description = "Delete an existing taxonomy";

    public sealed record Command(Guid Id) : SharedKernel.Messaging.Abstracts.ICommand<Deleted>;

    public sealed record Deleted(Guid Id);
}
