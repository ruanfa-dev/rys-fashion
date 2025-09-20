using ErrorOr;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Services;
using UseCases.Common.Security.Authentication.Models;

namespace UseCases.Accounts.Authentication.Login;

public static partial class LoginCommand
{
    internal sealed class Handler(IKeycloakAdminService keycloakService)
        : ICommandHandler<Command, LoginResult>
    {
        public async Task<ErrorOr<LoginResult>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var tokenRequest = new TokenRequest
                {
                    Username = request.Username,
                    Password = request.Password,
                    GrantType = "password",
                    ClientId = "rys-fashion-api",
                    Scope = "openid profile email rys-fashion-api"
                };

                var tokenResponse = await keycloakService.GetTokenAsync(tokenRequest, cancellationToken);
                
                return new LoginResult
                {
                    AccessToken = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken,
                    TokenType = tokenResponse.TokenType,
                    ExpiresIn = tokenResponse.ExpiresIn
                };
            }
            catch (Exception ex)
            {
                return Error.Unauthorized("Login.Failed", $"Login failed: {ex.Message}");
            }
        }
    }
}