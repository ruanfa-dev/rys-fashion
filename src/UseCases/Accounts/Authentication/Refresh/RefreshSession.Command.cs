using Core.Identity.Tokens;
using Core.Identity.Users;

using ErrorOr;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Persistence.Context;
using UseCases.Common.Security.Authentication.Tokens.Models;
using UseCases.Common.Security.Authentication.Tokens.Services;

namespace UseCases.Accounts.Authentication.Refresh;
public static partial class RefreshSession
{
    public sealed record Command(Param Param) : ICommand<AuthenticationResult>;

    public sealed class Handler(
        UserManager<User> userManager,
        IHttpContextAccessor httpContext,
        IUnitOfWork unitOfWork,
        IRefreshTokenService refreshTokenService,
        IJwtTokenService jwtTokenService,
        ILogger<Handler> logger) : ICommandHandler<Command, AuthenticationResult>
    {
        private readonly IHttpContextAccessor _httpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IRefreshTokenService _refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
        private readonly IJwtTokenService _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        private readonly ILogger<Handler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task<ErrorOr<AuthenticationResult>> Handle(Command request, CancellationToken cancellationToken)
        {
            string ipAddress = _httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            string refreshToken = request.Param.RefreshToken;
            bool rememberMe = request.Param.RememberMe;

            // Input validation
            if (string.IsNullOrWhiteSpace(refreshToken))
                return Error.Validation("Refresh.InvalidToken", "Refresh token is required");

            if (string.IsNullOrWhiteSpace(ipAddress) || ipAddress.Length > RefreshToken.Constraints.IpAddressLength)
                return Error.Validation("Refresh.InvalidIpAddress", "Valid IP address is required");

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Validate refresh token
                ErrorOr<RefreshTokenValidationResult> validationResult = await _refreshTokenService.ValidateRefreshTokenAsync(refreshToken, cancellationToken);
                if (validationResult.IsError)
                {
                    _logger.LogWarning("Invalid refresh token used from IP {IpAddress}", ipAddress);
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return validationResult.Errors;
                }

                User user = validationResult.Value.User;

                // Security validation (placeholder — adapt to your project's security checks)
                ErrorOr<Success> securityValidation = await ValidateUserSecurityAsync(user, cancellationToken);
                if (securityValidation.IsError)
                {
                    // Revoke token for security
                    await _refreshTokenService.RevokeTokenAsync(refreshToken, ipAddress, "User security status changed", cancellationToken);
                    await LogSecurityEventAsync(user.Id, ipAddress, "Refresh blocked", securityValidation.Errors.First().Description, cancellationToken);
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return securityValidation.Errors;
                }

                // Rotate token
                ErrorOr<RefreshTokenResult> rotationResult = await _refreshTokenService.RotateRefreshTokenAsync(
                    refreshToken, ipAddress, rememberMe, cancellationToken);
                if (rotationResult.IsError)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return rotationResult.Errors;
                }

                // Generate new access token
                ErrorOr<AccessTokenResult> accessResult = await jwtTokenService.GenerateAccessTokenAsync(user, cancellationToken);
                if (accessResult.IsError)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return accessResult.Errors;
                }

                // Update user activity
                user.RecordSignIn(ipAddress);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await LogSecurityEventAsync(user.Id, ipAddress, "Token refresh success", $"RememberMe: {rememberMe}", cancellationToken);

                return new AuthenticationResult
                {
                    AccessToken = accessResult.Value.Token,
                    AccessTokenExpiresAt = accessResult.Value.ExpiresAt,
                    RefreshToken = rotationResult.Value.Token,
                    RefreshTokenExpiresAt = rotationResult.Value.ExpiresAt,
                    TokenType = "Bearer"
                };
            }
            catch (DbUpdateConcurrencyException)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning("Token rotation conflict from IP {IpAddress}", ipAddress);
                return Error.Conflict("Refresh.ConcurrentUse", "Token is being used elsewhere. Please authenticate again.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Token refresh failed from IP {IpAddress}", ipAddress);
                return Error.Failure("Refresh.Failed", "Token refresh failed");
            }
        }

        private async Task<ErrorOr<Success>> ValidateUserSecurityAsync(User user, CancellationToken ct)
        {
            try
            {
                // Check lockout status
                if (await userManager.IsLockedOutAsync(user))
                {
                    _logger.LogWarning("Authentication blocked for locked user {UserId}", user.Id);
                    return Error.Validation("Refresh.UserLocked", "Account is locked");
                }

                // Check email confirmation (if required)
                if (!user.EmailConfirmed)
                {
                    _logger.LogWarning("Authentication blocked for unconfirmed user {UserId}", user.Id);
                    return Error.Validation("Refresh.EmailNotConfirmed", "Email must be confirmed");
                }

                return Result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Security validation failed for user {UserId}", user.Id);
                return Error.Failure("Refresh.SecurityCheckFailed", "Security validation failed");
            }
        }

        // Minimal audit/log helper
        private Task LogSecurityEventAsync(Guid userId, string ipAddress, string eventName, string details, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Event} for user {UserId} from IP {IpAddress}: {Details}", eventName, userId, ipAddress, details);
            return Task.CompletedTask;
        }
    }
}
