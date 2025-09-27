using Core.Identity;
using Core.Identity.Permissions;

namespace UseCases.Common.Security.Authorization.Permissions;

public static partial class Feature
{
    public static partial class Admin
    {
        public static readonly Permission[] All =
        [
            .. User.All,
            .. Role.All,
            .. AccessPermission.All,
            .. AuditLog.All,
            .. Property.All
        ];
    }
}
