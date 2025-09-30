using Core.Identity.Users;

using ErrorOr;

using FluentValidation;

using Mapster;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Externals;
using UseCases.Common.Security.Authentication.Services;
using UseCases.Common.Security.Authentication.Tokens.Models;
using UseCases.Common.Security.Authentication.Tokens.Services;

namespace UseCases.Accounts.Authentication.Login.External.Exchange;

public static partial class ExchangeExternalToken
{
    public sealed record Param(
        string Provider,
        string? AccessToken = null,
        string? IdToken = null,
        string? AuthorizationCode = null,
        string? RedirectUri = null,
        bool RememberMe = false); 

    public sealed class ParamValidator : AbstractValidator<Param>
    {
        public ParamValidator()
        {
            RuleFor(x => x.Provider)
                .NotEmpty()
                .WithErrorCode("Provider.Required")
                .WithMessage("Provider is required");

            RuleFor(x => x)
                .Must(p =>
                    !string.IsNullOrWhiteSpace(p.AccessToken) ||
                    !string.IsNullOrWhiteSpace(p.IdToken) ||
                    !string.IsNullOrWhiteSpace(p.AuthorizationCode))
                .WithErrorCode("Token.Required")
                .WithMessage("Either access token, ID token, or authorization code is required");
        }
    }

    public sealed record Command(Param Param) : ICommand<Result>;

    public sealed record Result : AuthenticationResult
    {
        public bool IsNewUser { get; set; }
        public bool IsNewLogin { get; set; }
        public UserProfile? UserProfile { get; set; }
    }

    public sealed record UserProfile
    {
        public string Email { get; init; } = null!;
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? ProfilePictureUrl { get; init; }
        public bool EmailVerified { get; init; }
        public bool HasExternalLogins { get; init; }
        public string[] ExternalProviders { get; init; } = Array.Empty<string>();
        public Dictionary<string, string> AdditionalClaims { get; init; } = new();
    }

    public sealed class Handler(
        UserManager<User> userManager,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IExternalTokenValidator tokenValidator,
        IExternalUserService externalUserService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<Handler> logger
    ) : ICommandHandler<Command, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {
            Param param = request.Param;
            string ipAddress = GetClientIpAddress();

            try
            {
                logger.LogDebug("Validating external token for provider: {Provider}", param.Provider);
                ErrorOr<ExternalUserInfo> validationResult = await tokenValidator.ValidateTokenAsync(
                    provider: param.Provider,
                    accessToken: param.AccessToken,
                    idToken: param.IdToken,
                    authorizationCode: param.AuthorizationCode,
                    redirectUri: param.RedirectUri,
                    cancellationToken
                );

                if (validationResult.IsError)
                {
                    logger.LogWarning("Token validation failed for provider {Provider}: {Errors}",
                        param.Provider, string.Join(", ", validationResult.Errors.Select(e => e.Description)));
                    return validationResult.Errors;
                }

                ExternalUserInfo externalUserInfo = validationResult.Value;
                logger.LogDebug("Successfully validated token for user: {Email} from provider: {Provider}",
                    externalUserInfo.Email, param.Provider);

                ErrorOr<(User User, bool IsNewUser, bool IsNewLogin)> userResult = await externalUserService.FindOrCreateUserWithExternalLoginAsync(
                    externalUserInfo,
                    param.Provider,
                    cancellationToken);

                if (userResult.IsError)
                {
                    logger.LogError("Failed to find or create user for provider {Provider}: {Errors}",
                        param.Provider, string.Join(", ", userResult.Errors.Select(e => e.Description)));
                    return userResult.Errors;
                }

                (User user, bool isNewUser, bool isNewLogin) = userResult.Value;

                user.RecordSignIn(ipAddress);
                await userManager.UpdateAsync(user);

                ErrorOr<AccessTokenResult> accessResult = await jwtTokenService.GenerateAccessTokenAsync(user!, cancellationToken);
                if (accessResult.IsError)
                {
                    logger.LogError("Access token generation failed for user {UserId}", user!.Id);
                    return accessResult.Errors;
                }

                ErrorOr<RefreshTokenResult> refreshResult = await refreshTokenService.GenerateRefreshTokenAsync(
                    user!.Id, ipAddress, param.RememberMe, cancellationToken);
                if (refreshResult.IsError)
                {
                    logger.LogError("Refresh token generation failed for user {UserId}", user.Id);
                    return refreshResult.Errors;
                }

                AuthenticationResult tokens = new AuthenticationResult
                {
                    AccessToken = accessResult.Value.Token,
                    AccessTokenExpiresAt = accessResult.Value.ExpiresAt,
                    RefreshToken = refreshResult.Value.Token,
                    RefreshTokenExpiresAt = refreshResult.Value.ExpiresAt,
                    TokenType = "Bearer"
                };

                UserProfile userProfile = await BuildUserProfileAsync(user, externalUserInfo, cancellationToken);

                Result result = tokens.Adapt<Result>();
                result.IsNewUser = isNewUser;
                result.IsNewLogin = isNewLogin;
                result.UserProfile = userProfile;

                logger.LogInformation("External token exchange successful for user {UserId} via {Provider}. NewUser: {IsNewUser}, NewLogin: {IsNewLogin}",
                    user.Id, param.Provider, isNewUser, isNewLogin);

                return result;
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("External token exchange was cancelled for provider: {Provider}", param.Provider);
                return Error.Failure("TokenExchange.Cancelled", "Token exchange operation was cancelled");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during external token exchange for provider: {Provider}", param.Provider);
                return Error.Failure("TokenExchange.UnexpectedError", "An unexpected error occurred during token exchange");
            }
        }

