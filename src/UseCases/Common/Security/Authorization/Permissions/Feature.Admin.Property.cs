using Core.Identity;

namespace UseCases.Common.Security.Authorization.Permissions;

public static partial class Feature
{
    public static partial class Admin
    {
        public static class Property
        {
            public static Permission Create => Permission.Create("admin.property.create");
            public static Permission Read => Permission.Create("admin.property.read");
            public static Permission Update => Permission.Create("admin.property.update");
            public static Permission Delete => Permission.Create("admin.property.delete");
            public static Permission List => Permission.Create("admin.property.list");
            public static Permission Assign => Permission.Create("admin.property.assign");

            public static readonly Permission[] All = [Create, Read, Update, Delete, List, Assign];
        }
    }
}
