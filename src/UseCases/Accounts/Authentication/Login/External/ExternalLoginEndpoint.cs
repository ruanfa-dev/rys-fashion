using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using SharedKernel.Models;

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
            var apiResponse = result.ToApiResponse("External providers retrieved successfully");

            // Add external auth related links and metadata
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("google-config", $"{Route}/config/google")
                    .WithLink("facebook-config", $"{Route}/config/facebook")
                    .WithLink("health-check", $"{Route}/health")
                    .WithMetadata("authenticationMethod", "external")
                    .WithMetadata("providersCount", apiResponse.Data.Count)
                    .WithMetadata("retrievedAt", DateTime.UtcNow);
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetExternalProviders.Name)
        .WithSummary(GetExternalProviders.Summary)
        .WithDescription(GetExternalProviders.Description)
        .Produces<ApiResponse<List<GetExternalProviders.Result>>>(StatusCodes.Status200OK)
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
                var errorResponse = new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = "Provider parameter is required",
                    Status = 400,
                    Timestamp = DateTime.UtcNow
                };
                return Results.Ok(errorResponse);
            }

            // Normalize provider name for security
            var normalizedProvider = provider.ToLowerInvariant().Trim();

            // Security: Only allow known providers
            if (!IsValidProvider(normalizedProvider))
            {
                var errorResponse = new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"Provider '{provider}' is not supported",
                    Status = 400,
                    Timestamp = DateTime.UtcNow
                };
                return Results.Ok(errorResponse);
            }

            var query = new GetOAuthConfig.Query(normalizedProvider);
            var result = await mediator.Send(query, cancellationToken);
            var apiResponse = result.ToApiResponse($"OAuth configuration for {provider} retrieved successfully");

            // Add OAuth-specific metadata and links
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("providers", $"{Route}/providers")
                    .WithLink("token-exchange", $"{Route}/token/exchange/{normalizedProvider}")
                    .WithLink("token-verify", $"{Route}/token/verify/{normalizedProvider}")
                    .WithMetadata("provider", normalizedProvider)
                    .WithMetadata("configType", "oauth")
                    .WithMetadata("retrievedAt", DateTime.UtcNow);
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(GetOAuthConfig.Name)
        .WithSummary(GetOAuthConfig.Summary)
        .WithDescription(GetOAuthConfig.Description)
        .Produces<ApiResponse<GetOAuthConfig.Result>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .AllowAnonymous(); // Allow anonymous access to get OAuth config

        // Exchange external provider token for application tokens
        group.MapPost("/token/exchange/{provider}", async (
            [FromRoute] string provider,
            [FromBody] ExchangeExternalToken.Param param,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken) =>
        {
            // Validate provider parameter
            if (string.IsNullOrWhiteSpace(provider))
            {
                var errorResponse = new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = "Provider parameter is required",
                    Status = 400,
                    Timestamp = DateTime.UtcNow
                };
                return Results.Ok(errorResponse);
            }

            // Normalize provider name for security
            var normalizedProvider = provider.ToLowerInvariant().Trim();

            // Security: Only allow known providers
            if (!IsValidProvider(normalizedProvider))
            {
                var errorResponse = new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"Provider '{provider}' is not supported",
                    Status = 400,
                    Timestamp = DateTime.UtcNow
                };
                return Results.Ok(errorResponse);
            }

            // Create command with validated provider
            param = param with { Provider = normalizedProvider };
            var result = await mediator.Send(new ExchangeExternalToken.Command(param), cancellationToken);
            var apiResponse = result.ToApiResponse($"External token exchanged successfully for {provider}");

            // Add authentication-related metadata and links
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("profile", "/api/account/profile")
                    .WithLink("session", "/api/account/auth/session")
                    .WithLink("logout", "/api/account/auth/logout")
                    .WithMetadata("provider", normalizedProvider)
                    .WithMetadata("authenticationMethod", "external-token-exchange")
                    .WithMetadata("exchangedAt", DateTime.UtcNow);
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(ExchangeExternalToken.Name)
        .WithSummary(ExchangeExternalToken.Summary)
        .WithDescription(ExchangeExternalToken.Description)
        .Produces<ApiResponse<ExchangeExternalToken.Result>>(StatusCodes.Status200OK)
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
                var errorResponse = new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = "Provider parameter is required",
                    Status = 400,
                    Timestamp = DateTime.UtcNow
                };
                return Results.Ok(errorResponse);
            }

            // Normalize provider name for security
            var normalizedProvider = provider.ToLowerInvariant().Trim();

            // Security: Only allow known providers
            if (!IsValidProvider(normalizedProvider))
            {
                var errorResponse = new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"Provider '{provider}' is not supported",
                    Status = 400,
                    Timestamp = DateTime.UtcNow
                };
                return Results.Ok(errorResponse);
            }

            // Create command with validated provider
            var commandWithProvider = command with { Provider = normalizedProvider };
            var result = await mediator.Send(commandWithProvider, cancellationToken);
            var apiResponse = result.ToApiResponse($"External token verified successfully for {provider}");

            // Add verification metadata and links
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                apiResponse
                    .WithLink("token-exchange", $"{Route}/token/exchange/{normalizedProvider}")
                    .WithLink("providers", $"{Route}/providers")
                    .WithMetadata("provider", normalizedProvider)
                    .WithMetadata("operation", "token-verification")
                    .WithMetadata("verifiedAt", DateTime.UtcNow);
            }

            return TypedResults.Ok(apiResponse);
        })
        .WithName(VerifyExternalToken.Name)
        .WithSummary(VerifyExternalToken.Summary)
        .WithDescription(VerifyExternalToken.Description)
        .Produces<ApiResponse<VerifyExternalToken.Result>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .AllowAnonymous();

        // Health check endpoint for external authentication status
        group.MapGet("/health", () =>
        {
            var healthResponse = new ApiResponse<object>
            {
                IsSuccess = true,
                Data = new
                {
                    status = "healthy",
                    timestamp = DateTimeOffset.UtcNow,
                    supportedProviders = GetSupportedProviders()
                },
                Message = "External authentication service is healthy",
                Status = 200,
                Timestamp = DateTime.UtcNow
            };

            healthResponse
                .WithLink("providers", $"{Route}/providers")
                .WithMetadata("serviceType", "external-authentication")
                .WithMetadata("healthCheck", "passed");

            return Results.Ok(healthResponse);
        })
        .WithName("ExternalAuthHealth")
        .WithSummary("External authentication health check")
        .WithDescription("Check the health status of external authentication services")
        .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
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