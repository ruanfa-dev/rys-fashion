namespace Core.Identity;

public partial class Permission
{
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
}
