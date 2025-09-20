using ErrorOr;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Models;

namespace UseCases.Accounts.Authentication.Login;

public static partial class LoginCommand
{
    public const string Name = nameof(LoginCommand);
    public const string Summary = "Login with Keycloak";
    public const string Description = "Authenticates user with Keycloak and returns JWT tokens";

    public record Command(string Username, string Password) : ICommand<LoginResult>;

    public record LoginResult
    {
        public string AccessToken { get; init; } = string.Empty;
        public string? RefreshToken { get; init; }
        public string TokenType { get; init; } = "Bearer";
        public int ExpiresIn { get; init; }
    }
}