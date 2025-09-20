using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Infrastructure.Security.Authentication.Options;

using Microsoft.Extensions.Options;

using UseCases.Common.Security.Authentication.Services;
using UseCases.Common.Security.Authentication.Models;

namespace Infrastructure.Security.Authentication.Services;

public class KeycloakAdminService : IKeycloakAdminService
{
    private readonly HttpClient _httpClient;
    private readonly KeycloakOptions _keycloakOptions;

    public KeycloakAdminService(HttpClient httpClient, IOptions<KeycloakOptions> keycloakOptions)
    {
        _httpClient = httpClient;
        _keycloakOptions = keycloakOptions.Value;
    }

    #region Authentication Operations

    public async Task<TokenResponse> GetTokenAsync(TokenRequest request, CancellationToken cancellationToken = default)
    {
        var tokenEndpoint = $"{_keycloakOptions.Authority}/protocol/openid-connect/token";
        
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", request.GrantType),
            new KeyValuePair<string, string>("client_id", request.ClientId),
            new KeyValuePair<string, string>("client_secret", _keycloakOptions.ClientSecret),
            new KeyValuePair<string, string>("username", request.Username ?? string.Empty),
            new KeyValuePair<string, string>("password", request.Password ?? string.Empty),
            new KeyValuePair<string, string>("scope", request.Scope ?? "openid profile email")
        });

        var response = await _httpClient.PostAsync(tokenEndpoint, formContent, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent) ?? new Dictionary<string, object>();

        return new TokenResponse
        {
            AccessToken = tokenData.TryGetValue("access_token", out var accessToken) ? accessToken?.ToString() ?? string.Empty : string.Empty,
            RefreshToken = tokenData.TryGetValue("refresh_token", out var refreshToken) ? refreshToken?.ToString() : null,
            TokenType = tokenData.TryGetValue("token_type", out var tokenType) ? tokenType?.ToString() ?? "Bearer" : "Bearer",
            ExpiresIn = tokenData.TryGetValue("expires_in", out var expiresIn) ? Convert.ToInt32(expiresIn) : 0,
            RefreshExpiresIn = tokenData.TryGetValue("refresh_expires_in", out var refreshExpiresIn) ? Convert.ToInt32(refreshExpiresIn) : 0,
            Scope = tokenData.TryGetValue("scope", out var scope) ? scope?.ToString() : null
        };
    }

    public async Task<TokenResponse> RefreshTokenAsync(TokenRequest request, CancellationToken cancellationToken = default)
    {
        var tokenEndpoint = $"{_keycloakOptions.Authority}/protocol/openid-connect/token";
        
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("client_id", request.ClientId),
            new KeyValuePair<string, string>("client_secret", _keycloakOptions.ClientSecret),
            new KeyValuePair<string, string>("refresh_token", request.RefreshToken ?? string.Empty)
        });

        var response = await _httpClient.PostAsync(tokenEndpoint, formContent, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent) ?? new Dictionary<string, object>();

        return new TokenResponse
        {
            AccessToken = tokenData.TryGetValue("access_token", out var accessToken) ? accessToken?.ToString() ?? string.Empty : string.Empty,
            RefreshToken = tokenData.TryGetValue("refresh_token", out var refreshToken) ? refreshToken?.ToString() : null,
            TokenType = tokenData.TryGetValue("token_type", out var tokenType) ? tokenType?.ToString() ?? "Bearer" : "Bearer",
            ExpiresIn = tokenData.TryGetValue("expires_in", out var expiresIn) ? Convert.ToInt32(expiresIn) : 0
        };
    }

    public async Task LogoutAsync(string token, CancellationToken cancellationToken = default)
    {
        var logoutEndpoint = $"{_keycloakOptions.Authority}/protocol/openid-connect/logout";
        
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _keycloakOptions.ClientId),
            new KeyValuePair<string, string>("client_secret", _keycloakOptions.ClientSecret),
            new KeyValuePair<string, string>("refresh_token", token)
        });

        await _httpClient.PostAsync(logoutEndpoint, formContent, cancellationToken);
    }

    public async Task<UserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var userInfoEndpoint = $"{_keycloakOptions.Authority}/protocol/openid-connect/userinfo";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.GetAsync(userInfoEndpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var userInfoData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent) ?? new Dictionary<string, object>();

        return new UserInfo
        {
            Subject = userInfoData.TryGetValue("sub", out var sub) ? sub?.ToString() ?? string.Empty : string.Empty,
            PreferredUsername = userInfoData.TryGetValue("preferred_username", out var username) ? username?.ToString() ?? string.Empty : string.Empty,
            Email = userInfoData.TryGetValue("email", out var email) ? email?.ToString() ?? string.Empty : string.Empty,
            EmailVerified = userInfoData.TryGetValue("email_verified", out var emailVerified) ? Convert.ToBoolean(emailVerified) : false,
            GivenName = userInfoData.TryGetValue("given_name", out var givenName) ? givenName?.ToString() ?? string.Empty : string.Empty,
            FamilyName = userInfoData.TryGetValue("family_name", out var familyName) ? familyName?.ToString() ?? string.Empty : string.Empty,
            Name = userInfoData.TryGetValue("name", out var name) ? name?.ToString() ?? string.Empty : string.Empty,
            RealmAccess = userInfoData.TryGetValue("realm_access", out var realmAccess) && realmAccess != null ? 
                JsonSerializer.Deserialize<RealmAccess>(realmAccess.ToString() ?? "{}") : 
                new RealmAccess { Roles = new List<string>() }
        };
    }

    #endregion

    #region User Management Operations

    public async Task<IEnumerable<KeycloakUser>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/users";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var users = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonContent) ?? new List<Dictionary<string, object>>();

        return users.Select(u => new KeycloakUser
        {
            Id = u.TryGetValue("id", out var id) ? id?.ToString() ?? string.Empty : string.Empty,
            Username = u.TryGetValue("username", out var username) ? username?.ToString() ?? string.Empty : string.Empty,
            Email = u.TryGetValue("email", out var email) ? email?.ToString() ?? string.Empty : string.Empty,
            FirstName = u.TryGetValue("firstName", out var firstName) ? firstName?.ToString() ?? string.Empty : string.Empty,
            LastName = u.TryGetValue("lastName", out var lastName) ? lastName?.ToString() ?? string.Empty : string.Empty,
            Enabled = u.TryGetValue("enabled", out var enabled) ? Convert.ToBoolean(enabled) : false,
            EmailVerified = u.TryGetValue("emailVerified", out var emailVerified) ? Convert.ToBoolean(emailVerified) : false,
            CreatedTimestamp = u.TryGetValue("createdTimestamp", out var createdTimestamp) ? Convert.ToInt64(createdTimestamp) : 0
        });
    }

    public async Task<IEnumerable<KeycloakUser>> SearchUsersAsync(
        string? search = null,
        string? username = null,
        string? email = null,
        string? firstName = null,
        string? lastName = null,
        bool? enabled = null,
        int? first = null,
        int? max = null,
        CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/users";
        
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(search)) queryParams.Add($"search={Uri.EscapeDataString(search)}");
        if (!string.IsNullOrEmpty(username)) queryParams.Add($"username={Uri.EscapeDataString(username)}");
        if (!string.IsNullOrEmpty(email)) queryParams.Add($"email={Uri.EscapeDataString(email)}");
        if (!string.IsNullOrEmpty(firstName)) queryParams.Add($"firstName={Uri.EscapeDataString(firstName)}");
        if (!string.IsNullOrEmpty(lastName)) queryParams.Add($"lastName={Uri.EscapeDataString(lastName)}");
        if (enabled.HasValue) queryParams.Add($"enabled={enabled.Value.ToString().ToLower()}");
        if (first.HasValue) queryParams.Add($"first={first.Value}");
        if (max.HasValue) queryParams.Add($"max={max.Value}");

        if (queryParams.Count > 0)
        {
            endpoint += "?" + string.Join("&", queryParams);
        }

        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var users = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonContent) ?? new List<Dictionary<string, object>>();

        return users.Select(u => new KeycloakUser
        {
            Id = u.TryGetValue("id", out var id) ? id?.ToString() ?? string.Empty : string.Empty,
            Username = u.TryGetValue("username", out var username) ? username?.ToString() ?? string.Empty : string.Empty,
            Email = u.TryGetValue("email", out var email) ? email?.ToString() ?? string.Empty : string.Empty,
            FirstName = u.TryGetValue("firstName", out var firstName) ? firstName?.ToString() ?? string.Empty : string.Empty,
            LastName = u.TryGetValue("lastName", out var lastName) ? lastName?.ToString() ?? string.Empty : string.Empty,
            Enabled = u.TryGetValue("enabled", out var enabled) ? Convert.ToBoolean(enabled) : false,
            EmailVerified = u.TryGetValue("emailVerified", out var emailVerified) ? Convert.ToBoolean(emailVerified) : false,
            CreatedTimestamp = u.TryGetValue("createdTimestamp", out var createdTimestamp) ? Convert.ToInt64(createdTimestamp) : 0
        });
    }

    public async Task<KeycloakUser?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/users/{userId}";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var user = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);

            if (user == null) return null;

            return new KeycloakUser
            {
                Id = user.TryGetValue("id", out var id) ? id?.ToString() ?? string.Empty : string.Empty,
                Username = user.TryGetValue("username", out var username) ? username?.ToString() ?? string.Empty : string.Empty,
                Email = user.TryGetValue("email", out var email) ? email?.ToString() ?? string.Empty : string.Empty,
                FirstName = user.TryGetValue("firstName", out var firstName) ? firstName?.ToString() ?? string.Empty : string.Empty,
                LastName = user.TryGetValue("lastName", out var lastName) ? lastName?.ToString() ?? string.Empty : string.Empty,
                Enabled = user.TryGetValue("enabled", out var enabled) ? Convert.ToBoolean(enabled) : false,
                EmailVerified = user.TryGetValue("emailVerified", out var emailVerified) ? Convert.ToBoolean(emailVerified) : false,
                CreatedTimestamp = user.TryGetValue("createdTimestamp", out var createdTimestamp) ? Convert.ToInt64(createdTimestamp) : 0
            };
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<KeycloakUser?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var users = await SearchUsersAsync(username: username, max: 1, cancellationToken: cancellationToken);
        return users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<KeycloakUser?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var users = await SearchUsersAsync(email: email, max: 1, cancellationToken: cancellationToken);
        return users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<string> CreateUserAsync(CreateKeycloakUserRequest request, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/users";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var userPayload = new
        {
            username = request.Username,
            email = request.Email,
            firstName = request.FirstName,
            lastName = request.LastName,
            enabled = request.Enabled,
            emailVerified = request.EmailVerified,
            attributes = request.Attributes
        };

        var jsonContent = JsonSerializer.Serialize(userPayload);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Extract user ID from location header
        var locationHeader = response.Headers.Location?.ToString();
        return locationHeader?.Split('/').LastOrDefault() ?? string.Empty;
    }

    public async Task UpdateUserAsync(string userId, UpdateKeycloakUserRequest request, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/users/{userId}";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var userPayload = new
        {
            email = request.Email,
            firstName = request.FirstName,
            lastName = request.LastName,
            enabled = request.Enabled,
            emailVerified = request.EmailVerified,
            attributes = request.Attributes
        };

        var jsonContent = JsonSerializer.Serialize(userPayload);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync(endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/users/{userId}";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.DeleteAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task SetUserPasswordAsync(string userId, string password, bool temporary = false, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/users/{userId}/reset-password";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var passwordPayload = new
        {
            type = "password",
            value = password,
            temporary = temporary
        };

        var jsonContent = JsonSerializer.Serialize(passwordPayload);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync(endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task SetUserEnabledAsync(string userId, bool enabled, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/users/{userId}";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var userPayload = new { enabled = enabled };
        var jsonContent = JsonSerializer.Serialize(userPayload);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync(endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task SendVerifyEmailAsync(string userId, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/users/{userId}/send-verify-email";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.PutAsync(endpoint, null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<int> GetUserCountAsync(CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/users/count";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return int.TryParse(content, out var count) ? count : 0;
    }

    #endregion

    #region Role Management Operations

    public async Task<IEnumerable<KeycloakRole>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/roles";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var roles = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonContent) ?? new List<Dictionary<string, object>>();

        return roles.Select(r => new KeycloakRole
        {
            Id = r.TryGetValue("id", out var id) ? id?.ToString() ?? string.Empty : string.Empty,
            Name = r.TryGetValue("name", out var name) ? name?.ToString() ?? string.Empty : string.Empty,
            Description = r.TryGetValue("description", out var description) ? description?.ToString() : null,
            Composite = r.TryGetValue("composite", out var composite) ? Convert.ToBoolean(composite) : false,
            ClientRole = r.TryGetValue("clientRole", out var clientRole) ? Convert.ToBoolean(clientRole) : false,
            ContainerId = r.TryGetValue("containerId", out var containerId) ? containerId?.ToString() : null
        });
    }

    public async Task<KeycloakRole?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/roles/{roleName}";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var role = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);

            if (role == null) return null;

            return new KeycloakRole
            {
                Id = role.TryGetValue("id", out var id) ? id?.ToString() ?? string.Empty : string.Empty,
                Name = role.TryGetValue("name", out var name) ? name?.ToString() ?? string.Empty : string.Empty,
                Description = role.TryGetValue("description", out var description) ? description?.ToString() : null,
                Composite = role.TryGetValue("composite", out var composite) ? Convert.ToBoolean(composite) : false,
                ClientRole = role.TryGetValue("clientRole", out var clientRole) ? Convert.ToBoolean(clientRole) : false,
                ContainerId = role.TryGetValue("containerId", out var containerId) ? containerId?.ToString() : null
            };
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task CreateRoleAsync(CreateKeycloakRoleRequest request, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/roles";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var rolePayload = new
        {
            name = request.Name,
            description = request.Description,
            composite = request.Composite
        };

        var jsonContent = JsonSerializer.Serialize(rolePayload);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateRoleAsync(string roleName, UpdateKeycloakRoleRequest request, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/roles/{roleName}";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var rolePayload = new
        {
            name = request.Name,
            description = request.Description,
            composite = request.Composite
        };

        var jsonContent = JsonSerializer.Serialize(rolePayload);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync(endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/roles/{roleName}";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.DeleteAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    #endregion

    #region User-Role Assignment Operations

    public async Task<IEnumerable<KeycloakRole>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/users/{userId}/role-mappings/realm";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var roles = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonContent) ?? new List<Dictionary<string, object>>();

        return roles.Select(r => new KeycloakRole
        {
            Id = r.TryGetValue("id", out var id) ? id?.ToString() ?? string.Empty : string.Empty,
            Name = r.TryGetValue("name", out var name) ? name?.ToString() ?? string.Empty : string.Empty,
            Description = r.TryGetValue("description", out var description) ? description?.ToString() : null,
            Composite = r.TryGetValue("composite", out var composite) ? Convert.ToBoolean(composite) : false,
            ClientRole = r.TryGetValue("clientRole", out var clientRole) ? Convert.ToBoolean(clientRole) : false
        });
    }

    public async Task AssignRolesToUserAsync(string userId, IEnumerable<string> roleNames, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/users/{userId}/role-mappings/realm";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var roles = new List<object>();
        foreach (var roleName in roleNames)
        {
            var role = await GetRoleByNameAsync(roleName, cancellationToken);
            if (role is not null)
            {
                roles.Add(new { id = role.Id, name = role.Name });
            }
        }

        var jsonContent = JsonSerializer.Serialize(roles);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveRolesFromUserAsync(string userId, IEnumerable<string> roleNames, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/users/{userId}/role-mappings/realm";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var roles = new List<object>();
        foreach (var roleName in roleNames)
        {
            var role = await GetRoleByNameAsync(roleName, cancellationToken);
            if (role is not null)
            {
                roles.Add(new { id = role.Id, name = role.Name });
            }
        }

        var request = new HttpRequestMessage(HttpMethod.Delete, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(roles), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IEnumerable<KeycloakRole>> GetAvailableRolesForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/users/{userId}/role-mappings/realm/available";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var roles = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonContent) ?? new List<Dictionary<string, object>>();

        return roles.Select(r => new KeycloakRole
        {
            Id = r.TryGetValue("id", out var id) ? id?.ToString() ?? string.Empty : string.Empty,
            Name = r.TryGetValue("name", out var name) ? name?.ToString() ?? string.Empty : string.Empty,
            Description = r.TryGetValue("description", out var description) ? description?.ToString() : null,
            Composite = r.TryGetValue("composite", out var composite) ? Convert.ToBoolean(composite) : false,
            ClientRole = r.TryGetValue("clientRole", out var clientRole) ? Convert.ToBoolean(clientRole) : false
        });
    }

    #endregion

    #region Group Management Operations

    public async Task<string> CreateGroupAsync(CreateKeycloakGroupRequest request, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/groups";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var groupPayload = new
        {
            name = request.Name,
            path = request.Path,
            attributes = request.Attributes
        };

        var jsonContent = JsonSerializer.Serialize(groupPayload);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Extract group ID from location header
        var locationHeader = response.Headers.Location?.ToString();
        return locationHeader?.Split('/').LastOrDefault() ?? string.Empty;
    }

    public async Task<IEnumerable<KeycloakGroup>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/groups";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var groups = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonContent) ?? new List<Dictionary<string, object>>();

        return groups.Select(g => new KeycloakGroup
        {
            Id = g.TryGetValue("id", out var id) ? id?.ToString() ?? string.Empty : string.Empty,
            Name = g.TryGetValue("name", out var name) ? name?.ToString() ?? string.Empty : string.Empty,
            Path = g.TryGetValue("path", out var path) ? path?.ToString() ?? string.Empty : string.Empty
        });
    }

    public async Task<KeycloakGroup?> GetGroupByIdAsync(string groupId, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/groups/{groupId}";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var group = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);

            if (group == null) return null;

            return new KeycloakGroup
            {
                Id = group.TryGetValue("id", out var id) ? id?.ToString() ?? string.Empty : string.Empty,
                Name = group.TryGetValue("name", out var name) ? name?.ToString() ?? string.Empty : string.Empty,
                Path = group.TryGetValue("path", out var path) ? path?.ToString() ?? string.Empty : string.Empty
            };
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task UpdateGroupAsync(string groupId, UpdateKeycloakGroupRequest request, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/groups/{groupId}";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var groupPayload = new
        {
            name = request.Name,
            attributes = request.Attributes
        };

        var jsonContent = JsonSerializer.Serialize(groupPayload);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync(endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteGroupAsync(string groupId, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/groups/{groupId}";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.DeleteAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task AddUserToGroupAsync(string userId, string groupId, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/users/{userId}/groups/{groupId}";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.PutAsync(endpoint, null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveUserFromGroupAsync(string userId, string groupId, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/users/{userId}/groups/{groupId}";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.DeleteAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IEnumerable<KeycloakGroup>> GetUserGroupsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/users/{userId}/groups";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var groups = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonContent) ?? new List<Dictionary<string, object>>();

        return groups.Select(g => new KeycloakGroup
        {
            Id = g.TryGetValue("id", out var id) ? id?.ToString() ?? string.Empty : string.Empty,
            Name = g.TryGetValue("name", out var name) ? name?.ToString() ?? string.Empty : string.Empty,
            Path = g.TryGetValue("path", out var path) ? path?.ToString() ?? string.Empty : string.Empty
        });
    }

    #endregion

    #region Session Management Operations

    public async Task<IEnumerable<KeycloakSession>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/users/{userId}/sessions";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var sessions = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonContent) ?? new List<Dictionary<string, object>>();

        return sessions.Select(s => new KeycloakSession
        {
            Id = s.TryGetValue("id", out var id) ? id?.ToString() ?? string.Empty : string.Empty,
            Username = s.TryGetValue("username", out var username) ? username?.ToString() ?? string.Empty : string.Empty,
            UserId = s.TryGetValue("userId", out var sessionUserId) ? sessionUserId?.ToString() ?? string.Empty : string.Empty,
            IpAddress = s.TryGetValue("ipAddress", out var ipAddress) ? ipAddress?.ToString() ?? string.Empty : string.Empty,
            Start = s.TryGetValue("start", out var start) ? Convert.ToInt64(start) : 0,
            LastAccess = s.TryGetValue("lastAccess", out var lastAccess) ? Convert.ToInt64(lastAccess) : 0
        });
    }

    public async Task LogoutUserSessionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/users/{userId}/logout";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.PostAsync(endpoint, null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<KeycloakRealmStats> GetRealmStatsAsync(CancellationToken cancellationToken = default)
    {
        // This would typically require multiple API calls to gather statistics
        var userCount = await GetUserCountAsync(cancellationToken);
        var allUsers = await GetUsersAsync(cancellationToken);
        var roles = await GetRolesAsync(cancellationToken);
        var groups = await GetGroupsAsync(cancellationToken);
        var clients = await GetClientsAsync(cancellationToken);

        var enabledUsers = allUsers.Count(u => u.Enabled);
        var disabledUsers = userCount - enabledUsers;

        return new KeycloakRealmStats
        {
            TotalUsers = userCount,
            EnabledUsers = enabledUsers,
            DisabledUsers = disabledUsers,
            TotalRoles = roles.Count(),
            TotalGroups = groups.Count(),
            TotalClients = clients.Count(),
            ActiveSessions = 0, // Would need separate API call to get session count
            LastUpdated = DateTimeOffset.UtcNow
        };
    }

    #endregion

    #region Client Management Operations

    public async Task<IEnumerable<KeycloakClient>> GetClientsAsync(CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/clients";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var clients = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonContent) ?? new List<Dictionary<string, object>>();

        return clients.Select(c => new KeycloakClient
        {
            Id = c.TryGetValue("id", out var id) ? id?.ToString() ?? string.Empty : string.Empty,
            ClientId = c.TryGetValue("clientId", out var clientId) ? clientId?.ToString() ?? string.Empty : string.Empty,
            Name = c.TryGetValue("name", out var name) ? name?.ToString() ?? string.Empty : string.Empty,
            Description = c.TryGetValue("description", out var description) ? description?.ToString() : null,
            Enabled = c.TryGetValue("enabled", out var enabled) ? Convert.ToBoolean(enabled) : false,
            ServiceAccountsEnabled = c.TryGetValue("serviceAccountsEnabled", out var serviceAccountsEnabled) ? Convert.ToBoolean(serviceAccountsEnabled) : false,
            PublicClient = c.TryGetValue("publicClient", out var publicClient) ? Convert.ToBoolean(publicClient) : false,
            Protocol = c.TryGetValue("protocol", out var protocol) ? protocol?.ToString() : null
        });
    }

    public async Task<IEnumerable<KeycloakRole>> GetClientRolesAsync(string clientId, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/clients/{clientId}/roles";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var roles = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonContent) ?? new List<Dictionary<string, object>>();

        return roles.Select(r => new KeycloakRole
        {
            Id = r.TryGetValue("id", out var id) ? id?.ToString() ?? string.Empty : string.Empty,
            Name = r.TryGetValue("name", out var name) ? name?.ToString() ?? string.Empty : string.Empty,
            Description = r.TryGetValue("description", out var description) ? description?.ToString() : null,
            Composite = r.TryGetValue("composite", out var composite) ? Convert.ToBoolean(composite) : false,
            ClientRole = true,
            ContainerId = r.TryGetValue("containerId", out var containerId) ? containerId?.ToString() : null
        });
    }

    #endregion

    #region Token Exchange Operations

    /// <summary>
    /// Exchanges an external OAuth token (Google/Facebook) for a Keycloak token
    /// </summary>
    public async Task<TokenResponse> ExchangeExternalTokenAsync(ExternalTokenExchangeRequest request, CancellationToken cancellationToken = default)
    {
        var tokenEndpoint = $"{_keycloakOptions.Authority}/protocol/openid-connect/token";
        
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:token-exchange"),
            new KeyValuePair<string, string>("client_id", _keycloakOptions.ClientId),
            new KeyValuePair<string, string>("client_secret", _keycloakOptions.ClientSecret),
            new KeyValuePair<string, string>("subject_token", request.ExternalToken),
            new KeyValuePair<string, string>("subject_token_type", "urn:ietf:params:oauth:token-type:access_token"),
            new KeyValuePair<string, string>("subject_issuer", request.Provider), // 'google' or 'facebook'
            new KeyValuePair<string, string>("scope", "openid profile email")
        });

        var response = await _httpClient.PostAsync(tokenEndpoint, formContent, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Token exchange failed: {response.StatusCode} - {errorContent}");
        }

        var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent) ?? new Dictionary<string, object>();

        return new TokenResponse
        {
            AccessToken = tokenData.TryGetValue("access_token", out var accessToken) ? accessToken?.ToString() ?? string.Empty : string.Empty,
            RefreshToken = tokenData.TryGetValue("refresh_token", out var refreshToken) ? refreshToken?.ToString() : null,
            TokenType = tokenData.TryGetValue("token_type", out var tokenType) ? tokenType?.ToString() ?? "Bearer" : "Bearer",
            ExpiresIn = tokenData.TryGetValue("expires_in", out var expiresIn) ? Convert.ToInt32(expiresIn) : 0,
            Scope = tokenData.TryGetValue("scope", out var scope) ? scope?.ToString() : null
        };
    }

    /// <summary>
    /// Links an external account to an existing Keycloak user
    /// </summary>
    public async Task LinkExternalAccountAsync(string userId, string provider, string externalToken, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdminTokenAsync();
        var endpoint = $"{_keycloakOptions.AdminApiUrl}/admin/realms/{_keycloakOptions.Realm}/users/{userId}/federated-identity/{provider}";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        // Get external user info first
        var externalUserInfo = await GetExternalUserInfoAsync(provider, externalToken, cancellationToken);
        
        var linkPayload = new
        {
            identityProvider = provider,
            userId = externalUserInfo.ProviderId,
            userName = externalUserInfo.Email ?? externalUserInfo.Username
        };

        var jsonContent = JsonSerializer.Serialize(linkPayload);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Gets external user information from the external token
    /// </summary>
    private async Task<ExternalUserInfo> GetExternalUserInfoAsync(string provider, string externalToken, CancellationToken cancellationToken)
    {
        return provider.ToLowerInvariant() switch
        {
            "google" => await GetGoogleUserInfoAsync(externalToken, cancellationToken),
            "facebook" => await GetFacebookUserInfoAsync(externalToken, cancellationToken),
            _ => throw new ArgumentException($"Unsupported provider: {provider}")
        };
    }

    /// <summary>
    /// Gets user information from Google using the access token
    /// </summary>
    private async Task<ExternalUserInfo> GetGoogleUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        
        var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo", cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var userInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent) ?? new Dictionary<string, object>();
        
        return new ExternalUserInfo
        {
            ProviderId = userInfo.TryGetValue("id", out var id) ? id?.ToString() ?? string.Empty : string.Empty,
            Email = userInfo.TryGetValue("email", out var email) ? email?.ToString() : null,
            Username = userInfo.TryGetValue("email", out var username) ? username?.ToString() ?? string.Empty : string.Empty,
            FirstName = userInfo.TryGetValue("given_name", out var firstName) ? firstName?.ToString() : null,
            LastName = userInfo.TryGetValue("family_name", out var lastName) ? lastName?.ToString() : null,
            EmailVerified = userInfo.TryGetValue("verified_email", out var verified) ? Convert.ToBoolean(verified) : false,
            PictureUrl = userInfo.TryGetValue("picture", out var picture) ? picture?.ToString() : null
        };
    }

    /// <summary>
    /// Gets user information from Facebook using the access token
    /// </summary>
    private async Task<ExternalUserInfo> GetFacebookUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        
        var requestUrl = $"https://graph.facebook.com/me?fields=id,email,first_name,last_name,picture&access_token={accessToken}";
        var response = await httpClient.GetAsync(requestUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var userInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent) ?? new Dictionary<string, object>();
        
        return new ExternalUserInfo
        {
            ProviderId = userInfo.TryGetValue("id", out var id) ? id?.ToString() ?? string.Empty : string.Empty,
            Email = userInfo.TryGetValue("email", out var email) ? email?.ToString() : null,
            Username = userInfo.TryGetValue("email", out var username) ? username?.ToString() ?? string.Empty : string.Empty,
            FirstName = userInfo.TryGetValue("first_name", out var firstName) ? firstName?.ToString() : null,
            LastName = userInfo.TryGetValue("last_name", out var lastName) ? lastName?.ToString() : null,
            EmailVerified = true, // Facebook requires verified email for API access
            PictureUrl = ExtractFacebookPicture(userInfo)
        };
    }

    /// <summary>
    /// Extracts Facebook profile picture URL from user info
    /// </summary>
    private static string? ExtractFacebookPicture(Dictionary<string, object> userInfo)
    {
        if (userInfo.TryGetValue("picture", out var pictureObj) && pictureObj != null)
        {
            try
            {
                var pictureData = JsonSerializer.Deserialize<Dictionary<string, object>>(pictureObj.ToString() ?? "{}");
                if (pictureData?.TryGetValue("data", out var dataObj) == true && dataObj != null)
                {
                    var data = JsonSerializer.Deserialize<Dictionary<string, object>>(dataObj.ToString() ?? "{}");
                    if (data?.TryGetValue("url", out var urlObj) == true)
                    {
                        return urlObj?.ToString();
                    }
                }
            }
            catch
            {
                // Ignore JSON parsing errors for profile picture
            }
        }
        return null;
    }

    #endregion

    private async Task<string> GetAdminTokenAsync()
    {
        var tokenEndpoint = $"{_keycloakOptions.Authority}/protocol/openid-connect/token";
        
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", _keycloakOptions.ClientId),
            new KeyValuePair<string, string>("client_secret", _keycloakOptions.ClientSecret)
        });

        var response = await _httpClient.PostAsync(tokenEndpoint, formContent);
        response.EnsureSuccessStatusCode();

        var jsonContent = await response.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent) ?? new Dictionary<string, object>();

        return tokenData.TryGetValue("access_token", out var accessToken) ? accessToken?.ToString() ?? throw new InvalidOperationException("Failed to obtain admin token") : throw new InvalidOperationException("Failed to obtain admin token");
    }
}