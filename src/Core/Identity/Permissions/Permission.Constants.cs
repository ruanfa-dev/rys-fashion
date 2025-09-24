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
}
