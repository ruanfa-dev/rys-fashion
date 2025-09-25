namespace UseCases.Admin.Identity.Permissions.Common;


/// <summary>
/// Base parameter for permission operations
/// </summary>
public record PermissionBaseResult
{
    public string Name { get; set; } = null!;
    public string Area { get; set; } = null!;
    public string Resource { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string? Description { get; set; }
    public string? DisplayName { get; set; }
}

/// <summary>
/// Base result for permission operations
/// </summary>
public record PermissionResult : PermissionBaseResult
{
    public required Guid Id { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
}

/// <summary>
/// Detailed result for permission operations with comprehensive information
/// </summary>
public record PermissionDetailedResult : PermissionResult
{
    #region Assignment Information
    public PermissionAssignmentInfo[] RoleAssignments { get; init; } = [];
    public PermissionAssignmentInfo[] UserAssignments { get; init; } = [];
    #endregion
}

public sealed record PermissionAssignmentInfo
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
}

