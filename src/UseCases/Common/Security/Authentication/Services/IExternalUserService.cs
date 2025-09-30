using Core.Identity.Users;

using ErrorOr;

using Microsoft.AspNetCore.Identity;

using UseCases.Common.Security.Authentication.Externals;

namespace UseCases.Common.Security.Authentication.Services;

/// <summary>
/// Interface for managing external user authentication and integration with Identity
/// </summary>
public interface IExternalUserService
{
    /// <summary>
    /// Finds or creates a user based on external authentication information
    /// Handles all Identity operations for external logins
    /// </summary>
    Task<ErrorOr<(User User, bool IsNewUser, bool IsNewLogin)>> FindOrCreateUserWithExternalLoginAsync(
        ExternalUserInfo externalUserInfo,
        string provider,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user already has an external login for a specific provider
    /// </summary>
    Task<bool> HasExternalLoginAsync(Guid userId, string provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all external logins for a user
    /// </summary>
    Task<IList<UserLoginInfo>> GetExternalLoginsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an external login from a user (with safety checks)
    /// </summary>
    Task<ErrorOr<Success>> RemoveExternalLoginAsync(
        Guid userId,
        string provider,
        string providerKey,
        CancellationToken cancellationToken = default);
}