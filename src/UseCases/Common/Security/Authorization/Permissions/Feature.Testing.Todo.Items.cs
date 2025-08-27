namespace UseCases.Common.Security.Authorization.Permissions;
public static partial class Feature
{
    public static partial class Testing
    {
        public static partial class Todo
        {
            public static class Items
            {
                public const string Create = "todo.items.create";
                public const string List = "todo.items.list";
                public const string View = "todo.items.view";
                public const string Update = "todo.items.update";
                public const string Delete = "todo.items.delete";
                public const string Track = "todo.items.track";
                public static readonly string[] All =
                [
                    Create,
                    List,
                    View,
                    Update,
                    Delete,
                    Track
                ];
                public static readonly string[] ReadOnly =
                [
                    List,
                    View
                ];
                public static readonly string[] WriteOnly =
                [
                    Create,
                    Update,
                    Delete,
                    Track
                ];
            }
        }
    }
}
