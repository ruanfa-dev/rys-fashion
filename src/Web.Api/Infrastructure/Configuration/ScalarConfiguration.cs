using Scalar.AspNetCore;

namespace Web.Api.Infrastructure.Configuration;

public static class ScalarConfiguration
{
    internal static IApplicationBuilder UseScalarWithUi(this WebApplication app)
    {

        app.MapScalarApiReference(options =>
        {
            options.WithOpenApiRoutePattern("/openapi/v1.json");
            options.Theme = ScalarTheme.Laserwave;
            options.AddPreferredSecuritySchemes("Bearer");
        });
        return app;
    }
}
