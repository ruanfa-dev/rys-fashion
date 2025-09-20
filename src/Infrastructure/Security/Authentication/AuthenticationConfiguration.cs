using System.Text;

using Ardalis.GuardClauses;

using Infrastructure.Security.Authentication.Contexts;
using Infrastructure.Security.Authentication.Externals.Validators;
using Infrastructure.Security.Authentication.Options;
using Infrastructure.Security.Authentication.Services;
using Infrastructure.Security.Authentication.Tokens.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Serilog;

using UseCases.Common.Security.Authentication.Contexts;
using UseCases.Common.Security.Authentication.Externals;
using UseCases.Common.Security.Authentication.Services;
using UseCases.Common.Security.Authentication.Tokens.Services;

namespace Infrastructure.Security.Authentication;

/// <summary>
/// Authentication configuration for Keycloak integration
/// </summary>
public static class AuthenticationConfiguration
{
    public static IServiceCollection AddAuthenticationInternal(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Keycloak Options with validation
        services.AddOptions<KeycloakOptions>()
            .Bind(configuration.GetSection(KeycloakOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Add Authentication with JWT Bearer
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var serviceProvider = services.BuildServiceProvider();
            var keycloakOptions = serviceProvider.GetRequiredService<IOptions<KeycloakOptions>>().Value;

            if (string.IsNullOrEmpty(keycloakOptions.Authority))
            {
                Log.Warning("Keycloak Authority is not configured. JWT authentication may not work properly.");
                return;
            }

            options.Authority = keycloakOptions.Authority;
            options.Audience = keycloakOptions.Audience;
            options.RequireHttpsMetadata = keycloakOptions.RequireHttpsMetadata;
            options.SaveToken = keycloakOptions.SaveTokens;
            options.IncludeErrorDetails = true;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = keycloakOptions.ValidateIssuer,
                ValidateAudience = keycloakOptions.ValidateAudience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = keycloakOptions.ClockSkew,

                // Keycloak-specific claims mapping
                RoleClaimType = "realm_access.roles",
                NameClaimType = "preferred_username"
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Log.Warning("JWT Authentication failed: {Error}", context.Exception?.Message);
                    return Task.CompletedTask;
                },

                OnTokenValidated = context =>
                {
                    Log.Debug("JWT Token validated for user: {User}", 
                        context.Principal?.Identity?.Name ?? "Unknown");
                    return Task.CompletedTask;
                },

                OnChallenge = context =>
                {
                    Log.Debug("JWT Authentication challenge: {Error}", context.Error);
                    return Task.CompletedTask;
                }
            };

            Log.Information("JWT Bearer authentication configured with Keycloak authority: {Authority}", keycloakOptions.Authority);
        });

        // Register Keycloak Admin Service
        RegisterKeycloakAdminService(services, configuration);

        Log.Information("Authentication configuration completed successfully");
        return services;
    }

    private static void RegisterKeycloakAdminService(IServiceCollection services, IConfiguration configuration)
    {
        try
        {
            var keycloakOptions = configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>();
            
            if (keycloakOptions != null && 
                !string.IsNullOrEmpty(keycloakOptions.AdminApiUrl) && 
                !string.IsNullOrEmpty(keycloakOptions.ClientSecret))
            {
                // Configure HttpClient for Keycloak Admin API
                services.AddHttpClient<IKeycloakAdminService, KeycloakAdminService>(client =>
                {
                    client.BaseAddress = new Uri(keycloakOptions.AdminApiUrl);
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.Timeout = TimeSpan.FromSeconds(30);
                })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    // For development - you might want to ignore SSL errors
                    // ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                });

                Log.Information("Keycloak Admin Service registered for user management");
            }
            else
            {
                // Register a placeholder service that throws NotSupportedException
                services.AddTransient<IKeycloakAdminService, UnsupportedKeycloakAdminService>();
                Log.Warning("Keycloak configuration is invalid or missing. Admin service not registered.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to register Keycloak Admin Service");
            services.AddTransient<IKeycloakAdminService, UnsupportedKeycloakAdminService>();
        }
    }
}

