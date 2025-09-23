using Core.Identity;

namespace UseCases.Common.Security.Authorization.Permissions;

public static partial class Feature
{
    public static partial class Admin
    {
        public static class OptionValue
        {
            public static Permission Create => Permission.Create("admin.option_value.create");
            public static Permission Read => Permission.Create("admin.option_value.read");
            public static Permission Update => Permission.Create("admin.option_value.update");
            public static Permission Delete => Permission.Create("admin.option_value.delete");
            public static Permission List => Permission.Create("admin.option_value.list");

            public static readonly Permission[] All = [Create, Read, Update, Delete, List];
        }
    }
}
