using Core.Todos;

using Mapster;

namespace UseCases.Todos.Lists.Common;

public class TodoListMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Item
        config.NewConfig<TodoList, TodoListSelectItemResult>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Title, src => src.Title);

        // List
        config.NewConfig<TodoList, TodoListResult>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Title, src => src.Title)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.CreatedBy, src => src.CreatedBy)
            .Map(dest => dest.Colour, src => src.Colour != null ? src.Colour.Code : string.Empty);

        // Detail
        config.NewConfig<TodoList, TodoResult>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Title, src => src.Title)
            .Map(dest => dest.Colour, src => src.Colour != null ? src.Colour.Code : string.Empty)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.CreatedBy, src => src.CreatedBy)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Map(dest => dest.UpdatedBy, src => src.UpdatedBy);
    }
}
