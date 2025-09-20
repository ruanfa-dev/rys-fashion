using Infrastructure.Security.Authorization.Options;
using Infrastructure.Security.Authorization.Policies;
using Infrastructure.Security.Authorization.Providers;
using Infrastructure.Security.Authorization.Requirements;
using Infrastructure.Security.Authorization.Seeders;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using UseCases.Common.Security.Authorization.Providers;

namespace Infrastructure.Security.Authorization;

/// <summary>
/// Authorization configuration for Keycloak resource and scope-based authorization
/// </summary>
public static class AuthorizationConfiguration
{
    public static IServiceCollection AddAuthorizationInternal(this IServiceCollection services, IConfiguration configuration)
    {
        // Register: AuthUser Cache Options
        services.AddOptions<AuthUserCacheOption>()
           .Bind(configuration.GetSection(AuthUserCacheOption.Section))
           .ValidateDataAnnotations()
           .ValidateOnStart();

        // Register: Keycloak-based user authorization provider (replaces EF Identity version)
        services.AddScoped<IUserAuthorizationProvider, KeycloakUserAuthorizationProvider>();

        // Register: Authorization requirement handler for resource/scope validation
        services.AddTransient<IAuthorizationHandler, HasAuthorizationRequirementHandler>();

        // Register: Dynamic policy provider for resource-based authorization
        services.AddSingleton<IAuthorizationPolicyProvider, HasAuthorizationPolicyProvider>();

        // Register: Keycloak authorization seeder for resource-scope management
        services.AddScoped<KeycloakAuthorizationSeeder>();

        // Register: Authorization with minimal configuration
        // Policies will be dynamically created based on resources and scopes
        services.AddAuthorization();

        return services;
    }
}
