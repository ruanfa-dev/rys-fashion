using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using UseCases.Accounts.Authentication.Login.External.Exchange;
using UseCases.Accounts.Authentication.Login.External.GetConfig;
using UseCases.Accounts.Authentication.Login.External.Providers;
using UseCases.Accounts.Authentication.Login.External.Verify;
using UseCases.Common.Extensions;

namespace UseCases.Accounts.Authentication.Login.External;

public sealed class ExternalLoginEndpoint : ICarterModule
{
    internal const string Route = $"{AuthenticationEndpoint.Route}/external";
    internal const string Tag = "External Authentication";
    internal const string Description = "Secure token exchange endpoints for external authentication (OAuth handled by frontend)";
    internal const string Summary = "External Authentication Token Exchange API";
    internal const string Name = "ExternalAuthentication";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route)
            .WithName(Name)
            .WithTags(AccountEndpoint.Tag, AuthenticationEndpoint.Tag, Tag)
            .WithSummary(Summary)
            .WithDescription(Description)
            .DisableAntiforgery(); // Disable antiforgery for external API calls

        // Get available external providers with frontend configuration
        group.MapGet("/providers", async ([FromServices] ISender mediator) =>
        {
            var query = new GetExternalProviders.Query();
            var result = await mediator.Send(query);

            return result.ToTypedResult();
        })
        .WithName(GetExternalProviders.Name)
        .WithSummary(GetExternalProviders.Summary)
        .WithDescription(GetExternalProviders.Description)
        .Produces<List<GetExternalProviders.Result>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .AllowAnonymous(); // Allow anonymous access to discover providers

        // Get OAuth configuration for frontend clients
        group.MapGet("/config/{provider}", async (
            [FromRoute] string provider,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            // Validate provider parameter
            if (string.IsNullOrWhiteSpace(provider))
            {
                return Results.BadRequest(new { error = "Provider parameter is required" });
            }

            // Normalize provider name for security
            var normalizedProvider = provider.ToLowerInvariant().Trim();
            
            // Security: Only allow known providers
            if (!IsValidProvider(normalizedProvider))
            {
                return Results.BadRequest(new { error = $"Provider '{provider}' is not supported" });
            }

            var query = new GetOAuthConfig.Query(normalizedProvider);
            var result = await mediator.Send(query, cancellationToken);

            return result.ToTypedResult();
        })
        .WithName(GetOAuthConfig.Name)
        .WithSummary(GetOAuthConfig.Summary)
        .WithDescription(GetOAuthConfig.Description)
        .Produces<GetOAuthConfig.Result>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .AllowAnonymous(); // Allow anonymous access to get OAuth config

        // Exchange external provider token for application tokens
        group.MapPost("/token/exchange/{provider}", async (
            [FromRoute] string provider,
            [FromBody] ExchangeExternalToken.Command command,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            // Validate provider parameter
            if (string.IsNullOrWhiteSpace(provider))
            {
                return Results.BadRequest(new { error = "Provider parameter is required" });
            }

            // Normalize provider name for security
            var normalizedProvider = provider.ToLowerInvariant().Trim();

            // Security: Only allow known providers
            if (!IsValidProvider(normalizedProvider))
            {
                return Results.BadRequest(new { error = $"Provider '{provider}' is not supported" });
            }

            // Create command with validated provider
            var commandWithProvider = command with { Provider = normalizedProvider };
            var result = await mediator.Send(commandWithProvider, cancellationToken);

            return result.ToTypedResult();
        })
        .WithName(ExchangeExternalToken.Name)
        .WithSummary(ExchangeExternalToken.Summary)
        .WithDescription(ExchangeExternalToken.Description)
        .Produces<ExchangeExternalToken.Result>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .AllowAnonymous();

        // Verify external token without creating session (useful for frontend validation)
        group.MapPost("/token/verify/{provider}", async (
            [FromRoute] string provider,
            [FromBody] VerifyExternalToken.Command command,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            // Validate provider parameter
            if (string.IsNullOrWhiteSpace(provider))
            {
                return Results.BadRequest(new { error = "Provider parameter is required" });
            }

            // Normalize provider name for security
            var normalizedProvider = provider.ToLowerInvariant().Trim();

            // Security: Only allow known providers
            if (!IsValidProvider(normalizedProvider))
            {
                return Results.BadRequest(new { error = $"Provider '{provider}' is not supported" });
            }

            // Create command with validated provider
            var commandWithProvider = command with { Provider = normalizedProvider };
            var result = await mediator.Send(commandWithProvider, cancellationToken);

            return result.ToTypedResult();
        })
        .WithName(VerifyExternalToken.Name)
        .WithSummary(VerifyExternalToken.Summary)
        .WithDescription(VerifyExternalToken.Description)
        .Produces<VerifyExternalToken.Result>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .AllowAnonymous();

        // Health check endpoint for external authentication status
        group.MapGet("/health", () =>
        {
            return Results.Ok(new 
            { 
                status = "healthy",
                timestamp = DateTimeOffset.UtcNow,
                supportedProviders = GetSupportedProviders()
            });
        })
        .WithName("ExternalAuthHealth")
        .WithSummary("External authentication health check")
        .WithDescription("Check the health status of external authentication services")
        .Produces<object>(StatusCodes.Status200OK)
        .AllowAnonymous()
        .ExcludeFromDescription(); // Hide from Swagger for internal use
    }

    /// <summary>
    /// Validates if the provider is supported and secure for production use
    /// </summary>
    /// <param name="provider">The provider name to validate</param>
    /// <returns>True if the provider is valid and supported</returns>
    private static bool IsValidProvider(string provider)
    {
        // Only allow these specific providers (removed Microsoft)
        var supportedProviders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "google",
            "facebook"
        };

        return supportedProviders.Contains(provider);
    }

    /// <summary>
    /// Gets the list of supported providers for health check endpoint
    /// </summary>
    /// <returns>Array of supported provider names</returns>
    private static string[] GetSupportedProviders()
    {
        return new[] { "google", "facebook" };
    }
}