namespace UseCases.Admin.Roles.Common;

public record BatchUserRoleResult : BatchRolesParam
{
    public required Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? AssignedBy { get; set; }
}
