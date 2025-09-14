using ErrorOr;

namespace Core.Identity;

/// <summary>
/// Predefined errors for user permission management operations
/// </summary>
public static class UserPermission
{
    public static class Errors
    {
        #region Permission Assignment Errors
        public static Error AlreadyAssigned(string permission) => Error.Conflict(
            code: "UserPermission.AlreadyAssigned",
            description: $"User already has permission '{permission}'");

        public static Error NotAssigned(string permission) => Error.Conflict(
            code: "UserPermission.NotAssigned",
            description: $"User does not have permission '{permission}'");

        public static Error AssignmentFailed(string permission) => Error.Failure(
            code: "UserPermission.AssignmentFailed",
            description: $"Failed to assign permission '{permission}' to user");

        public static Error RemovalFailed(string permission) => Error.Failure(
            code: "UserPermission.RemovalFailed",
            description: $"Failed to remove permission '{permission}' from user");
        #endregion

        #region Business Logic Errors
        public static Error MaxPermissionsExceeded(int maxPermissions) => Error.Validation(
            code: "UserPermission.MaxPermissionsExceeded",
            description: $"User cannot have more than {maxPermissions} direct permissions assigned");

        public static Error CannotAssignSystemPermission(string permission) => Error.Validation(
            code: "UserPermission.CannotAssignSystemPermission",
            description: $"Cannot assign system permission '{permission}' directly to users");

        public static Error PermissionAlreadyInherited(string permission, string source) => Error.Conflict(
            code: "UserPermission.PermissionAlreadyInherited",
            description: $"Permission '{permission}' is already inherited from {source}");

        public static Error CannotRemoveInheritedPermission(string permission, string source) => Error.Validation(
            code: "UserPermission.CannotRemoveInheritedPermission",
            description: $"Cannot remove permission '{permission}' because it is inherited from {source}");

        public static Error ConflictingPermissions(string permission1, string permission2) => Error.Validation(
            code: "UserPermission.ConflictingPermissions",
            description: $"Permissions '{permission1}' and '{permission2}' conflict with each other");
        #endregion

        public static Error UnexpectedError => Error.Failure(
            code: "UserPermission.UnexpectedError",
            description: "An unexpected error occurred while managing user permissions");
    }
}