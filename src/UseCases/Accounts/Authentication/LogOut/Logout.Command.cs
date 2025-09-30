using ErrorOr;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Contexts;
using UseCases.Common.Security.Authentication.Tokens.Services;

namespace UseCases.Accounts.Authentication.LogOut;

public static partial class Logout
{
    public sealed record Param(string? RefreshToken);
    public sealed record Command(Param Param) : ICommand<Deleted>;

    public sealed class Handler(
        IRefreshTokenService refreshTokenService,
        IHttpContextAccessor accessor,
        ILogger<Handler> logger) : ICommandHandler<Command, Deleted>
    {
        private readonly IRefreshTokenService _refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
        private readonly IHttpContextAccessor _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        private readonly ILogger<Handler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task<ErrorOr<Deleted>> Handle(Command request, CancellationToken cancellationToken)
        {
            var httpContext = _accessor.HttpContext;
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            try
            {
                var refreshToken = request.Param.RefreshToken;

                if (string.IsNullOrWhiteSpace(refreshToken))
                {
                    // No refresh token was provided in the request body.
                    // If the user is authenticated via an access token, log them out.
                    var principalUserId = httpContext?.User.GetUserId();
                    if (principalUserId.HasValue)
                    {
                        var revokeAllResult = await _refreshTokenService.RevokeAllUserTokensAsync(
                            userId: principalUserId.Value, ipAddress, reason: "User logout", null, cancellationToken);

                        if (revokeAllResult.IsError)
                        {
                            _logger.LogWarning("Failed to revoke all tokens for user {UserId} during logout from IP {IpAddress}: {Errors}",
                                principalUserId, ipAddress, string.Join(", ", revokeAllResult.Errors.Select(e => e.Code)));
                            return revokeAllResult.Errors;
                        }

                        _logger.LogInformation("Revoked {Count} tokens for user {UserId} during logout from IP {IpAddress}",
                            revokeAllResult.Value, principalUserId, ipAddress);
                    }
                    else
                    {
                        _logger.LogInformation("Logout requested with no refresh token and no authenticated user from IP {IpAddress}", ipAddress);
                    }

                    return Result.Deleted;
                }

                // A refresh token was supplied in the request body; revoke only that token.
                var revokeResult = await _refreshTokenService.RevokeTokenAsync(
                    refreshToken,
                    ipAddress,
                    "User logout",
                    cancellationToken);

                if (revokeResult.IsError)
                {
                    _logger.LogWarning("Failed to revoke refresh token during logout from IP {IpAddress}: {Errors}",
                        ipAddress, string.Join(", ", revokeResult.Errors.Select(e => e.Code)));
                    return revokeResult.Errors;
                }

                _logger.LogInformation("Refresh token revoked from IP {IpAddress}", ipAddress);

                // Try to log user id for auditing if token validation is available.
                var validation = await _refreshTokenService
                        .ValidateRefreshTokenAsync(refreshToken, cancellationToken);
                if (!validation.IsError && validation.Value.RefreshToken is not null)
                {
                    var user = validation.Value.RefreshToken.User;
                    if (user is not null)
                    {
                        _logger.LogInformation("User {UserId} logged out from IP {IpAddress}", user.Id, ipAddress);
                    }
                }

                return Result.Deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed from IP {IpAddress}", ipAddress);
                return Error.Failure("Logout.Failed", "Logout failed");
            }
        }
    }
}
