using ErrorOr;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Roles.Create;

public static partial class CreateRoleCommand
{
    public const string Name = nameof(CreateRoleCommand);
    public const string Summary = "Create new role in Keycloak";
    public const string Description = "Creates a new role in Keycloak realm";

    public record Command(CreateRoleParam Param) : ICommand<CreateRoleResult>;

    public record CreateRoleParam
    {
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
    }

    public record CreateRoleResult
    {
        public string Name { get; init; } = string.Empty;
    }
}