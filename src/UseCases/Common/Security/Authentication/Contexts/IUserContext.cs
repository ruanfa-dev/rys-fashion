namespace UseCases.Common.Security.Authentication.Contexts;

public interface IUserContext
{
    /// <summary>
    /// Gets the current user's ID from the 'sub' claim (Keycloak subject)
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets the current user's username from 'preferred_username' claim (Keycloak)
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// Gets the current user's email from 'email' claim
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Indicates whether the current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets all roles assigned to the current user from Keycloak
    /// </summary>
    IEnumerable<string> Roles { get; }

    /// <summary>
    /// Gets all permissions assigned to the current user
    /// </summary>
    IEnumerable<string> Permissions { get; }

    /// <summary>
    /// Checks if the user has a specific role
    /// </summary>
    /// <param name="role">Role name to check</param>
    /// <returns>True if user has the role</returns>
    bool HasRole(string role);

    /// <summary>
    /// Checks if the user has a specific permission
    /// </summary>
    /// <param name="permission">Permission to check</param>
    /// <returns>True if user has the permission</returns>
    bool HasPermission(string permission);

    /// <summary>
    /// Checks if the user has any of the specified roles
    /// </summary>
    /// <param name="roles">Roles to check</param>
    /// <returns>True if user has at least one role</returns>
    bool HasAnyRole(params string[] roles);

    /// <summary>
    /// Checks if the user has all of the specified roles
    /// </summary>
    /// <param name="roles">Roles to check</param>
    /// <returns>True if user has all roles</returns>
    bool HasAllRoles(params string[] roles);

    /// <summary>
    /// Gets a specific claim value from the user's token
    /// </summary>
    /// <param name="claimType">Claim type to retrieve</param>
    /// <returns>Claim value or null</returns>
    string? GetClaimValue(string claimType);

    /// <summary>
    /// Gets all values for a specific claim type
    /// </summary>
    /// <param name="claimType">Claim type to retrieve</param>
    /// <returns>Collection of claim values</returns>
    IEnumerable<string> GetClaimValues(string claimType);
}
