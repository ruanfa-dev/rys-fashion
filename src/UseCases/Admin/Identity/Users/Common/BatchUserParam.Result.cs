namespace UseCases.Admin.Identity.Users.Common;
public record BatchRoleUserResult : BatchUserParam
{
    public Guid RoleId { get; init; }
    public string? RoleName { get; init; }
    public string? Message { get; init; }
    public DateTimeOffset AssignedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? AssignedBy { get; init; }
}