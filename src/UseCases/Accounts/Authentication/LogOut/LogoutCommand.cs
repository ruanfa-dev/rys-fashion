using ErrorOr;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Accounts.Authentication.LogOut;

public static partial class LogoutCommand
{
    public const string Name = nameof(LogoutCommand);
    public const string Summary = "Logout from Keycloak";
    public const string Description = "Logs out user from Keycloak and invalidates tokens";

    public record Command(string? RefreshToken) : ICommand<LogoutResult>;

    public record LogoutResult
    {
        public bool Success { get; init; }
    }
}