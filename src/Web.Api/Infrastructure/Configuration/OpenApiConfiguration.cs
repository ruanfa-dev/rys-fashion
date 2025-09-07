using System.Text.Json;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Web.Api.Infrastructure.Configuration;

public static class OpenApiConfiguration
{
    public static IServiceCollection AddOpenApiWithAuth(this IServiceCollection services)
    {
        services.AddOpenApi(options => options
            .AddDocumentTransformer<MultiAuthSecuritySchemeTransformer>()
            .AddDocumentTransformer<SnakeCaseSchemaTransformer>()
            .AddOperationTransformer<SnakeCaseParameterTransformer>());
        return services;
    }

    internal sealed class MultiAuthSecuritySchemeTransformer() : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>
            {
                [JwtBearerDefaults.AuthenticationScheme] = CreateJwtSecurityScheme(),
                ["Google"] = CreateGoogleOAuth2Scheme(),
                ["Facebook"] = CreateFacebookOAuth2Scheme()
            };

            // Apply security requirements for all operations
            foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
            {
                operation.Value.Security.Add(CreateJwtSecurityRequirement());
                operation.Value.Security.Add(CreateGoogleSecurityRequirement());
                operation.Value.Security.Add(CreateFacebookSecurityRequirement());
            }

            return Task.CompletedTask;
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
                    Array.Empty<string>()
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
                    new[] { "openid", "profile", "email" }
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
                    new[] { "email", "public_profile" }
                }
            };
        }
    }

    internal sealed class SnakeCaseSchemaTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            // Transform component schemas
            if (document.Components?.Schemas != null)
            {
                foreach (var schema in document.Components.Schemas.Values)
                {
                    TransformSchema(schema);
                }
            }

            return Task.CompletedTask;
        }

        private void TransformSchema(OpenApiSchema schema)
        {
            if (schema.Properties != null)
            {
                var propertiesToUpdate = schema.Properties.ToList();
                schema.Properties.Clear();

                foreach (var (key, value) in propertiesToUpdate)
                {
                    var snakeCaseKey = ToSnakeCase(key);
                    schema.Properties[snakeCaseKey] = value;
                    TransformSchema(value);
                }
            }

            if (schema.Items != null)
            {
                TransformSchema(schema.Items);
            }

            if (schema.AdditionalProperties is OpenApiSchema additionalPropsSchema)
            {
                TransformSchema(additionalPropsSchema);
            }
        }

        private string ToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return JsonNamingPolicy.SnakeCaseLower.ConvertName(input);
        }
    }

    internal sealed class SnakeCaseParameterTransformer : IOpenApiOperationTransformer
    {
        public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            // Transform query parameter names to snake_case
            if (operation.Parameters != null)
            {
                foreach (var parameter in operation.Parameters.Where(p => p.In == ParameterLocation.Query))
                {
                    parameter.Name = ToSnakeCase(parameter.Name);
                }
            }

            return Task.CompletedTask;
        }

        private string ToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return JsonNamingPolicy.SnakeCaseLower.ConvertName(input);
        }
    }
}
