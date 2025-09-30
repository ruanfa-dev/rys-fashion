using SharedKernel.Domain.Primitives;

namespace Core.Identity.Permissions;

/// <summary>
/// Represents a permission identifier in the system using a three segment name:
/// {area}.{resource}.{action}.
/// </summary>
/// <remarks>
/// Each Permission carries a generated Name in the form "area.resource.action" (all lowercase).
/// The class provides helpers to create permissions from segments or from a full name,
/// to validate and parse permission names, and to generate human readable display names
/// and descriptions.
/// 
/// Examples:
/// - "admin.user.create" => Area = "admin", Resource = "user", Action = "create"
/// - DisplayName => "Create User"
/// - Description => "Create User in Admin area"
/// </remarks>
public partial class Permission : AuditableEntity
{
    #region Properties

    /// <summary>
    /// Gets the full permission name composed by joining Area, Resource and Action
    /// using <see cref="Constants.Separator"/> (e.g. "area.resource.action").
    /// This value is always stored in lowercase.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets the permission area segment (for example "admin", "billing", "catalog").
    /// Stored in lowercase.
    /// </summary>
    public string Area { get; set; } = null!;

    /// <summary>
    /// Gets the permission resource segment (for example "user", "invoices", "product").
    /// Stored in lowercase.
    /// </summary>
    public string Resource { get; set; } = null!;

    /// <summary>
    /// Gets the permission action segment (for example "create", "read", "update", "delete").
    /// Stored in lowercase.
    /// </summary>
    public string Action { get; set; } = null!;

    /// <summary>
    /// Optional long description for the permission. If not provided it is generated
    /// via <see cref="GenerateDescription(string, string, string)"/>.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional short display name for the permission. If not provided it is generated
    /// via <see cref="GenerateDisplayName(string, string, string)"/>.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the category of the permission, indicating whether it applies to users, roles, or both.
    /// </summary>
    public PermissionCategory Category { get; set; }
    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of <see cref="Permission"/> from separate segments.
    /// </summary>
    /// <param name="area">The area segment. Must not be null or whitespace; it will be normalized to lowercase.</param>
    /// <param name="resource">The resource segment. Must not be null or whitespace; it will be normalized to lowercase.</param>
    /// <param name="action">The action segment. Must not be null or whitespace; it will be normalized to lowercase.</param>
    /// <param name="description">Optional description. If null, a default description is generated.</param>
    /// <param name="displayName">Optional display name. If null, a default display name is generated.</param>
    /// <param name="category">Optional permission category. Defaults to <see cref="Constants.PermissionCategory.None"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="area"/>, <paramref name="resource"/> or <paramref name="action"/> is null.</exception>
    public Permission(string area, string resource, string action, string? description = null, string? displayName = null, PermissionCategory category = PermissionCategory.None)
    {
        Area = area?.ToLowerInvariant() ?? throw new ArgumentNullException(nameof(area));
        Resource = resource?.ToLowerInvariant() ?? throw new ArgumentNullException(nameof(resource));
        Action = action?.ToLowerInvariant() ?? throw new ArgumentNullException(nameof(action));

        Name = $"{Area}{Constants.Separator}{Resource}{Constants.Separator}{Action}";
        Description = description ?? GenerateDescription(Area, Resource, Action);
        DisplayName = displayName ?? GenerateDisplayName(Area, Resource, Action);
        Category = category;
    }
    #endregion

    #region Static Methods

    /// <summary>
    /// Generates a human readable description for a permission from its segments.
    /// Example: area="admin", resource="user", action="create" => "Create User in Admin area".
    /// </summary>
    /// <param name="area">Area segment. Expected to be non-empty.</param>
    /// <param name="resource">Resource segment. Expected to be non-empty.</param>
    /// <param name="action">Action segment. Expected to be non-empty.</param>
    /// <returns>A sentence describing the permission's intent and scope.</returns>
    public static string GenerateDescription(string area, string resource, string action)
    {
        string areaDisplay = FormatDisplayName(area);
        string resourceDisplay = FormatDisplayName(resource);
        string actionDisplay = FormatDisplayName(action);

        return $"{actionDisplay} {resourceDisplay} in {areaDisplay} area";
    }

