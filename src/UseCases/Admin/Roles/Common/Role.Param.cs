namespace UseCases.Admin.Roles.Common;

/// <summary>
/// Base parameter for role operations
/// </summary>
public record RoleParam
{
    public required string Name { get; init; }
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public int Priority { get; init; } = 0;
    public bool IsSystemRole { get; init; } = false;
}
