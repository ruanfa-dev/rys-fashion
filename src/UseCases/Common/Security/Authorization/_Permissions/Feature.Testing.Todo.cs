//namespace Application.Common.Security.Authorization.Permissions;

//public static partial class Feature
//{
//    public static partial class Testing
//    {
//        public static class Resources
//        {
//            public const ulong TodoItem = 1ul << 1;
//            public const ulong TodoList = 1ul << 2;
//        }
//        public static class Actions
//        {
//            public const ulong Create = 1ul << 1;
//            public const ulong List = 1ul << 2;
//            public const ulong View = 1ul << 3;
//            public const ulong Update = 1ul << 4;
//            public const ulong Delete = 1ul << 5;
//            public const ulong Track = 1ul << 6;
//        }
//        public static class Todo
//        {
//            public static class Items
//            {
//                public const ulong Track = Resources.TodoItem | Actions.Track;
//                public const ulong Create = Resources.TodoItem | Actions.Create;
//                public const ulong List = Resources.TodoItem | Actions.List;
//                public const ulong View = Resources.TodoItem | Actions.View;
//                public const ulong Update = Resources.TodoItem | Actions.Update;
//                public const ulong Delete = Resources.TodoItem | Actions.Delete;

//                public const ulong Manage =
//                    Track |
//                    Create |
//                    Update |
//                    Delete;

//                public const ulong Read =
//                    View |
//                    List;

//                public const ulong All =
//                    Manage |
//                    Read;
//            }

//            public static class Lists
//            {
//                public const ulong Create = Resources.TodoList | Actions.Create;
//                public const ulong List = Resources.TodoList | Actions.List;
//                public const ulong View = Resources.TodoList | Actions.View;
//                public const ulong Update = Resources.TodoList | Actions.Update;
//                public const ulong Delete = Resources.TodoList | Actions.Delete;

//                public const ulong Manage =
//                    Create |
//                    Update |
//                    Delete;

//                public const ulong Read =
//                    View |
//                    List;

//                public const ulong All =
//                    Manage |
//                    Read;
//            }
//        }
//    }
//}
