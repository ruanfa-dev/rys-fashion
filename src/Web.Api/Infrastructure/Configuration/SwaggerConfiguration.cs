using System.Text.Json;

using Microsoft.AspNetCore.Authentication.JwtBearer;
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

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "JWT Authentication",
                Description = "Enter your JWT token in this field",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                BearerFormat = "JWT"
            };

            o.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);

            var securityRequirement = new OpenApiSecurityRequirement
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

            o.UseAllOfToExtendReferenceSchemas();
            o.AddSecurityRequirement(securityRequirement);
        });

        Log.Information("Register: Swagger with authentication configuration.");
        return services;
    }

    internal static IApplicationBuilder UseSwaggerWithUi(this WebApplication app)
    {
        app.UseSwagger(options => options.RouteTemplate = "/openapi/{documentName}.json");
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/openapi/v1.json", "Stellar FashionShop API V1");
            c.RoutePrefix = string.Empty;
        });

        Log.Information("Use: Swagger with UI.");
        return app;
    }

    public class SnakeCaseSchemaFilter : ISchemaFilter
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

    public class SnakeCaseParameterFilter : IParameterFilter
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