    /// <summary>
    /// Generates a short display name for a permission from its segments.
    /// Example: area="admin", resource="user", action="create" => "Create User".
    /// </summary>
    /// <param name="area">Area segment. Provided for symmetry; not used in the default implementation of the display name.</param>
    /// <param name="resource">Resource segment. Expected to be non-empty.</param>
    /// <param name="action">Action segment. Expected to be non-empty.</param>
    /// <returns>A short, human-friendly label for the permission.</returns>
    public static string GenerateDisplayName(string area, string resource, string action)
    {
        string resourceDisplay = FormatDisplayName(resource);
        string actionDisplay = FormatDisplayName(action);

        return $"{actionDisplay} {resourceDisplay}";
    }

    /// <summary>
    /// Creates a new Permission instance from distinct segments.
    /// This is a convenience factory that forwards to the constructor.
    /// </summary>
    /// <param name="area">Area segment.</param>
    /// <param name="resource">Resource segment.</param>
    /// <param name="action">Action segment.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="displayName">Optional display name.</param>
    /// <param name="category">Optional permission category. Defaults to <see cref="Constants.PermissionCategory.None"/>.</param>
    /// <returns>A new <see cref="Permission"/> instance.</returns>
    public static Permission Create(string area, string resource, string action, string? description = null, string? displayName = null, PermissionCategory category = PermissionCategory.None)
    {
        return new Permission(area, resource, action, description, displayName, category);
    }

    /// <summary>
    /// Creates a new Permission instance from a full permission name in the
    /// "{area}.{resource}.{action}" format.
    /// </summary>
    /// <param name="name">Full permission name. Must be three non-empty segments separated by <see cref="Constants.Separator"/>.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="displayName">Optional display name.</param>
    /// <returns>A new <see cref="Permission"/> instance parsed from <paramref name="name"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when the <paramref name="name"/> does not have the expected format.</exception>
    public static Permission Create(string name, string? description = null, string? displayName = null)
    {
        (string Area, string Resource, string Action)? parsed = ParsePermissionName(name);
        if (parsed == null)
            throw new ArgumentException($"Invalid permission name format. Expected format: 'area.resource.action'. Received: '{name}'", nameof(name));
        return new Permission(parsed.Value.Area, parsed.Value.Resource, parsed.Value.Action, description, displayName, PermissionCategory.Both);
    }

    /// <summary>
    /// Formats a single segment into a display-friendly form by capitalizing the first character.
    /// This helper does not validate the string for length; callers must ensure non-empty input.
    /// </summary>
    /// <param name="name">A non-empty segment string.</param>
    /// <returns>The input with the first character converted to upper case and the rest preserved.</returns>
    private static string FormatDisplayName(string name)
    {
        return char.ToUpperInvariant(name[0]) + name[1..];
    }

    /// <summary>
    /// Validates whether a permission name is in the expected three-segment format.
    /// </summary>
    /// <param name="permissionName">The permission name to validate.</param>
    /// <returns><see langword="true"/> if the name consists of exactly three non-empty segments separated by <see cref="Constants.Separator"/>; otherwise <see langword="false"/>.</returns>
    public static bool IsValidPermissionName(string permissionName)
    {
        if (string.IsNullOrWhiteSpace(permissionName))
            return false;

        permissionName = permissionName.Trim();

        if (permissionName.Length < Constraints.MinNameLength || permissionName.Length > Constraints.MaxNameLength)
            return false;

        string[] parts = permissionName.Split(Constants.Separator);
        if (parts.Length != Constraints.Segments)
            return false;

        foreach (string p in parts)
        {
            if (p.Length < Constraints.MinSegmentLength || p.Length > Constraints.MaxSegmentLength)
                return false;

            // Normalize to lowercase before validation since we store everything in lowercase
            string normalizedPart = p.ToLowerInvariant();

            // Use System.Text.RegularExpressions.Regex to validate allowed characters
            if (!System.Text.RegularExpressions.Regex.IsMatch(normalizedPart, Constraints.SegmentAllowedPattern))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Parses a permission name into its constituent segments.
    /// </summary>
    /// <param name="permissionName">Permission name in "{area}.{resource}.{action}" format.</param>
    /// <returns>
    /// A tuple containing Area, Resource and Action if the name is valid; otherwise <see langword="null"/>.
    /// </returns>
    public static (string Area, string Resource, string Action)? ParsePermissionName(string permissionName)
    {
        if (!IsValidPermissionName(permissionName))
            return null;

        string[] parts = permissionName.Split(Constants.Separator);
        // Return lowercase normalized segments since that's how we store them
        return (parts[0].ToLowerInvariant(), parts[1].ToLowerInvariant(), parts[2].ToLowerInvariant());
    }

    #endregion
}
