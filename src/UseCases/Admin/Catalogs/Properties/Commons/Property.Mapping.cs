using Core.Catalog.Properties;

using Mapster;

namespace UseCases.Admin.Catalogs.Properties.Commons;

public sealed class PropertyMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // List
        config.NewConfig<Property, PropertyResult.ListItem>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Presentation, src => src.Presentation)
            .Map(dest => dest.Kind, src => src.Kind)
            .Map(dest => dest.DisplayOn, src => src.DisplayOn)
            .Map(dest => dest.Filterable, src => src.Filterable)
            .Map(dest => dest.Position, src => src.Position)
            .Map(dest => dest.PublicMetadata, src => src.PublicMetadata)
            .Map(dest => dest.PrivateMetadata, src => src.PrivateMetadata)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Map(dest => dest.ProductsCount, src => src.Products.Count())
            .Map(dest => dest.PrototypeCount, src => src.Prototypes.Count())
            .IgnoreNullValues(true);
      
        // Details
        config.NewConfig<Property, PropertyResult.Details>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Presentation, src => src.Presentation)
            .Map(dest => dest.Kind, src => src.Kind)
            .Map(dest => dest.DisplayOn, src => src.DisplayOn)
            .Map(dest => dest.Filterable, src => src.Filterable)
            .Map(dest => dest.Position, src => src.Position)
            .Map(dest => dest.PublicMetadata, src => src.PublicMetadata)
            .Map(dest => dest.PrivateMetadata, src => src.PrivateMetadata)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Map(dest => dest.CreatedBy, src => src.CreatedBy)
            .Map(dest => dest.UpdatedBy, src => src.UpdatedBy)
            .IgnoreNullValues(true);
            
        // Combo
        config.NewConfig<Property, PropertyResult.ComboItem>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Presentation, src => src.Presentation);
    }
}
