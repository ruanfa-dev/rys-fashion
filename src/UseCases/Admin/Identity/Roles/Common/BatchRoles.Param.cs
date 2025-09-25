namespace UseCases.Admin.Identity.Roles.Common;

public record BatchRolesParam
{
    public required string[] RoleIds { get; set; }
}

