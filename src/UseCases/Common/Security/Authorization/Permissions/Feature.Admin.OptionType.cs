using Core.Identity.Permissions;

namespace UseCases.Common.Security.Authorization.Permissions;

public static partial class Feature
{
    public static partial class Admin
    {
        public static class OptionType
        {
            public static Permission Create => Permission.Create("admin.optionType.create");
            public static Permission List => Permission.Create("admin.optionType.list");
            public static Permission View => Permission.Create("admin.optionType.view");
            public static Permission Update => Permission.Create("admin.optionType.update");
            public static Permission Delete => Permission.Create("admin.optionType.delete");

            public static Permission[] All => [Create, List, View, Update, Delete];
        }
    }
}