/// <summary>
/// Placeholder service for when Keycloak Admin API is not properly configured
/// </summary>
public class UnsupportedKeycloakAdminService : IKeycloakAdminService
{
    private static readonly NotSupportedException UnsupportedException = 
        new("Keycloak Admin API is not configured. Please check your Keycloak settings.");

    // All methods throw NotSupportedException
    public Task<UseCases.Common.Security.Authentication.Models.TokenResponse> GetTokenAsync(UseCases.Common.Security.Authentication.Models.TokenRequest request, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task<UseCases.Common.Security.Authentication.Models.TokenResponse> RefreshTokenAsync(UseCases.Common.Security.Authentication.Models.TokenRequest request, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task LogoutAsync(string token, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task<UseCases.Common.Security.Authentication.Models.UserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task<string> CreateUserAsync(UseCases.Common.Security.Authentication.Models.CreateKeycloakUserRequest request, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task<IEnumerable<UseCases.Common.Security.Authentication.Models.KeycloakUser>> GetUsersAsync(CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task<IEnumerable<UseCases.Common.Security.Authentication.Models.KeycloakUser>> SearchUsersAsync(string? search = null, string? username = null, string? email = null, string? firstName = null, string? lastName = null, bool? enabled = null, int? first = null, int? max = null, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task<UseCases.Common.Security.Authentication.Models.KeycloakUser?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task<UseCases.Common.Security.Authentication.Models.KeycloakUser?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task<UseCases.Common.Security.Authentication.Models.KeycloakUser?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task UpdateUserAsync(string userId, UseCases.Common.Security.Authentication.Models.UpdateKeycloakUserRequest request, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task SetUserPasswordAsync(string userId, string password, bool temporary = false, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task SetUserEnabledAsync(string userId, bool enabled, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task SendVerifyEmailAsync(string userId, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task<int> GetUserCountAsync(CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task CreateRoleAsync(UseCases.Common.Security.Authentication.Models.CreateKeycloakRoleRequest request, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task<IEnumerable<UseCases.Common.Security.Authentication.Models.KeycloakRole>> GetRolesAsync(CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task<UseCases.Common.Security.Authentication.Models.KeycloakRole?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task UpdateRoleAsync(string roleName, UseCases.Common.Security.Authentication.Models.UpdateKeycloakRoleRequest request, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task DeleteRoleAsync(string roleName, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task<IEnumerable<UseCases.Common.Security.Authentication.Models.KeycloakRole>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task AssignRolesToUserAsync(string userId, IEnumerable<string> roleNames, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task RemoveRolesFromUserAsync(string userId, IEnumerable<string> roleNames, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task<IEnumerable<UseCases.Common.Security.Authentication.Models.KeycloakRole>> GetAvailableRolesForUserAsync(string userId, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task<string> CreateGroupAsync(UseCases.Common.Security.Authentication.Models.CreateKeycloakGroupRequest request, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task<IEnumerable<UseCases.Common.Security.Authentication.Models.KeycloakGroup>> GetGroupsAsync(CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task<UseCases.Common.Security.Authentication.Models.KeycloakGroup?> GetGroupByIdAsync(string groupId, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task UpdateGroupAsync(string groupId, UseCases.Common.Security.Authentication.Models.UpdateKeycloakGroupRequest request, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task DeleteGroupAsync(string groupId, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task AddUserToGroupAsync(string userId, string groupId, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task RemoveUserFromGroupAsync(string userId, string groupId, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task<IEnumerable<UseCases.Common.Security.Authentication.Models.KeycloakGroup>> GetUserGroupsAsync(string userId, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task<IEnumerable<UseCases.Common.Security.Authentication.Models.KeycloakSession>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task LogoutUserSessionsAsync(string userId, CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task<UseCases.Common.Security.Authentication.Models.KeycloakRealmStats> GetRealmStatsAsync(CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task<IEnumerable<UseCases.Common.Security.Authentication.Models.KeycloakClient>> GetClientsAsync(CancellationToken cancellationToken = default)
        => throw UnsupportedException;

    public Task<IEnumerable<UseCases.Common.Security.Authentication.Models.KeycloakRole>> GetClientRolesAsync(string clientId, CancellationToken cancellationToken = default)
        => throw UnsupportedException;
}