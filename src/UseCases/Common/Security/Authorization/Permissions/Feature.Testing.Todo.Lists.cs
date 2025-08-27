namespace UseCases.Common.Security.Authorization.Permissions;
public static partial class Feature
{
    public static partial class Testing
    {
        public static partial class Todo
        {
            public static class Lists
            {
                public const string Create = "todo.lists.create";
                public const string List = "todo.lists.list";
                public const string View = "todo.lists.view";
                public const string Update = "todo.lists.update";
                public const string Delete = "todo.lists.delete";

                public static readonly string[] All =
                [
                    Create,
                    List,
                    View,
                    Update,
                    Delete
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
                    Delete
                ];
            }
        }
    }
}
