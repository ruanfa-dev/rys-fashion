using ErrorOr;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Roles.Delete;

public static partial class DeleteRoleCommand
{
    public const string Name = nameof(DeleteRoleCommand);
    public const string Summary = "Delete role from Keycloak";
    public const string Description = "Deletes a role from Keycloak realm";

    public record Command(string RoleName) : ICommand<DeleteRoleResult>;

    public record DeleteRoleResult
    {
        public string Name { get; init; } = string.Empty;
        public bool Success { get; init; }
    }
}