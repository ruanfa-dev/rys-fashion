using ErrorOr;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Users.Create;

public static partial class CreateUserCommand
{
    public const string Name = nameof(CreateUserCommand);
    public const string Summary = "Create new user in Keycloak";
    public const string Description = "Creates a new user in Keycloak realm with optional password and role assignment";

    public record Command(CreateUserParam Param) : ICommand<CreateUserResult>;

    public record CreateUserParam
    {
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public bool Enabled { get; init; } = true;
        public bool EmailVerified { get; init; } = false;
        public string? Password { get; init; }
        public bool TemporaryPassword { get; init; } = false;
        public IEnumerable<string>? RoleNames { get; init; }
        public Dictionary<string, object[]>? Attributes { get; init; }
    }

    public record CreateUserResult
    {
        public string Id { get; init; } = string.Empty;
    }
}