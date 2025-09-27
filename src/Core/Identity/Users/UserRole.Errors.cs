using ErrorOr;

namespace Core.Identity.Users;

/// <summary>
/// Predefined errors for user role management operations
/// </summary>
public partial class UserRole
{
    public static class Errors
    {
        #region Validation Errors
        public static Error MaxRolesExceeded => Error.Validation(
            code: "UserRole.MaxRolesExceeded",
            description: $"User cannot have more than {Constraints.MaxRolePerUser} roles assigned");

        public static Error MaxUsersExceeded => Error.Validation(
            code: "UserRole.MaxUsersExceeded",
            description: $"Role cannot be assigned to more than {Constraints.MaxUsersPerRole} users");

        public static Error UserIdsRequired => Error.Validation(
            code: "UserRole.UserIdsRequired",
            description: "User IDs are required for multi-user operations");

        public static Error DuplicateUsers => Error.Validation(
            code: "UserRole.DuplicateUsers",
            description: "Duplicate user IDs are not allowed");

        public static Error DuplicateRoles => Error.Validation(
            code: "UserRole.DuplicateRoles",
            description: "Duplicate role names are not allowed");
        #endregion

        #region Conflict Errors
        public static Error AlreadyAssigned(string roleName) => Error.Conflict(
            code: "UserRole.AlreadyAssigned",
            description: $"User already has role '{roleName}'");

        public static Error NotAssigned(string roleName) => Error.Conflict(
            code: "UserRole.NotAssigned",
            description: $"User does not have role '{roleName}'");
        #endregion

        #region Failure Errors
        public static Error AssignmentFailed(string roleName) => Error.Failure(
            code: "UserRole.AssignmentFailed",
            description: $"Failed to assign role '{roleName}' to user");

        public static Error RemovalFailed(string roleName) => Error.Failure(
            code: "UserRole.RemovalFailed",
            description: $"Failed to remove role '{roleName}' from user");

        public static Error UnexpectedError => Error.Failure(
            code: "UserRole.UnexpectedError",
            description: "An unexpected error occurred while managing user roles");
        #endregion

        #region Operation Errors
        public static Error InvalidOperation(string operation) => Error.Validation(
            code: "UserRole.InvalidOperation",
            description: $"Invalid user role operation: {operation}");
        #endregion
    }
}