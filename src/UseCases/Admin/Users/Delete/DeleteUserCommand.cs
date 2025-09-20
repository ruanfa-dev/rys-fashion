using ErrorOr;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Users.Delete;

public static partial class DeleteUserCommand
{
    public const string Name = nameof(DeleteUserCommand);
    public const string Summary = "Delete user from Keycloak";
    public const string Description = "Deletes a user from Keycloak realm";

    public record Command(string Id) : ICommand<DeleteUserResult>;

    public record DeleteUserResult
    {
        public string Id { get; init; } = string.Empty;
        public bool Success { get; init; }
    }
}