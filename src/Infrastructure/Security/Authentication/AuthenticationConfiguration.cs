using System.Text;

using Ardalis.GuardClauses;

using Infrastructure.Security.Authentication.Contexts;
using Infrastructure.Security.Authentication.Externals.Validators;
using Infrastructure.Security.Authentication.Options;
using Infrastructure.Security.Authentication.Services;
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
using UseCases.Common.Security.Authentication.Services;
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
        // Configure JWT options (required)
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Configure external provider options (optional)
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
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict; // More secure for production
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Shorter session for security
        })
        .AddCookie(IdentityConstants.ExternalScheme, options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(15); // Short-lived external auth cookie
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = true;
            options.SaveToken = false; // Don't store tokens in AuthenticationProperties for security
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                ClockSkew = TimeSpan.FromMinutes(2), // Reduced clock skew for tighter security
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetService<ILogger<JwtBearerEvents>>();
                    logger?.LogWarning("JWT authentication failed: {Exception}", context.Exception.Message);
                    
                    // Clear any existing authentication
                    context.Response.Headers["Token-Expired"] = "true";
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetService<ILogger<JwtBearerEvents>>();
                    logger?.LogDebug("JWT token validated for user: {UserId}",
                        context.Principal?.FindFirst("sub")?.Value ?? "Unknown");
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetService<ILogger<JwtBearerEvents>>();
                    logger?.LogInformation("JWT authentication challenge triggered");
                    return Task.CompletedTask;
                }
            };
        });

        // Configure Google OAuth if credentials are available
        if (googleOptions != null && IsProviderConfigurationValid(googleOptions.ClientId, googleOptions.ClientSecret))
        {
            authBuilder.AddGoogle(options =>
            {
                options.ClientId = googleOptions.ClientId;
                options.ClientSecret = googleOptions.ClientSecret;
                
                // Essential scopes for e-commerce
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.SaveTokens = false; // Don't persist tokens for security
                
                // Security settings
                options.UsePkce = true; // Enable PKCE for better security
                options.CallbackPath = "/signin-google";
                
                options.Events.OnRedirectToAuthorizationEndpoint = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetService<ILogger>();
                    logger?.LogDebug("Redirecting to Google authorization endpoint");
                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
            });
        }

        // Configure Facebook OAuth if credentials are available
        if (facebookOptions != null && IsProviderConfigurationValid(facebookOptions.AppId, facebookOptions.AppSecret))
        {
            authBuilder.AddFacebook(options =>
            {
                options.AppId = facebookOptions.AppId;
                options.AppSecret = facebookOptions.AppSecret;
                
                // Essential scopes for e-commerce
                options.Scope.Clear();
                options.Scope.Add("email");
                options.Scope.Add("public_profile");
                
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.SaveTokens = false; // Don't persist tokens for security
                options.CallbackPath = "/signin-facebook";
                
                // Security fields
                options.Fields.Clear();
                options.Fields.Add("id");
                options.Fields.Add("email");
                options.Fields.Add("first_name");
                options.Fields.Add("last_name");
                options.Fields.Add("name");
                options.Fields.Add("picture.width(200).height(200)"); // Standardized picture size
                
                options.Events.OnRedirectToAuthorizationEndpoint = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetService<ILogger>();
                    logger?.LogDebug("Redirecting to Facebook authorization endpoint");
                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
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
        
        // Add enhanced external user service for Identity EF Core integration
        services.AddScoped<IExternalUserService, ExternalUserService>();
        
        return services;
    }

    public static IServiceCollection AddExternalTokenValidation(this IServiceCollection services)
    {
        // Configure HTTP clients with security best practices
        services.AddHttpClient<GoogleTokenValidator>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15); // Reduced timeout for better UX
            client.DefaultRequestHeaders.Add("User-Agent", "RysFashion.Shop/1.0");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddHttpClient<FacebookTokenValidator>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15); // Reduced timeout for better UX
            client.DefaultRequestHeaders.Add("User-Agent", "RysFashion.Shop/1.0");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // Register individual validators
        services.AddScoped<GoogleTokenValidator>();
        services.AddScoped<FacebookTokenValidator>();

        // Register composite validator (production-ready)
        services.AddScoped<IExternalTokenValidator, CompositeExternalTokenValidator>();

        return services;
    }
}