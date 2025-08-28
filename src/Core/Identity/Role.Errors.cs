using ErrorOr;

namespace Core.Identity;

public partial class Role
{
    public static class Errors
    {
        // Role ID errors
        public static Error RoleIdRequired => Error.Validation(
            code: $"Role.RoleIdRequired",
            description: "Role ID is required.");

        // Name validation errors
        public static Error NameRequired => Error.Validation(
            code: $"Role.NameRequired",
            description: "Role name is required.");
        public static Error NameTooShort => Error.Validation(
            code: $"Role.NameTooShort",
            description: $"Role name must be at least {Constraints.MinNameLength} characters long.");
        public static Error NameTooLong => Error.Validation(
            code: $"Role.NameTooLong",
            description: $"Role name must be at most {Constraints.MaxNameLength} characters long.");
        public static Error NameInvalidFormat => Error.Validation(
            code: $"Role.NameInvalidFormat",
            description: "Role name contains invalid characters. Only alphanumeric characters, spaces, underscores, and hyphens are allowed.");

        // Description validation errors
        public static Error DescriptionTooLong => Error.Validation(
            code: $"Role.DescriptionTooLong",
            description: $"Role description must be at most {Constraints.MaxDescriptionLength} characters long.");

        // Business logic errors
        public static Error RoleNotFound => Error.NotFound(
            code: $"Role.RoleNotFound",
            description: "Role not found.");
        public static Error DefaultRoleNotFound => Error.NotFound(
            code: $"Role.RoleNotFound",
            description: "The default user role is not configured in the system.");
        public static Error RoleAlreadyExists(string roleName) => Error.Conflict(
            code: $"Role.RoleAlreadyExists",
            description: $"A role with the name '{roleName}' already exists.");
        public static Error CannotDeleteDefaultRole(string roleName) => Error.Validation(
            code: $"Role.CannotDeleteDefaultRole",
            description: $"Cannot delete default role '{roleName}'.");
        public static Error CannotModifyDefaultRole(string roleName) => Error.Validation(
            code: $"Role.CannotModifyDefaultRole",
            description: $"Cannot modify default role '{roleName}'.");
        public static Error RoleInUse(string roleName) => Error.Validation(
            code: $"Role.RoleInUse",
            description: $"Cannot delete role '{roleName}' because it is assigned to one or more users.");
    }
}
