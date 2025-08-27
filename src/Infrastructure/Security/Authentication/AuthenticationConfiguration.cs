using System.Text;

using Ardalis.GuardClauses;

using Infrastructure.Security.Authentication.Contexts;
using Infrastructure.Security.Authentication.Options;
using Infrastructure.Security.Authentication.Tokens;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using UseCases.Common.Security.Authentication.Contexts;
using UseCases.Common.Security.Authentication.Tokens;

namespace Infrastructure.Security.Authentication;

public static class AuthenticationConfiguration
{
    public static IServiceCollection AddAuthenticationInternal(this IServiceCollection services, IConfiguration configuration)
    {
        // Register: Authentication Options
        services.AddAuthenticationOptions(configuration);

        // Register: Authentication context
        services.AddAuthenticationContext();

        return services;
    }

    public static IServiceCollection AddAuthenticationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<GoogleOption>()
            .Bind(configuration.GetSection(GoogleOption.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<FacebookOption>()
            .Bind(configuration.GetSection(FacebookOption.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Resolve options for authentication setup
        var jwtOptions = configuration.GetSection(JwtOptions.Section).Get<JwtOptions>();
        Guard.Against.Null(jwtOptions, nameof(jwtOptions), "JWT options must be configured in appsettings.");
        var googleOptions = configuration.GetSection(GoogleOption.Section).Get<GoogleOption>();
        Guard.Against.Null(googleOptions, nameof(googleOptions), "Google authentication options must be configured in appsettings.");
        var facebookOptions = configuration.GetSection(FacebookOption.Section).Get<FacebookOption>();
        Guard.Against.Null(facebookOptions, nameof(facebookOptions), "Facebook authentication options must be configured in appsettings.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret))
            };
        })
        .AddGoogle(o =>
        {
            o.ClientId = googleOptions.ClientId;
            o.ClientSecret = googleOptions.ClientSecret;
            o.SaveTokens = true;
            o.CallbackPath = googleOptions.CallbackPath;
        })
        .AddFacebook(o =>
        {
            o.AppId = facebookOptions.AppId;
            o.AppSecret = facebookOptions.AppSecret;
            o.SaveTokens = true;
            o.CallbackPath = facebookOptions.CallbackPath;
        });

        return services;
    }

    public static IServiceCollection AddAuthenticationContext(this IServiceCollection services)
    {
        // Register: Authentication Context
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();

        // Register: JwtTokenService
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        return services;
    }
}
