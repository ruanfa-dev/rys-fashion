namespace UseCases.Admin.Permissions.Common;


/// <summary>
/// Result for batch role-permission assignment operations
/// </summary>
public record BatchRolePermissionResult : BatchPermissionsParam
{
    public required string Message { get; init; }
    public required Guid RoleId { get; init; }
    public required string RoleName { get; init; }
    public required string? AssignedBy { get; init; }
    public DateTimeOffset AssignedAt { get; init; }
    public int AffectedUsersCount { get; init; }
}

/// <summary>
/// Result for batch user-permission operations
/// </summary>
public record BatchUserPermissionResult : BatchPermissionsParam
{
    public required string Message { get; init; }
    public required Guid UserId { get; init; }
    public required string UserName { get; init; }
    public required string? AssignedBy { get; init; }
    public DateTimeOffset AssignedAt { get; init; }
}
