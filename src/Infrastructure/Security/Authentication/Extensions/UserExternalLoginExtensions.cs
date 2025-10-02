using Core.Identity.Users;

using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Security.Authentication.Extensions;

/// <summary>
/// Extension methods for User entity to work with external logins
/// </summary>
public static class UserExternalLoginExtensions
{
    /// <summary>
    /// Gets a summary of external login information for a user
    /// </summary>
    public static async Task<ExternalLoginSummary> GetExternalLoginSummaryAsync(
        this User user,
        UserManager<User> userManager)
    {
        try
        {
            IList<UserLoginInfo> logins = await userManager.GetLoginsAsync(user);
            bool hasPassword = await userManager.HasPasswordAsync(user);

            return new ExternalLoginSummary
            {
                UserId = user.Id,
                HasPassword = hasPassword,
                ExternalLogins = logins.Select(l => new ExternalLoginInfo
                {
                    Provider = l.LoginProvider,
                    ProviderKey = l.ProviderKey,
                    DisplayName = l.ProviderDisplayName ?? l.LoginProvider
                }).ToList(),
                CanRemoveExternalLogins = hasPassword || logins.Count > 1,
                PrimaryAuthenticationMethod = hasPassword ? "Password" : 
                    logins.Count > 0 ? logins.First().LoginProvider : "None"
            };
        }
        catch
        {
            return new ExternalLoginSummary
            {
                UserId = user.Id,
                HasPassword = false,
                ExternalLogins = [],
                CanRemoveExternalLogins = false,
                PrimaryAuthenticationMethod = "Unknown"
            };
        }
    }

    /// <summary>
    /// Checks if a user can safely remove an external login
    /// </summary>
    public static async Task<bool> CanRemoveExternalLoginAsync(
        this User user,
        UserManager<User> userManager,
        string provider)
    {
        try
        {
            bool hasPassword = await userManager.HasPasswordAsync(user);
            if (hasPassword)
                return true;

            IList<UserLoginInfo> logins = await userManager.GetLoginsAsync(user);
            bool hasOtherLogins = logins.Any(l => !l.LoginProvider.Equals(provider, StringComparison.OrdinalIgnoreCase));
            
            return hasOtherLogins;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the display name for an external provider
    /// </summary>
    public static string GetProviderDisplayName(this string provider) =>
        provider.ToLowerInvariant() switch
        {
            "google" => "Google",
            "facebook" => "Facebook",
            "microsoft" => "Microsoft",
            "github" => "GitHub",
            "twitter" => "Twitter",
            "linkedin" => "LinkedIn",
            _ => provider
        };
}

/// <summary>
/// Summary of external login information for a user
/// </summary>
public sealed record ExternalLoginSummary
{
    public Guid UserId { get; init; }
    public bool HasPassword { get; init; }
    public List<ExternalLoginInfo> ExternalLogins { get; init; } = [];
    public bool CanRemoveExternalLogins { get; init; }
    public string PrimaryAuthenticationMethod { get; init; } = string.Empty;
    public int TotalLoginMethods => (HasPassword ? 1 : 0) + ExternalLogins.Count;
    public bool IsExternalOnly => !HasPassword && ExternalLogins.Count > 0;
    public string[] ProviderNames => ExternalLogins.Select(l => l.Provider.ToLowerInvariant()).ToArray();
}

/// <summary>
/// Information about an external login
/// </summary>
public sealed record ExternalLoginInfo
{
    public string Provider { get; init; } = string.Empty;
    public string ProviderKey { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}