using ErrorOr;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Roles.Update;

public static partial class UpdateRoleCommand
{
    public const string Name = nameof(UpdateRoleCommand);
    public const string Summary = "Update role in Keycloak";
    public const string Description = "Updates an existing role in Keycloak realm";

    public record Command(string RoleName, UpdateRoleParam Param) : ICommand<UpdateRoleResult>;

    public record UpdateRoleParam
    {
        public string? Name { get; init; }
        public string? Description { get; init; }
    }

    public record UpdateRoleResult
    {
        public string Name { get; init; } = string.Empty;
        public bool Success { get; init; }
    }
}