using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

using System.Text.Json;

namespace Web.Api.Infrastructure.Configuration;

public static class OpenApiConfiguration
{
    public static IServiceCollection AddOpenApiWithAuth(this IServiceCollection services)
    {
        services.AddOpenApi(options => options
            .AddDocumentTransformer<BearerSecuritySchemeTransformer>()
            .AddDocumentTransformer<SnakeCaseSchemaTransformer>());
        return services;
    }

    internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
    {
        public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
            if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
            {
                // Add the security scheme at the document level
                var requirements = new Dictionary<string,
                    OpenApiSecurityScheme>
                {
                    ["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        In = ParameterLocation.Header,
                        BearerFormat = "Json Web Token"
                    }
                };
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = requirements;

                // Apply it as a requirement for all operations
                foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
                {
                    operation.Value.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Id = "Bearer",
                                Type = ReferenceType.SecurityScheme
                            }
                        }] = Array.Empty<string>()
                    });
                }
            }
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

            // Transform parameter names in paths
            foreach (var path in document.Paths.Values)
            {
                foreach (var operation in path.Operations.Values)
                {
                    if (operation.Parameters != null)
                    {
                        foreach (var parameter in operation.Parameters.Where(p => p.In == ParameterLocation.Query))
                        {
                            parameter.Name = ToSnakeCase(parameter.Name);
                        }
                    }
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
}