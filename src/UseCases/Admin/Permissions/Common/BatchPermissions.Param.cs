namespace UseCases.Admin.Permissions.Common;

public record BatchPermissionsParam
{
    public required string[] Permissions { get; init; }
}
