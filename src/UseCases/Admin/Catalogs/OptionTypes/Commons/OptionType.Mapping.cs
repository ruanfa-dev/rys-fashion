using Core.Catalogs;

using Mapster;

namespace UseCases.Admin.Catalogs.OptionTypes.Commons;

public sealed class OptionTypeMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<OptionType, OptionTypeResult>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Presentation, src => src.Presentation)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.CreatedBy, src => src.CreatedBy)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Map(dest => dest.UpdatedBy, src => src.UpdatedBy)
            .Map(dest => dest.Position, src => src.Position);

        config.NewConfig<OptionType, OptionTypeListItemResult>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Presentation, src => src.Presentation)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Map(dest => dest.Position, src => src.Position)
            .Map(dest => dest.OptionValuesCount, src => src.OptionValues.Count)
            .Map(dest => dest.PrototypesCount, src => src.PrototypeOptionTypes.Count)
            .Map(dest => dest.ProductsCount, src => src.ProductOptionTypes.Count);

        config.NewConfig<OptionType, OptionTypeSelectItemResult>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Presentation, src => src.Presentation)
            .Map(dest => dest.Position, src => src.Position);
    }
}
