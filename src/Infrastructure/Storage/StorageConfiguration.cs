using Infrastructure.Storage.Options;
using Infrastructure.Storage.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using UseCases.Common.Storage.Services;

namespace Infrastructure.Storage;

public static class StorageConfiguration
{
    public static IServiceCollection AddStorageServices(
        this IServiceCollection services,
        IConfiguration configuration,
        Microsoft.AspNetCore.Hosting.IWebHostEnvironment environment)
    {
        // Register and validate storage options
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.Section))
                .AddOptionsWithValidateOnStart<StorageOptions>()
                .Validate(options => ValidateStorageOptions(options, environment),
                         "Storage configuration is invalid");

        // Register appropriate storage service based on environment
        if (environment.IsDevelopment())
        {
            services.AddScoped<IStorageService, LocalStorageService>();
        }
        else
        {
            services.AddScoped<IStorageService, AzureStorageService>();
        }

        return services;
    }

    private static bool ValidateStorageOptions(StorageOptions options, Microsoft.AspNetCore.Hosting.IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            return ValidateLocalStorageOptions(options);
        }
        else
        {
            return ValidateAzureStorageOptions(options);
        }
    }

    private static bool ValidateLocalStorageOptions(StorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.LocalPath))
            return false;

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            return false;

        if (options.MaxFileSizeBytes <= 0)
            return false;

        if (options.AllowedExtensions?.Length == 0)
            return false;

        return true;
    }

    private static bool ValidateAzureStorageOptions(StorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.AzureConnectionString))
            return false;

        if (string.IsNullOrWhiteSpace(options.AzureContainerName))
            return false;

        if (options.MaxFileSizeBytes <= 0)
            return false;

        if (options.AllowedExtensions?.Length == 0)
            return false;

        return true;
    }
}