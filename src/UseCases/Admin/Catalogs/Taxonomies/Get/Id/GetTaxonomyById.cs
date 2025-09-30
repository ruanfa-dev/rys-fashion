namespace UseCases.Admin.Catalogs.Taxonomies.Get.Id;
public partial class GetTaxonomyById
{
    public const string Name = "GetTaxonomyById";
    public const string Summary = "Get taxonomy by id";
    public const string Description = "Retrieve taxonomy details by identifier";

    public  sealed record Result : Commons.TaxonomyResult.Details;

    public sealed record Query(Guid Id) : SharedKernel.Messaging.Abstracts.IQuery<Result>;
}
