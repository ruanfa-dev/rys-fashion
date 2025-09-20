using ErrorOr;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Models;

namespace UseCases.Accounts.Authentication.RefreshTokens;

public static partial class RefreshTokenCommand
{
    public const string Name = nameof(RefreshTokenCommand);
    public const string Summary = "Refresh Keycloak token";
    public const string Description = "Refreshes an expired Keycloak access token using refresh token";

    public record Command(string RefreshToken) : ICommand<RefreshTokenResult>;

    public record RefreshTokenResult
    {
        public string AccessToken { get; init; } = string.Empty;
        public string? RefreshToken { get; init; }
        public string TokenType { get; init; } = "Bearer";
        public int ExpiresIn { get; init; }
    }
}