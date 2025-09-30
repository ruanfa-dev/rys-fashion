using Ardalis.GuardClauses;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using UseCases.Common.Systems.Options;

namespace Infrastructure.Systems;
public static class SystemConfiguration
{
    public static IServiceCollection AddSystems(this IServiceCollection services, IConfiguration configuration)
    {
        // Add: section options
        // Add: admin panel options
        IConfigurationSection adminPanelOption = configuration.GetSection(AdminPanelOption.Section);
        Guard.Against.Null(adminPanelOption, message: "System options section not found in configuration.");
        services.Configure<AdminPanelOption>(adminPanelOption);

        // Add: storefront options
        IConfigurationSection storeOption = configuration.GetSection(StorefrontOption.Section);
        Guard.Against.Null(storeOption, message: "Store options section not found in configuration.");
        services.Configure<StorefrontOption>(storeOption);

        return services;
    }

}
