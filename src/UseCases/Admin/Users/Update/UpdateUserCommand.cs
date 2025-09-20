using ErrorOr;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Users.Update;

public static partial class UpdateUserCommand
{
    public const string Name = nameof(UpdateUserCommand);
    public const string Summary = "Update user in Keycloak";
    public const string Description = "Updates an existing user in Keycloak realm";

    public record Command(string Id, UpdateUserParam Param) : ICommand<UpdateUserResult>;

    public record UpdateUserParam
    {
        public string? Email { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public bool? Enabled { get; init; }
        public bool? EmailVerified { get; init; }
        public Dictionary<string, object[]>? Attributes { get; init; }
    }

    public record UpdateUserResult
    {
        public string Id { get; init; } = string.Empty;
        public bool Success { get; init; }
    }
}