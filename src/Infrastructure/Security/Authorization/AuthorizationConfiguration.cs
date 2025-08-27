using Infrastructure.Security.Authorization.Options;
using Infrastructure.Security.Authorization.Policies;
using Infrastructure.Security.Authorization.Providers;
using Infrastructure.Security.Authorization.Requirements;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Security.Authorization;
public static class AuthorizationConfiguration
{
    public static IServiceCollection AddAuthorizationInternal(this IServiceCollection services, IConfiguration configuration)
    {
        // Register: AuthUser Cache Options
        services.AddOptions<AuthUserCacheOption>()
           .Bind(configuration.GetSection(AuthUserCacheOption.Section))
           .ValidateDataAnnotations()
           .ValidateOnStart();

        // Register: services for user authorization
        services.AddScoped<IUserAuthorizationProvider, UserAuthorizationProvider>();

        // Register: service for user context
        services.AddTransient<IAuthorizationHandler, HasAuthorizationRequirementHandler>();

        // Register: the custom policy provider
        services.AddSingleton<IAuthorizationPolicyProvider, HasAuthorizationPolicyProvider>();

        // Register: Authorization
        services.AddAuthorization();

        return services;
    }
}
