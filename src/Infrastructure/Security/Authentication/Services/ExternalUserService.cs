using Core.Identity.Users;

using ErrorOr;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using UseCases.Common.Security.Authentication.Externals;
using UseCases.Common.Security.Authentication.Services;

namespace Infrastructure.Security.Authentication.Services;

/// <summary>
/// Service for managing external user authentication and integration with Identity EF Core
/// </summary>
public sealed class ExternalUserService(
    UserManager<User> userManager,
    ILogger<ExternalUserService> logger)
    : IExternalUserService
{
    // Supported external providers for production e-commerce
    private static readonly HashSet<string> SupportedProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "google",
        "facebook"
    };

    /// <summary>
    /// Finds or creates a user based on external authentication information
    /// Handles all Identity EF Core operations for external logins
    /// </summary>
    public async Task<ErrorOr<(User User, bool IsNewUser, bool IsNewLogin)>> FindOrCreateUserWithExternalLoginAsync(
        ExternalUserInfo externalUserInfo,
        string provider,
        CancellationToken cancellationToken = default)
    {
        // Validate provider is supported
        if (!SupportedProviders.Contains(provider))
        {
            logger.LogWarning("Attempted to use unsupported provider: {Provider}", provider);
            return Error.Validation("Provider.NotSupported", $"Provider '{provider}' is not supported");
        }

        try
        {
            // Step 1: Try to find user by existing external login
            var existingUser = await FindUserByExternalLoginAsync(provider, externalUserInfo.ProviderId, cancellationToken);
            if (existingUser != null)
            {
                logger.LogDebug("Found existing user {UserId} with external login {Provider}:{ProviderId}",
                    existingUser.Id, provider, externalUserInfo.ProviderId);

                await UpdateUserFromExternalInfoAsync(existingUser, externalUserInfo, cancellationToken);
                return (existingUser, IsNewUser: false, IsNewLogin: false);
            }

            // Step 2: Try to find user by email and link the external login
            if (!string.IsNullOrWhiteSpace(externalUserInfo.Email) &&
                !externalUserInfo.Email.EndsWith("@facebook.local") &&
                !externalUserInfo.Email.EndsWith("@google.local"))
            {
                var userByEmail = await FindUserByEmailAsync(externalUserInfo.Email, cancellationToken);
                if (userByEmail != null)
                {
                    logger.LogDebug("Found existing user {UserId} by email, linking external login {Provider}:{ProviderId}",
                        userByEmail.Id, provider, externalUserInfo.ProviderId);

                    var linkResult = await LinkExternalLoginToUserAsync(userByEmail, provider, externalUserInfo, cancellationToken);
                    if (linkResult.IsError)
                    {
                        return linkResult.Errors;
                    }

                    await UpdateUserFromExternalInfoAsync(userByEmail, externalUserInfo, cancellationToken);
                    return (userByEmail, IsNewUser: false, IsNewLogin: true);
                }
            }

            // Step 3: Create new user with external login
            logger.LogDebug("Creating new user for external login {Provider}:{ProviderId}",
                provider, externalUserInfo.ProviderId);

            var createResult = await CreateUserWithExternalLoginAsync(externalUserInfo, provider, cancellationToken);
            if (createResult.IsError)
            {
                return createResult.Errors;
            }

            return (createResult.Value, IsNewUser: true, IsNewLogin: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during external user management for {Provider}:{ProviderId}",
                provider, externalUserInfo.ProviderId);
            return Error.Failure("ExternalUser.ManagementError", "Failed to manage external user authentication");
        }
    }

    /// <summary>
    /// Checks if a user already has an external login for a specific provider
    /// </summary>
    public async Task<bool> HasExternalLoginAsync(Guid userId, string provider, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return false;

            var logins = await userManager.GetLoginsAsync(user);
            return logins.Any(l => l.LoginProvider.Equals(provider, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking external login for user {UserId} and provider {Provider}", userId, provider);
            return false;
        }
    }

    /// <summary>
    /// Gets all external logins for a user
    /// </summary>
    public async Task<IList<UserLoginInfo>> GetExternalLoginsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return new List<UserLoginInfo>();

            return await userManager.GetLoginsAsync(user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting external logins for user {UserId}", userId);
            return new List<UserLoginInfo>();
        }
    }

    /// <summary>
    /// Removes an external login from a user (with safety checks)
    /// </summary>
    public async Task<ErrorOr<Success>> RemoveExternalLoginAsync(
        Guid userId,
        string provider,
        string providerKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Error.NotFound("User.NotFound", "User not found");
            }

            // Safety check: don't allow removal of last login method if user has no password
            var hasPassword = await userManager.HasPasswordAsync(user);
            var logins = await userManager.GetLoginsAsync(user);

            if (!hasPassword && logins.Count <= 1)
            {
                logger.LogWarning("Attempted to remove last external login for user {UserId} without password", userId);
                return Error.Validation("ExternalLogin.CannotRemoveLast",
                    "Cannot remove the last external login. Set a password first or add another external login.");
            }

            var result = await userManager.RemoveLoginAsync(user, provider, providerKey);
            if (!result.Succeeded)
            {
                logger.LogError("Failed to remove external login for user {UserId}: {Errors}",
                    userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                return Error.Failure("ExternalLogin.RemovalFailed", "Failed to remove external login");
            }

            logger.LogInformation("Successfully removed external login {Provider}:{ProviderKey} for user {UserId}",
                provider, providerKey, userId);

            return Result.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing external login for user {UserId}", userId);
            return Error.Failure("ExternalLogin.RemovalError", "Error occurred while removing external login");
        }
    }

    #region Private Helper Methods

    private async Task<User?> FindUserByExternalLoginAsync(
        string provider,
        string providerKey,
        CancellationToken cancellationToken)
    {
        try
        {
            return await userManager.FindByLoginAsync(provider, providerKey);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error finding user by external login {Provider}:{ProviderKey}", provider, providerKey);
            return null;
        }
    }

    private async Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        try
        {
            return await userManager.FindByEmailAsync(email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error finding user by email {Email}", email);
            return null;
        }
    }

    private async Task<ErrorOr<Success>> LinkExternalLoginToUserAsync(
        User user,
        string provider,
        ExternalUserInfo externalUserInfo,
        CancellationToken cancellationToken)
    {
        var externalLoginInfo = new UserLoginInfo(
            loginProvider: provider,
            providerKey: externalUserInfo.ProviderId,
            displayName: GetProviderDisplayName(provider));

        var result = await userManager.AddLoginAsync(user, externalLoginInfo);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to link external login to existing user {Email}: {Errors}",
                user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Error.Failure("ExternalLogin.LinkFailed", "Failed to link external login to existing user");
        }

        logger.LogInformation("Successfully linked external login {Provider}:{ProviderId} to user {UserId}",
            provider, externalUserInfo.ProviderId, user.Id);

        return Result.Success;
    }

    private async Task<ErrorOr<User>> CreateUserWithExternalLoginAsync(
        ExternalUserInfo externalUserInfo,
        string provider,
        CancellationToken cancellationToken)
    {
        // Create new user with external information
        var newUser = User.Create(
            email: externalUserInfo.Email,
            userName: GenerateUsername(externalUserInfo),
            emailConfirmed: externalUserInfo.EmailVerified,
            firstName: externalUserInfo.FirstName ?? ExtractFirstNameFromEmail(externalUserInfo.Email),
            lastName: externalUserInfo.LastName);

        // Create the user
        var createResult = await userManager.CreateAsync(newUser);
        if (!createResult.Succeeded)
        {
            logger.LogError("Failed to create user from external token {Email}: {Errors}",
                externalUserInfo.Email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return Error.Failure("User.CreationFailed", "Failed to create user from external authentication");
        }

        // Add external login to the new user
        var externalLoginInfo = new UserLoginInfo(
            provider,
            externalUserInfo.ProviderId,
            GetProviderDisplayName(provider));

        var addLoginResult = await userManager.AddLoginAsync(newUser, externalLoginInfo);
        if (!addLoginResult.Succeeded)
        {
            // Rollback: delete the created user if adding login fails
            await userManager.DeleteAsync(newUser);
            logger.LogError("Failed to add external login to new user {Email}: {Errors}",
                externalUserInfo.Email, string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
            return Error.Failure("ExternalLogin.AdditionFailed", "Failed to add external login to new user");
        }

        logger.LogInformation("Created new user {UserId} from external token via {Provider}",
            newUser.Id, provider);

        return newUser;
    }

    private async Task UpdateUserFromExternalInfoAsync(
        User user,
        ExternalUserInfo externalUserInfo,
        CancellationToken cancellationToken)
    {
        bool updated = false;

        // Update first name if provided and different
        if (!string.IsNullOrWhiteSpace(externalUserInfo.FirstName) &&
            user.FirstName != externalUserInfo.FirstName)
        {
            user.FirstName = externalUserInfo.FirstName;
            updated = true;
        }

        // Update last name if provided and different
        if (!string.IsNullOrWhiteSpace(externalUserInfo.LastName) &&
            user.LastName != externalUserInfo.LastName)
        {
            user.LastName = externalUserInfo.LastName;
            updated = true;
        }

        // Update email confirmation if external provider confirms it
        if (externalUserInfo.EmailVerified && !user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
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
            else
            {
                logger.LogDebug("Updated user {UserId} information from external provider", user.Id);
            }
        }
    }

    private static string GenerateUsername(ExternalUserInfo externalUserInfo)
    {
        // Use email as username if available, otherwise generate from provider info
        if (!string.IsNullOrWhiteSpace(externalUserInfo.Email) &&
            !externalUserInfo.Email.EndsWith(".local"))
        {
            return externalUserInfo.Email;
        }

        // Fallback: generate username from name or provider ID
        var baseName = !string.IsNullOrWhiteSpace(externalUserInfo.FirstName)
            ? externalUserInfo.FirstName.ToLowerInvariant()
            : "user";

        return $"{baseName}_{externalUserInfo.ProviderId[..Math.Min(8, externalUserInfo.ProviderId.Length)]}";
    }

    private static string ExtractFirstNameFromEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "User";

        var localPart = email.Split('@')[0];

        // Remove numbers and special characters, capitalize first letter
        var cleanName = new string(localPart.Where(char.IsLetter).ToArray());

        return string.IsNullOrEmpty(cleanName)
            ? "User"
            : char.ToUpperInvariant(cleanName[0]) + cleanName[1..].ToLowerInvariant();
    }

    private static string GetProviderDisplayName(string provider) =>
        provider.ToLowerInvariant() switch
        {
            "google" => "Google",
            "facebook" => "Facebook",
            _ => provider
        };

    #endregion
}