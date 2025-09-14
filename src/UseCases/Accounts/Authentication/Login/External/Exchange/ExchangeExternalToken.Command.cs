using Core.Identity;

using ErrorOr;

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
    public sealed record Command(
        string? Provider = null,
        string? AccessToken = null,
        string? IdToken = null,
        string? AuthorizationCode = null,
        string? RedirectUri = null
    ) : ICommand<Result>;

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
        ITokenManagementService tokenManagementService,
        IExternalTokenValidator tokenValidator,
        IExternalUserService externalUserService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<Handler> logger
    ) : ICommandHandler<Command, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Validate input parameters
            if (string.IsNullOrWhiteSpace(request.Provider))
            {
                return Error.Validation("Provider.Required", "Provider is required");
            }

            if (string.IsNullOrWhiteSpace(request.AccessToken) &&
                string.IsNullOrWhiteSpace(request.IdToken) &&
                string.IsNullOrWhiteSpace(request.AuthorizationCode))
            {
                return Error.Validation("Token.Required", "Either access token, ID token, or authorization code is required");
            }

            var provider = request.Provider.ToLowerInvariant().Trim();

            try
            {
                // Step 1: Validate the external token using official provider SDKs
                logger.LogDebug("Validating external token for provider: {Provider}", provider);
                var validationResult = await tokenValidator.ValidateTokenAsync(
                    provider,
                    request.AccessToken,
                    request.IdToken,
                    request.AuthorizationCode,
                    request.RedirectUri,
                    cancellationToken
                );

                if (validationResult.IsError)
                {
                    logger.LogWarning("Token validation failed for provider {Provider}: {Errors}",
                        provider, string.Join(", ", validationResult.Errors.Select(e => e.Description)));
                    return validationResult.Errors;
                }

                var externalUserInfo = validationResult.Value;
                logger.LogDebug("Successfully validated token for user: {Email} from provider: {Provider}",
                    externalUserInfo.Email, provider);

                // Step 2: Find or create user using enhanced external user service
                var userResult = await externalUserService.FindOrCreateUserWithExternalLoginAsync(
                    externalUserInfo,
                    provider,
                    cancellationToken);

                if (userResult.IsError)
                {
                    logger.LogError("Failed to find or create user for provider {Provider}: {Errors}",
                        provider, string.Join(", ", userResult.Errors.Select(e => e.Description)));
                    return userResult.Errors;
                }

                var (user, isNewUser, isNewLogin) = userResult.Value;

                // Step 3: Record sign-in for tracking
                user.RecordSignIn(GetClientIpAddress());
                await userManager.UpdateAsync(user);

                // Step 4: Generate application tokens
                var tokens = await tokenManagementService.AuthenticateAsync(
                    user,
                    GetClientIpAddress(),
                    rememberMe: false,
                    cancellationToken
                );

                if (tokens.IsError)
                {
                    logger.LogError("Failed to generate tokens for user {UserId}: {Errors}",
                        user.Id, string.Join(", ", tokens.Errors.Select(e => e.Description)));
                    return tokens.Errors;
                }

                // Step 5: Build comprehensive user profile
                var userProfile = await BuildUserProfileAsync(user, externalUserInfo, cancellationToken);

                var result = tokens.Value.Adapt<Result>();
                result.IsNewUser = isNewUser;
                result.IsNewLogin = isNewLogin;
                result.UserProfile = userProfile;

                logger.LogInformation("External token exchange successful for user {UserId} via {Provider}. NewUser: {IsNewUser}, NewLogin: {IsNewLogin}",
                    user.Id, provider, isNewUser, isNewLogin);

                return result;
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("External token exchange was cancelled for provider: {Provider}", provider);
                return Error.Failure("TokenExchange.Cancelled", "Token exchange operation was cancelled");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during external token exchange for provider: {Provider}", provider);
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
                // Get all external logins for the user
                var externalLogins = await externalUserService.GetExternalLoginsAsync(user.Id, cancellationToken);
                var externalProviders = externalLogins.Select(l => l.LoginProvider.ToLowerInvariant()).ToArray();

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

                // Fallback to basic profile if there's an error
                return new UserProfile
                {
                    Email = user.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    EmailVerified = user.EmailConfirmed,
                    ProfilePictureUrl = externalUserInfo.ProfilePictureUrl,
                    HasExternalLogins = true, // Assume true since we just used external auth
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
            var context = httpContextAccessor.HttpContext;
            if (context == null) return "unknown";

            // Check for forwarded IP (common in production behind load balancers)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                // Take the first IP if there are multiple (client -> proxy1 -> proxy2 -> server)
                return forwardedFor.Split(',')[0].Trim();
            }

            // Check for real IP header (some proxy configurations)
            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(realIp))
            {
                return realIp.Trim();
            }

            // Fallback to connection remote IP
            var remoteIp = context.Connection.RemoteIpAddress?.ToString();
            return !string.IsNullOrWhiteSpace(remoteIp) ? remoteIp : "unknown";
        }
    }
}