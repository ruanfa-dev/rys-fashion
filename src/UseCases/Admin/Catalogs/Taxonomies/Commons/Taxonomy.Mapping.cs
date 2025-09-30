using Core.Catalog.Taxonomies;

using Mapster;

namespace UseCases.Admin.Catalogs.Taxonomies.Commons;

public sealed class TaxonomyMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // List
        config.NewConfig<Taxonomy, TaxonomyResult.ListItem>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Position, src => src.Position)
            .Map(dest => dest.StoreId, src => src.StoreId)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt);

        // Details
        config.NewConfig<Taxonomy, TaxonomyResult.Details>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Position, src => src.Position)
            .Map(dest => dest.StoreId, src => src.StoreId)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Map(dest => dest.CreatedBy, src => src.CreatedBy)
            .Map(dest => dest.UpdatedBy, src => src.UpdatedBy)
            .Map(dest => dest.PublicMetadata, src => src.PublicMetadata)
            .Map(dest => dest.PrivateMetadata, src => src.PrivateMetadata);

        // ComboItem
        config.NewConfig<Taxonomy, TaxonomyResult.ComboItem>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name);
    }
}