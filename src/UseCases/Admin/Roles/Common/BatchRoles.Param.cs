namespace UseCases.Admin.Roles.Common;

public record BatchRolesParam
{
    public required string[] RoleIds { get; set; }
}

