using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.Accounts.Authentication.External.ExchangeToken;
using UseCases.Common.Extensions;
using UseCases.Common.Security.Authentication.Models;

namespace Web.Api.Controllers;

/// <summary>
/// External OAuth authentication controller for handling Google/Facebook login
/// </summary>
[ApiController]
[Route("api/auth/external")]
[Tags("External Authentication")]
public sealed class ExternalAuthController : ControllerBase
{
    private readonly ISender _sender;

    public ExternalAuthController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Exchange external OAuth token (Google/Facebook) for application token
    /// </summary>
    /// <param name="request">External authentication request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Application tokens and user information</returns>
    [HttpPost("exchange")]
    [ProducesResponseType<ExternalAuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ExchangeToken(
        [FromBody] ExternalAuthRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new ExchangeExternalTokenCommand
        {
            Provider = request.Provider,
            AccessToken = request.AccessToken,
            IdToken = request.IdToken,
            LinkToExistingUser = request.LinkToExistingUser,
            ExistingUserId = request.ExistingUserId
        };

        var result = await _sender.Send(command, cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Get Keycloak authorization URL for external provider
    /// </summary>
    /// <param name="provider">OAuth provider (google or facebook)</param>
    /// <param name="redirectUri">Redirect URI after authentication</param>
    /// <param name="state">Optional state parameter</param>
    /// <returns>Authorization URL for the specified provider</returns>
    [HttpGet("authorize-url/{provider}")]
    [ProducesResponseType<AuthorizeUrlResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public IActionResult GetAuthorizeUrl(
        [FromRoute] string provider,
        [FromQuery] string redirectUri,
        [FromQuery] string? state = null)
    {
        if (!IsValidProvider(provider))
        {
            return BadRequest($"Unsupported provider: {provider}");
        }

        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            return BadRequest("Redirect URI is required");
        }

        // Get configuration from appsettings.json or environment
        var keycloakBaseUrl = HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()
            .GetValue<string>("Keycloak:Authority");

        if (string.IsNullOrEmpty(keycloakBaseUrl))
        {
            return BadRequest("Keycloak configuration is missing");
        }

        var realm = HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()
            .GetValue<string>("Keycloak:Realm") ?? "rys-fashion";

        // Build the Keycloak external provider authorization URL
        var authUrl = $"{keycloakBaseUrl}/realms/{realm}/broker/{provider.ToLowerInvariant()}/login";
        
        var queryParams = new List<string>
        {
            $"redirect_uri={Uri.EscapeDataString(redirectUri)}"
        };

        if (!string.IsNullOrEmpty(state))
        {
            queryParams.Add($"state={Uri.EscapeDataString(state)}");
        }

        var fullAuthUrl = $"{authUrl}?{string.Join("&", queryParams)}";

        var response = new AuthorizeUrlResponse
        {
            AuthorizeUrl = fullAuthUrl,
            Provider = provider.ToLowerInvariant(),
            RedirectUri = redirectUri,
            State = state
        };

        return Ok(response);
    }

    /// <summary>
    /// Handle callback from Keycloak after external authentication
    /// This endpoint processes the authorization code returned from Keycloak
    /// </summary>
    /// <param name="code">Authorization code from Keycloak</param>
    /// <param name="state">State parameter for security</param>
    /// <param name="error">Error parameter if authentication failed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Redirect to frontend with tokens or error</returns>
    [HttpGet("callback")]
    public async Task<IActionResult> HandleCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken = default)
    {
        // This would typically redirect to your frontend with the authorization code
        // Your frontend would then exchange the code for tokens
        
        if (!string.IsNullOrEmpty(error))
        {
            return BadRequest($"Authentication failed: {error}");
        }

        if (string.IsNullOrEmpty(code))
        {
            return BadRequest("Authorization code is missing");
        }

        // In a real implementation, you might:
        // 1. Exchange the code for tokens using Keycloak's token endpoint
        // 2. Redirect to frontend with tokens in URL fragment or via secure cookie
        // 3. Handle any errors appropriately

        var frontendUrl = HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()
            .GetValue<string>("Frontend:CallbackUrl") ?? "http://localhost:3000/auth/callback";

        var callbackUrl = $"{frontendUrl}?code={code}";
        
        if (!string.IsNullOrEmpty(state))
        {
            callbackUrl += $"&state={state}";
        }

        return Redirect(callbackUrl);
    }

    private static bool IsValidProvider(string provider)
    {
        var validProviders = new[] { "google", "facebook" };
        return validProviders.Contains(provider.ToLowerInvariant());
    }
}

/// <summary>
/// Response model for authorize URL endpoint
/// </summary>
public sealed record AuthorizeUrlResponse
{
    public string AuthorizeUrl { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string RedirectUri { get; init; } = string.Empty;
    public string? State { get; init; }
}