using UseCases.Common.Security.Authentication.Models;

namespace UseCases.Common.Security.Authentication.Services;

/// <summary>
/// Service interface for Keycloak Admin API operations including authentication, user management, and role management
/// </summary>
public interface IKeycloakAdminService
{
    #region Authentication Operations
    
    /// <summary>
    /// Authenticates user and returns access tokens using password grant type
    /// </summary>
    /// <param name="request">Token request with username and password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token response with access and refresh tokens</returns>
    Task<TokenResponse> GetTokenAsync(TokenRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Refreshes access token using refresh token
    /// </summary>
    /// <param name="request">Token request with refresh token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New token response</returns>
    Task<TokenResponse> RefreshTokenAsync(TokenRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Logs out user and invalidates tokens
    /// </summary>
    /// <param name="token">Access or refresh token to invalidate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogoutAsync(string token, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets user information from access token
    /// </summary>
    /// <param name="accessToken">Valid access token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User information from token</returns>
    Task<UserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken = default);
    
    #endregion

    #region External Token Exchange

    /// <summary>
    /// Exchanges an external token for Keycloak tokens
    /// </summary>
    /// <param name="request">External token exchange request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token response with access and refresh tokens</returns>
    Task<TokenResponse> ExchangeExternalTokenAsync(ExternalTokenExchangeRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Links an external account to a Keycloak user
    /// </summary>
    /// <param name="userId">User ID to link the external account to</param>
    /// <param name="provider">External provider name</param>
    /// <param name="externalToken">External token for authentication</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LinkExternalAccountAsync(string userId, string provider, string externalToken, CancellationToken cancellationToken = default);

    #endregion

    #region User Management Operations

    /// <summary>
    /// Creates a new user in Keycloak realm
    /// </summary>
    /// <param name="request">User creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user ID</returns>
    Task<string> CreateUserAsync(CreateKeycloakUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users from Keycloak realm
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of users</returns>
    Task<IEnumerable<KeycloakUser>> GetUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users with advanced search parameters
    /// </summary>
    /// <param name="search">Search term</param>
    /// <param name="username">Username filter</param>
    /// <param name="email">Email filter</param>
    /// <param name="firstName">First name filter</param>
    /// <param name="lastName">Last name filter</param>
    /// <param name="enabled">Enabled status filter</param>
    /// <param name="first">First result index</param>
    /// <param name="max">Maximum results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of users matching criteria</returns>
    Task<IEnumerable<KeycloakUser>> SearchUsersAsync(
        string? search = null,
        string? username = null, 
        string? email = null,
        string? firstName = null,
        string? lastName = null,
        bool? enabled = null,
        int? first = null,
        int? max = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific user by ID from Keycloak
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User details or null if not found</returns>
    Task<KeycloakUser?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by username from Keycloak
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User details or null if not found</returns>
    Task<KeycloakUser?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by email from Keycloak
    /// </summary>
    /// <param name="email">Email address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User details or null if not found</returns>
    Task<KeycloakUser?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user in Keycloak
    /// </summary>
    /// <param name="userId">User ID to update</param>
    /// <param name="request">Update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateUserAsync(string userId, UpdateKeycloakUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user from Keycloak realm
    /// </summary>
    /// <param name="userId">User ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets user password in Keycloak
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="password">New password</param>
    /// <param name="temporary">Whether password is temporary</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetUserPasswordAsync(string userId, string password, bool temporary = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="enabled">Enable or disable</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetUserEnabledAsync(string userId, bool enabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends verification email to user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendVerifyEmailAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user count in realm
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total user count</returns>
    Task<int> GetUserCountAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Role Management Operations

    /// <summary>
    /// Creates a new role in Keycloak realm
    /// </summary>
    /// <param name="request">Role creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CreateRoleAsync(CreateKeycloakRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles from Keycloak realm
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of roles</returns>
    Task<IEnumerable<KeycloakRole>> GetRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific role by name from Keycloak
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Role details or null if not found</returns>
    Task<KeycloakRole?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing role in Keycloak
    /// </summary>
    /// <param name="roleName">Role name to update</param>
    /// <param name="request">Update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateRoleAsync(string roleName, UpdateKeycloakRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a role from Keycloak realm
    /// </summary>
    /// <param name="roleName">Role name to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteRoleAsync(string roleName, CancellationToken cancellationToken = default);

    #endregion

    #region User-Role Assignment Operations

    /// <summary>
    /// Gets roles assigned to a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user roles</returns>
    Task<IEnumerable<KeycloakRole>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns roles to a user in Keycloak
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="roleNames">Role names to assign</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AssignRolesToUserAsync(string userId, IEnumerable<string> roleNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes roles from a user in Keycloak
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="roleNames">Role names to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveRolesFromUserAsync(string userId, IEnumerable<string> roleNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available roles that can be assigned to a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available roles</returns>
    Task<IEnumerable<KeycloakRole>> GetAvailableRolesForUserAsync(string userId, CancellationToken cancellationToken = default);

    #endregion

    #region Group Management Operations

    /// <summary>
    /// Creates a new group in Keycloak realm
    /// </summary>
    /// <param name="request">Group creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created group ID</returns>
    Task<string> CreateGroupAsync(CreateKeycloakGroupRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all groups from Keycloak realm
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of groups</returns>
    Task<IEnumerable<KeycloakGroup>> GetGroupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific group by ID from Keycloak
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Group details or null if not found</returns>
    Task<KeycloakGroup?> GetGroupByIdAsync(string groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing group in Keycloak
    /// </summary>
    /// <param name="groupId">Group ID to update</param>
    /// <param name="request">Update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateGroupAsync(string groupId, UpdateKeycloakGroupRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a group from Keycloak realm
    /// </summary>
    /// <param name="groupId">Group ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteGroupAsync(string groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a user to a group
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="groupId">Group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddUserToGroupAsync(string userId, string groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a user from a group
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="groupId">Group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveUserFromGroupAsync(string userId, string groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets groups that a user belongs to
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user groups</returns>
    Task<IEnumerable<KeycloakGroup>> GetUserGroupsAsync(string userId, CancellationToken cancellationToken = default);

    #endregion

    #region Session Management Operations

    /// <summary>
    /// Gets active user sessions
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active sessions</returns>
    Task<IEnumerable<KeycloakSession>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out all user sessions
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogoutUserSessionsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets realm statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Realm statistics</returns>
    Task<KeycloakRealmStats> GetRealmStatsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Client Management Operations

    /// <summary>
    /// Gets all clients in the realm
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of clients</returns>
    Task<IEnumerable<KeycloakClient>> GetClientsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets client roles for a specific client
    /// </summary>
    /// <param name="clientId">Client ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of client roles</returns>
    Task<IEnumerable<KeycloakRole>> GetClientRolesAsync(string clientId, CancellationToken cancellationToken = default);

    #endregion
}