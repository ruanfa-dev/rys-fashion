namespace Core.Identity;

/// <summary>
/// Represents permission helpers and related types used across the identity system.
/// This partial class holds nested types and constants that help construct and interpret
/// permission names and categories.
/// </summary>
public partial class Permission
{
    /// <summary>
    /// Constant values used by <see cref="Permission"/> helpers.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Separator used to join permission segments into a full name.
        /// Default is '.' producing names like "area.resource.action".
        /// </summary>
        /// <remarks>
        /// Use this separator when composing or parsing permission names to ensure a consistent
        /// format across the application. For example: "billing.invoice.create" where
        /// "billing" is the area, "invoice" is the resource and "create" is the action.
        /// </remarks>
        public const string Separator = ".";
    }

    /// <summary>
    /// Categorizes which subjects a permission applies to.
    /// </summary>
    /// <remarks>
    /// This enum is used to indicate whether a permission is intended for users, roles,
    /// both, or neither. It can be used by authorization helpers and tooling to filter
    /// or present permissions appropriately in management UIs.
    /// </remarks>
    public enum PermissionCategory
    {
        /// <summary>
        /// No specific category; the permission does not target a user or role explicitly.
        /// </summary>
        None = 0,

        /// <summary>
        /// Permission that applies to an individual user.
        /// </summary>
        User,

        /// <summary>
        /// Permission that applies to a role (a group of users).
        /// </summary>
        Role,

        /// <summary>
        /// Permission that applies to both users and roles.
        /// </summary>
        Both
    }

    /// <summary>
    /// Indicates the type or intent of a permission entry.
    /// </summary>
    /// <remarks>
    /// Use this enum to express whether a permission explicitly grants or denies access,
    /// is unspecified, or should be inherited from higher-level configuration.
    /// Typical usage:
    /// - Grant: explicit allow for an action/resource
    /// - Deny: explicit deny which may take precedence over grants
    /// - Inherit: follow parent/default rules
    /// </remarks>
    public enum PermissionType
    {
        /// <summary>
        /// No specific type; unspecified or default behavior.
        /// </summary>
        None = 0,

        /// <summary>
        /// Permission explicitly grants access.
        /// </summary>
        Grant = 1,

        /// <summary>
        /// Permission explicitly denies access.
        /// </summary>
        Deny = 2,

        /// <summary>
        /// Permission should be inherited from parent/default rules.
        /// </summary>
        Inherit = 3
    }
}
