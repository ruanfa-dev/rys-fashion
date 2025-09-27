using Core.Identity;
using Core.Identity.Permissions;

namespace UseCases.Common.Security.Authorization.Permissions;
public static partial class Feature
{
    public static partial class Testing
    {
        public static partial class TodoItems
        {
            public static Permission Create => Permission.Create("test.todo_items.create");
            public static Permission View => Permission.Create("testing.todo_items.view");
            public static Permission Update => Permission.Create("testing.todo_items.update");
            public static Permission Delete => Permission.Create("testing.todo_items.delete");
            public static Permission Track => Permission.Create("testing.todo_items.track");
            public static readonly Permission[] All =
            [
                Create,
                    View,
                    Update,
                    Delete,
                    Track
            ];
        }
        public static class TodoLists
        {
            public static Permission Create => Permission.Create("test.todo_lists.create");
            public static Permission List => Permission.Create("test.todo_lists.list");
            public static Permission View => Permission.Create("test.todo_lists.view");
            public static Permission Update => Permission.Create("test.todo_lists.update");
            public static Permission Delete => Permission.Create("test.todo_lists.delete");

            public static readonly Permission[] All =
            [
                Create,
                    List,
                    View,
                    Update,
                    Delete
            ];
        }
    }

}
