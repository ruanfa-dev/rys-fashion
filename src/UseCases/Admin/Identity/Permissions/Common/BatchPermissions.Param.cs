namespace UseCases.Admin.Identity.Permissions.Common;

public record BatchPermissionsParam
{
    public required string[] Permissions { get; init; }
}
