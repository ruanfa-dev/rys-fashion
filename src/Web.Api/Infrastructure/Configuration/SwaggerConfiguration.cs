using System.Text.Json;

using Infrastructure.Security.Authentication.Options;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

using Serilog;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace Web.Api.Infrastructure.Configuration;

public static class SwaggerConfiguration
{
    internal static IServiceCollection AddSwaggerWithAuth(this IServiceCollection services)
    {
        services.AddSwaggerGen(o =>
        {
            o.CustomSchemaIds(id => id.FullName!.Replace('+', '-'));
            o.SchemaFilter<SnakeCaseSchemaFilter>();
            o.ParameterFilter<SnakeCaseParameterFilter>();

            // Add security schemes
            o.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, CreateJwtSecurityScheme());
            o.AddSecurityDefinition("Google", CreateGoogleOAuth2Scheme());
            o.AddSecurityDefinition("Facebook", CreateFacebookOAuth2Scheme());

            // Add security requirements
            o.UseAllOfToExtendReferenceSchemas();
            o.AddSecurityRequirement(CreateJwtSecurityRequirement());
            o.AddSecurityRequirement(CreateGoogleSecurityRequirement());
            o.AddSecurityRequirement(CreateFacebookSecurityRequirement());
        });

        Log.Information("Register: Swagger with JWT, Google OAuth2, and Facebook OAuth2 authentication configuration.");
        return services;
    }

    internal static IApplicationBuilder UseSwaggerWithUi(this WebApplication app)
    {
        var googleOptions = app.Services.GetRequiredService<IOptions<GoogleOption>>().Value;
        var facebookOptions = app.Services.GetRequiredService<IOptions<FacebookOption>>().Value;
        
        app.UseSwagger(options => options.RouteTemplate = "/openapi/{documentName}.json");
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/openapi/v1.json", "Stellar FashionShop API V1");
            c.RoutePrefix = string.Empty;
            
            // OAuth2 configuration for Google (uses PKCE)
            c.OAuthClientId(googleOptions.ClientId);
            c.OAuthAppName("Stellar FashionShop API");
            c.OAuthUsePkce();
            c.OAuthScopeSeparator(" ");
            
            // OAuth2 configuration for Facebook
            c.OAuthAdditionalQueryStringParams(new Dictionary<string, string>
            {
                { "response_type", "code" },
                { "client_id", facebookOptions.AppId }
            });
            
            // Set OAuth2 redirect URL (adjust based on your configuration)
            c.OAuth2RedirectUrl($"{app.Configuration["BaseUrl"] ?? "https://localhost"}/swagger/oauth2-redirect.html");
            
            // For Facebook, if you need client secret (not recommended for public clients)
            // c.OAuthClientSecret(facebookOptions.ClientSecret);
        });

        Log.Information("Use: Swagger with UI and OAuth2 configuration.");
        return app;
    }

    private static OpenApiSecurityScheme CreateJwtSecurityScheme()
    {
        return new OpenApiSecurityScheme
        {
            Name = "JWT Authentication",
            Description = "Enter your JWT token in this field",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = JwtBearerDefaults.AuthenticationScheme,
            BearerFormat = "JWT"
        };
    }

    private static OpenApiSecurityScheme CreateGoogleOAuth2Scheme()
    {
        return new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Description = "Google OAuth2 Authentication",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri("https://accounts.google.com/o/oauth2/v2/auth"),
                    TokenUrl = new Uri("https://oauth2.googleapis.com/token"),
                    Scopes = new Dictionary<string, string>
                    {
                        { "openid", "OpenID Connect" },
                        { "profile", "User profile information" },
                        { "email", "User email address" }
                    }
                }
            }
        };
    }

    private static OpenApiSecurityScheme CreateFacebookOAuth2Scheme()
    {
        return new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Description = "Facebook OAuth2 Authentication",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri("https://www.facebook.com/v18.0/dialog/oauth"),
                    TokenUrl = new Uri("https://graph.facebook.com/v18.0/oauth/access_token"),
                    Scopes = new Dictionary<string, string>
                    {
                        { "openid", "OpenID Connect" },
                        { "email", "User email address" },
                        { "public_profile", "User public profile information" }
                    }
                }
            }
        };
    }

    private static OpenApiSecurityRequirement CreateJwtSecurityRequirement()
    {
        return new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = JwtBearerDefaults.AuthenticationScheme
                    }
                },
                new List<string>()
            }
        };
    }

    private static OpenApiSecurityRequirement CreateGoogleSecurityRequirement()
    {
        return new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Google"
                    }
                },
                new List<string> { "openid", "profile", "email" }
            }
        };
    }

    private static OpenApiSecurityRequirement CreateFacebookSecurityRequirement()
    {
        return new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Facebook"
                    }
                },
                new List<string> { "email", "public_profile" }
            }
        };
    }

    public sealed class SnakeCaseSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema.Properties == null) return;

            var propertiesToUpdate = schema.Properties.ToList();
            schema.Properties.Clear();

            foreach (var (key, value) in propertiesToUpdate)
            {
                var snakeCaseKey = JsonNamingPolicy.SnakeCaseLower.ConvertName(key);
                schema.Properties[snakeCaseKey] = value;
            }
        }
    }

    public sealed class SnakeCaseParameterFilter : IParameterFilter
    {
        public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
        {
            if (parameter.In == ParameterLocation.Query)
            {
                parameter.Name = JsonNamingPolicy.SnakeCaseLower.ConvertName(parameter.Name);
            }
        }
    }
}