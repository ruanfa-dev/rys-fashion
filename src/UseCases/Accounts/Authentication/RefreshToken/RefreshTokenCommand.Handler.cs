using ErrorOr;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Services;
using UseCases.Common.Security.Authentication.Models;

namespace UseCases.Accounts.Authentication.RefreshTokens;

public static partial class RefreshTokenCommand
{
    internal sealed class Handler(IKeycloakAdminService keycloakService)
        : ICommandHandler<Command, RefreshTokenResult>
    {
        public async Task<ErrorOr<RefreshTokenResult>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var tokenRequest = new TokenRequest
                {
                    RefreshToken = request.RefreshToken,
                    GrantType = "refresh_token",
                    ClientId = "rys-fashion-api",
                    Scope = "openid profile email rys-fashion-api"
                };

                var tokenResponse = await keycloakService.RefreshTokenAsync(tokenRequest, cancellationToken);
                
                return new RefreshTokenResult
                {
                    AccessToken = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken,
                    TokenType = tokenResponse.TokenType,
                    ExpiresIn = tokenResponse.ExpiresIn
                };
            }
            catch (Exception ex)
            {
                return Error.Unauthorized("RefreshToken.Failed", $"Token refresh failed: {ex.Message}");
            }
        }
    }
}