using ErrorOr;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Users.AssignRoles;

public static partial class AssignRolesToUserCommand
{
    public const string Name = nameof(AssignRolesToUserCommand);
    public const string Summary = "Assign roles to user in Keycloak";
    public const string Description = "Assigns roles to a user in Keycloak realm";

    public record Command(string UserId, AssignRolesParam Param) : ICommand<AssignRolesResult>;

    public record AssignRolesParam
    {
        public IEnumerable<string> RoleNames { get; init; } = Array.Empty<string>();
    }

    public record AssignRolesResult
    {
        public string UserId { get; init; } = string.Empty;
        public List<string> AssignedRoles { get; init; } = new();
        public bool Success { get; init; }
    }
}