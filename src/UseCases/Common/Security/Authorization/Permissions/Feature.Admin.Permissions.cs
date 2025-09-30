using Core.Identity.Permissions;

namespace UseCases.Common.Security.Authorization.Permissions;

public static partial class Feature
{
    public static partial class Admin
    {
        public static class AccessPermission
        {
            public static Permission View => Permission.Create("Admin.Permissions.View");
            public static Permission Assign => Permission.Create("Admin.Permissions.Assign");
            public static Permission List => Permission.Create("Admin.Permissions.List");
            public static Permission Manage => Permission.Create("Admin.Permissions.Manage");

            public static readonly Permission[] All = [View, Assign, List, Manage];
        }
    }
}
