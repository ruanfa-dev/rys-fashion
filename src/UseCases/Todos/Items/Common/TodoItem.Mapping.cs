using Core.Todos;

using Mapster;

using UseCases.Todos.Lists.Common;

namespace UseCases.Todos.Items.Common;

public class TodoItemMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // List
        config.NewConfig<TodoItem, TodoItemListResult>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Title, src => src.Title)
            .Map(dest => dest.Note, src => src.Note)
            .Map(dest => dest.Done, src => src.Done)
            .Map(dest => dest.DoneAt, src => src.DoneAt)
            .Map(dest => dest.Priority, src => src.Priority)
            .Map(dest => dest.Reminder, src => src.Reminder)
            .Map(dest => dest.ListId, src => src.ListId);

        // Detail
        config.NewConfig<TodoItem, TodoItemResult>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Title, src => src.Title)
            .Map(dest => dest.Note, src => src.Note)
            .Map(dest => dest.Done, src => src.Done)
            .Map(dest => dest.DoneAt, src => src.DoneAt)
            .Map(dest => dest.Priority, src => src.Priority)
            .Map(dest => dest.Reminder, src => src.Reminder)
            .Map(dest => dest.ListId, src => src.ListId)
            .Map(dest => dest.List,
                 src => src.List != null
                 ? src.List.Adapt<TodoListSelectItemResult>()
                 : null);
    }
}
