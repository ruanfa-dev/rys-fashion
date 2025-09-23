using Core.Catalogs;

using Mapster;

namespace UseCases.Admin.Catalogs.OptionValues.Commons;

public sealed class OptionValueMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<OptionValue, OptionValueResult>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Presentation, src => src.Presentation)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.CreatedBy, src => src.CreatedBy)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Map(dest => dest.UpdatedBy, src => src.UpdatedBy)
            .Map(dest => dest.Position, src => src.Position)
            .Map(dest => dest.OptionTypeId, src => src.OptionTypeId)
            .Map(dest => dest.OptionTypeName, src => src.OptionType != null ? src.OptionType.Name : null);

        config.NewConfig<OptionValue, OptionValueSelectItemResult>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Presentation, src => src.Presentation)
            .Map(dest => dest.Position, src => src.Position);
    }
}
