namespace Core.Identity.Roles;

/// <summary>
/// Business constraints for role permission management operations
/// </summary>
public static partial class RolePermission
{
    public static class Constraints
    {
        #region Validation Limits
        /// <summary>
        /// Maximum number of permissions that can be assigned to a single role
        /// </summary>
        public const int MaxPermissionsPerRole = 200;
        #endregion

        #region Security Limits
        /// <summary>
        /// Maximum number of users that can be affected by a permission change
        /// Prevents accidental mass permission changes
        /// </summary>
        public const int MaxUsersAffectedByPermissionChange = 1000;

        /// <summary>
        /// Maximum number of roles that can have the same permission assigned simultaneously
        /// </summary>
        public const int MaxRolesWithSamePermission = 50;
        #endregion
    }
}