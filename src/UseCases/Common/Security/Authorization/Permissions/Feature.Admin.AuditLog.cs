using Core.Identity.Permissions;

namespace UseCases.Common.Security.Authorization.Permissions;

public static partial class Feature
{
    public static partial class Admin
    {
        public static class AuditLog
        {
            public static Permission View => Permission.Create("admin.audit_log.view");
            public static Permission List => Permission.Create("admin.audit_log.list");
            public static Permission Export => Permission.Create("admin.audit_log.export");

            public static readonly Permission[] All = [View, List, Export];
        }
    }
}
