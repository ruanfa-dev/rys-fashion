using ErrorOr;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Users.RemoveRoles;

public static partial class RemoveRolesFromUserCommand
{
    public const string Name = nameof(RemoveRolesFromUserCommand);
    public const string Summary = "Remove roles from user in Keycloak";
    public const string Description = "Removes roles from a user in Keycloak realm";

    public record Command(string UserId, RemoveRolesParam Param) : ICommand<RemoveRolesResult>;

    public record RemoveRolesParam
    {
        public IEnumerable<string> RoleNames { get; init; } = Array.Empty<string>();
    }

    public record RemoveRolesResult
    {
        public string UserId { get; init; } = string.Empty;
        public List<string> RemovedRoles { get; init; } = new();
        public bool Success { get; init; }
    }
}