        private async Task<UserProfile> BuildUserProfileAsync(
            User user,
            ExternalUserInfo externalUserInfo,
            CancellationToken cancellationToken)
        {
            try
            {
                IList<UserLoginInfo> externalLogins = await externalUserService.GetExternalLoginsAsync(user.Id, cancellationToken);
                string[] externalProviders = externalLogins.Select(l => l.LoginProvider.ToLowerInvariant()).ToArray();

                return new UserProfile
                {
                    Email = user.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    EmailVerified = user.EmailConfirmed,
                    ProfilePictureUrl = externalUserInfo.ProfilePictureUrl,
                    HasExternalLogins = externalLogins.Count > 0,
                    ExternalProviders = externalProviders,
                    AdditionalClaims = new Dictionary<string, string>(externalUserInfo.AdditionalClaims)
                    {
                        ["user_id"] = user.Id.ToString(),
                        ["username"] = user.UserName ?? "",
                        ["sign_in_count"] = user.SignInCount.ToString(),
                        ["last_sign_in"] = user.LastSignInAt?.ToString("O") ?? "",
                        ["external_logins_count"] = externalLogins.Count.ToString()
                    }
                };
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error building user profile for user {UserId}, using basic profile", user.Id);

                return new UserProfile
                {
                    Email = user.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    EmailVerified = user.EmailConfirmed,
                    ProfilePictureUrl = externalUserInfo.ProfilePictureUrl,
                    HasExternalLogins = true,
                    ExternalProviders = Array.Empty<string>(),
                    AdditionalClaims = new Dictionary<string, string>
                    {
                        ["user_id"] = user.Id.ToString(),
                        ["error"] = "profile_build_error"
                    }
                };
            }
        }

        private string GetClientIpAddress()
        {
            HttpContext? context = httpContextAccessor.HttpContext;
            if (context == null) return "unknown";

            string? forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            string? realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(realIp))
            {
                return realIp.Trim();
            }

            string? remoteIp = context.Connection.RemoteIpAddress?.ToString();
            return !string.IsNullOrWhiteSpace(remoteIp) ? remoteIp : "unknown";
        }
    }
}