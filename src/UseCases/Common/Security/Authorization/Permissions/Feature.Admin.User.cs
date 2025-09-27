using Core.Identity;
using Core.Identity.Permissions;

namespace UseCases.Common.Security.Authorization.Permissions;

public static partial class Feature
{
    public static partial class Admin
    {
        public static class User
        {
            public static Permission Create => Permission.Create("admin.user.create");
            public static Permission List => Permission.Create("admin.user.list");
            public static Permission View => Permission.Create("admin.user.view");
            public static Permission Update => Permission.Create("admin.user.update");
            public static Permission Delete => Permission.Create("admin.user.delete");

            public static readonly Permission[] All = [Create, List, View, Update, Delete];
        }
       
    }
}
