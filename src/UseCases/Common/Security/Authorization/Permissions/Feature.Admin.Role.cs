using Core.Identity;

namespace UseCases.Common.Security.Authorization.Permissions;

public static partial class Feature
{
    public static partial class Admin
    {
        public static class Role
        {
            public static Permission Create => Permission.Create("admin.role.create");
            public static Permission Read => Permission.Create("admin.role.read");
            public static Permission Update => Permission.Create("admin.role.update");
            public static Permission Delete => Permission.Create("admin.role.delete");
            public static Permission List => Permission.Create("admin.role.list");
            public static Permission Assign => Permission.Create("admin.role.assign");

            public static readonly Permission[] All = [Create, Read, Update, Delete, List, Assign];
        }
    }
}
