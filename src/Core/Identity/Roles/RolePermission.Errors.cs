using ErrorOr;

namespace Core.Identity.Roles;

/// <summary>
/// Predefined errors for role permission management operations
/// </summary>
public static partial class RolePermission
{
    public static class Errors
    {
        #region Permission Assignment Errors
        public static Error AlreadyAssigned(string permission) => Error.Conflict(
            code: "RolePermission.AlreadyAssigned",
            description: $"Role already has permission '{permission}'");

        public static Error NotAssigned(string permission) => Error.Conflict(
            code: "RolePermission.NotAssigned",
            description: $"Role does not have permission '{permission}'");

        public static Error AssignmentFailed(string permission) => Error.Failure(
            code: "RolePermission.AssignmentFailed",
            description: $"Failed to assign permission '{permission}' to role");

        public static Error RemovalFailed(string permission) => Error.Failure(
            code: "RolePermission.RemovalFailed",
            description: $"Failed to remove permission '{permission}' from role");
        #endregion

        #region Role-Specific Errors
        public static Error DefaultRolePermissionCannotBeRemoved(string permission, string roleName) => Error.Validation(
            code: "RolePermission.DefaultRolePermissionCannotBeRemoved",
            description: $"Cannot remove default permission '{permission}' from role '{roleName}'");
        #endregion

        #region Business Logic Errors
        public static Error MaxPermissionsExceeded => Error.Validation(
            code: "RolePermission.MaxPermissionsExceeded",
            description: $"Role cannot have more than {Constraints.MaxPermissionsPerRole} permissions assigned");

        public static Error ConflictingPermissions(string permission1, string permission2) => Error.Validation(
            code: "RolePermission.ConflictingPermissions",
            description: $"Permissions '{permission1}' and '{permission2}' conflict with each other");

        public static Error PermissionRequiresOtherPermission(string permission, string requiredPermission) => Error.Validation(
            code: "RolePermission.PermissionRequiresOtherPermission",
            description: $"Permission '{permission}' requires '{requiredPermission}' to be assigned first");

        public static Error WouldAffectTooManyUsers(int userCount, int maxAllowed) => Error.Validation(
            code: "RolePermission.WouldAffectTooManyUsers",
            description: $"This operation would affect {userCount} users, which exceeds the maximum of {maxAllowed}");
        #endregion

        #region Batch Operation Errors

        public static Error DuplicatePermissionsInBatch => Error.Validation(
            code: "RolePermission.DuplicatePermissionsInBatch",
            description: "Batch operation contains duplicate permissions");
        #endregion


        #region General Errors
        public static Error UnexpectedError => Error.Unexpected(
            code: "RolePermission.UnexpectedError",
            description: "An unexpected error occurred while processing role permission operation");
        #endregion
    }
}