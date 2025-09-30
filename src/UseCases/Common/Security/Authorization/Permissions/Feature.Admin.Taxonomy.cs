using Core.Identity.Permissions;

namespace UseCases.Common.Security.Authorization.Permissions;

public static partial class Feature
{
    public static partial class Admin
    {
        public static class Taxonomy
        {
            public static Permission Create => Permission.Create("admin.taxonomy.create");
            public static Permission List => Permission.Create("admin.taxonomy.list");
            public static Permission View => Permission.Create("admin.taxonomy.view");
            public static Permission Update => Permission.Create("admin.taxonomy.update");
            public static Permission Delete => Permission.Create("admin.taxonomy.delete");

            public static readonly Permission[] All = [ Create, List, View, Update, Delete ];
        }
    }
}
