using Core.Catalogs;

using Mapster;

namespace UseCases.Admin.Catalogs.Properties.Commons;

public sealed class PropertyMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Property, PropertyResult>();
        config.NewConfig<PropertyParam, Property>();
        config.NewConfig<Property, PropertyListItemResult>()
            .Map(dest => dest.ProductsCount, src => src.ProductProperties.Count)
            .Map(dest => dest.PrototypesCount, src => src.PrototypeProperties.Count);
    }
}
