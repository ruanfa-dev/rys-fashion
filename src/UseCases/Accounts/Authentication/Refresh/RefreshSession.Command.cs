using ErrorOr;

using Microsoft.AspNetCore.Http;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Tokens.Models;
using UseCases.Common.Security.Authentication.Tokens.Services;

namespace UseCases.Accounts.Authentication.Sessions.Refresh;
public static partial class RefreshSession
{
    public sealed record Command(Param Param) : ICommand<AuthenticationResult>;
    public sealed class Handler(IHttpContextAccessor httpContext, ITokenManagementService tokenManagementService) : ICommandHandler<Command, AuthenticationResult>
    {
        public async Task<ErrorOr<AuthenticationResult>> Handle(Command request, CancellationToken cancellationToken)
        {
            var ipAddress = httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            string refreshToken = request.Param.RefreshToken;
            bool rememberMe = request.Param.RememberMe;

            // Attempt: refresh the token using the token management service
            var refreshResult = await tokenManagementService.RefreshAsync(
                refreshToken,
                ipAddress: ipAddress,
                rememberMe: rememberMe,
                cancellationToken: cancellationToken);
            if (refreshResult.IsError)
            {
                return refreshResult.Errors;
            }
            return refreshResult.Value;
        }
    }
}
