using System.Text;

using Ardalis.GuardClauses;

using Infrastructure.Security.Authentication.Contexts;
using Infrastructure.Security.Authentication.Externals.Validators;
using Infrastructure.Security.Authentication.Options;
using Infrastructure.Security.Authentication.Tokens.Services;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

using UseCases.Common.Security.Authentication.Contexts;
using UseCases.Common.Security.Authentication.Externals;
using UseCases.Common.Security.Authentication.Tokens.Services;

namespace Infrastructure.Security.Authentication;

public static class AuthenticationConfiguration
{
    public static IServiceCollection AddAuthenticationInternal(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthenticationOptions(configuration);
        services.AddAuthenticationContext();
        services.AddExternalTokenValidation();
        return services;
    }

    public static IServiceCollection AddAuthenticationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<GoogleOption>()
            .BindConfiguration(GoogleOption.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<FacebookOption>()
            .BindConfiguration(FacebookOption.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var jwtOptions = GetRequiredOptions<JwtOptions>(configuration, JwtOptions.Section);
        var googleOptions = GetOptionalOptions<GoogleOption>(configuration, GoogleOption.Section);
        var facebookOptions = GetOptionalOptions<FacebookOption>(configuration, FacebookOption.Section);

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(IdentityConstants.ExternalScheme, options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetService<ILogger<JwtBearerEvents>>();
                    logger?.LogWarning("JWT authentication failed: {Exception}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetService<ILogger<JwtBearerEvents>>();
                    logger?.LogDebug("JWT token validated for user: {UserId}",
                        context.Principal?.FindFirst("sub")?.Value ?? "Unknown");
                    return Task.CompletedTask;
                }
            };
        });

        if (googleOptions != null && IsProviderConfigurationValid(googleOptions.ClientId, googleOptions.ClientSecret))
        {
            authBuilder.AddGoogle(options =>
            {
                options.ClientId = googleOptions.ClientId;
                options.ClientSecret = googleOptions.ClientSecret;
                options.Scope.Add("profile");
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.SaveTokens = true;
            });
        }

        if (facebookOptions != null && IsProviderConfigurationValid(facebookOptions.AppId, facebookOptions.AppSecret))
        {
            authBuilder.AddFacebook(options =>
            {
                options.AppId = facebookOptions.AppId;
                options.AppSecret = facebookOptions.AppSecret;
                options.Scope.Add("profile");
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.SaveTokens = true;
            });
        }
        return services;
    }

    private static T GetRequiredOptions<T>(IConfiguration configuration, string sectionName) where T : class, new()
    {
        var options = configuration.GetSection(sectionName).Get<T>();
        Guard.Against.Null(options, nameof(options), $"{typeof(T).Name} options must be configured in appsettings at section '{sectionName}'.");
        return options;
    }

    private static T? GetOptionalOptions<T>(IConfiguration configuration, string sectionName) where T : class, new()
    {
        var section = configuration.GetSection(sectionName);
        return section.Exists() ? section.Get<T>() : null;
    }

    private static bool IsProviderConfigurationValid(string? clientId, string? clientSecret)
    {
        return !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret);
    }

    public static IServiceCollection AddAuthenticationContext(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<ITokenManagementService, TokenManagementService>();
        return services;
    }

    public static IServiceCollection AddExternalTokenValidation(this IServiceCollection services)
    {
        // Add HTTP clients for token validators
        services.AddHttpClient<GoogleTokenValidator>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "Rys.Shop/1.0");
        });

        services.AddHttpClient<FacebookTokenValidator>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "Rys.Shop/1.0");
        });

        // Register individual validators
        services.AddScoped<GoogleTokenValidator>();
        services.AddScoped<FacebookTokenValidator>();

        // Register composite validator
        services.AddScoped<IExternalTokenValidator, CompositeExternalTokenValidator>();

        return services;
    }
}