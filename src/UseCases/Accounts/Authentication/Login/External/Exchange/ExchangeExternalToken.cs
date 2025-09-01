using Core.Identity;

using ErrorOr;

using Mapster;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Accounts.Common;
using UseCases.Common.Security.Authentication.Externals;
using UseCases.Common.Security.Authentication.Tokens.Models;
using UseCases.Common.Security.Authentication.Tokens.Services;

namespace UseCases.Accounts.Authentication.Login.External.Exchange;
public static partial class ExchangeExternalToken
{
    public const string Name = "ExchangeExternalToken";
    public const string Summary = "Exchange external provider token for application tokens";
    public const string Description = "Accepts an external provider token (from frontend OAuth) and exchanges it for application JWT tokens using official provider SDKs for validation";

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
        public UserProfile? UserProfile { get; set; }
    }

    public sealed record UserProfile
    {
        public string Email { get; init; } = null!;
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? ProfilePictureUrl { get; init; }
        public bool EmailVerified { get; init; }
        public string? GitHubUsername { get; init; } // For GitHub
    }

    public sealed class Handler(
        UserManager<User> userManager,
        ITokenManagementService tokenManagementService,
        IExternalTokenValidator tokenValidator,
        IHttpContextAccessor httpContextAccessor,
        ILogger<Handler> logger
    ) : ICommandHandler<Command, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {
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

            // Validate the external token using official provider SDKs
            var validationResult = await tokenValidator.ValidateTokenAsync(
                request.Provider,
                request.AccessToken,
                request.IdToken,
                request.AuthorizationCode,
                request.RedirectUri,
                cancellationToken
            );

            if (validationResult.IsError)
            {
                logger.LogWarning("Token validation failed for provider {Provider}: {Errors}",
                    request.Provider, string.Join(", ", validationResult.Errors.Select(e => e.Description)));
                return validationResult.Errors;
            }

            var externalUserInfo = validationResult.Value;

            // Find or create user
            var userResult = await FindOrCreateUserAsync(externalUserInfo, request.Provider);
            if (userResult.IsError)
            {
                return userResult.Errors;
            }

            var (user, isNewUser) = userResult.Value;

            // Generate application tokens
            var ipAddress = GetClientIpAddress();
            var tokens = await tokenManagementService.AuthenticateAsync(
                user,
                ipAddress,
                rememberMe: false,
                cancellationToken
            );

            if (tokens.IsError)
            {
                logger.LogError("Failed to generate tokens for user {UserId}: {Errors}",
                    user.Id, string.Join(", ", tokens.Errors.Select(e => e.Description)));
                return tokens.Errors;
            }
            var  result = tokens.Value.Adapt<Result>();
            result.IsNewUser = isNewUser;
            result.UserProfile = new UserProfile
            {
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                EmailVerified = user.EmailConfirmed,
                ProfilePictureUrl = externalUserInfo.ProfilePictureUrl,
                GitHubUsername = externalUserInfo.AdditionalClaims.GetValueOrDefault("login")
            };
            logger.LogInformation("Token exchange successful for user {UserId} via {Provider}",
                user.Id, request.Provider);

            return result;
        }

        private async Task<ErrorOr<(User User, bool IsNewUser)>> FindOrCreateUserAsync(
            ExternalUserInfo externalUserInfo,
            string provider)
        {
            // Try to find user by external login first
            var existingUser = await userManager.FindByLoginAsync(provider, externalUserInfo.ProviderId);
            if (existingUser != null)
            {
                await UpdateUserFromExternalInfo(existingUser, externalUserInfo);
                return (existingUser, false);
            }

            // Try to find user by email
            var userByEmail = await userManager.FindByEmailAsync(externalUserInfo.Email);
            if (userByEmail != null)
            {
                // Link external login to existing user
                var externalLoginInfo = new UserLoginInfo(
                    loginProvider: provider,
                    providerKey: externalUserInfo.ProviderId,
                    displayName: provider);
                var externalAddLoginResult = await userManager.AddLoginAsync(userByEmail, externalLoginInfo);

                if (!externalAddLoginResult.Succeeded)
                {
                    logger.LogError("Failed to link external login to existing user {Email}: {Errors}",
                        externalUserInfo.Email, string.Join(", ", externalAddLoginResult.Errors.Select(e => e.Description)));
                    return externalAddLoginResult.Errors.ToApplicationResult();
                }

                await UpdateUserFromExternalInfo(userByEmail, externalUserInfo);
                return (userByEmail, false);
            }

            // Create new user
            var newUser = new User
            {
                Email = externalUserInfo.Email,
                UserName = externalUserInfo.Email,
                EmailConfirmed = externalUserInfo.EmailVerified,
                FirstName = externalUserInfo.FirstName ?? externalUserInfo.Email.Split('@')[0],
                LastName = externalUserInfo.LastName
            };

            var createResult = await userManager.CreateAsync(newUser);
            if (!createResult.Succeeded)
            {
                logger.LogError("Failed to create user from external token {Email}: {Errors}",
                    externalUserInfo.Email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return createResult.Errors.ToApplicationResult();
            }

            // Add external login
            var newLoginInfo = new UserLoginInfo(provider, externalUserInfo.ProviderId, provider);
            var newAddLoginResult = await userManager.AddLoginAsync(newUser, newLoginInfo);
            if (!newAddLoginResult.Succeeded)
            {
                await userManager.DeleteAsync(newUser);
                logger.LogError("Failed to add external login to new user {Email}: {Errors}",
                    externalUserInfo.Email, string.Join(", ", newAddLoginResult.Errors.Select(e => e.Description)));
                return newAddLoginResult.Errors.ToApplicationResult();
            }

            logger.LogInformation("Created new user {UserId} from external token via {Provider}",
                newUser.Id, provider);

            return (newUser, true);
        }

        private async Task UpdateUserFromExternalInfo(User user, ExternalUserInfo externalUserInfo)
        {
            bool updated = false;

            if (!string.IsNullOrWhiteSpace(externalUserInfo.FirstName) && user.FirstName != externalUserInfo.FirstName)
            {
                user.FirstName = externalUserInfo.FirstName;
                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(externalUserInfo.LastName) && user.LastName != externalUserInfo.LastName)
            {
                user.LastName = externalUserInfo.LastName;
                updated = true;
            }

            if (updated)
            {
                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    logger.LogWarning("Failed to update user {UserId} from external info: {Errors}",
                        user.Id, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                }
            }
        }

        private string GetClientIpAddress()
        {
            var context = httpContextAccessor.HttpContext;
            if (context == null) return "unknown";

            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(realIp))
            {
                return realIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}