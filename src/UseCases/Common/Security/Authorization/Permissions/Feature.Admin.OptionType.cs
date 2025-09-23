using Core.Identity;

namespace UseCases.Common.Security.Authorization.Permissions;

public static partial class Feature
{
    public static partial class Admin
    {
        public static class OptionType
        {
            public static Permission Create => Permission.Create("admin.option_type.create");
            public static Permission Read => Permission.Create("admin.option_type.read");
            public static Permission Update => Permission.Create("admin.option_type.update");
            public static Permission Delete => Permission.Create("admin.option_type.delete");
            public static Permission List => Permission.Create("admin.option_type.list");
            public static Permission Assign => Permission.Create("admin.option_type.assign");

            public static readonly Permission[] All = [Create, Read, Update, Delete, List, Assign];
        }
    }
}
