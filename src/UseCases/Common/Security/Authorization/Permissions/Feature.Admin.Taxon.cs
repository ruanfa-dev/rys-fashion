using Core.Identity.Permissions;

namespace UseCases.Common.Security.Authorization.Permissions;

public static partial class Feature
{
    public static partial class Admin
    {
        public static class Taxon
        {
            public static Permission Create => Permission.Create("admin.taxon.create");
            public static Permission List => Permission.Create("admin.taxon.list");
            public static Permission View => Permission.Create("admin.taxon.view");
            public static Permission Update => Permission.Create("admin.taxon.update");
            public static Permission Delete => Permission.Create("admin.taxon.delete");

            public static readonly Permission[] All = [Create, List, View, Update, Delete];
        }
    }
}
