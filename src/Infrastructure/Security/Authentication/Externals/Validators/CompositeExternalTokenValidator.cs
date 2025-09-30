using ErrorOr;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using UseCases.Common.Security.Authentication.Externals;

namespace Infrastructure.Security.Authentication.Externals.Validators;

public sealed class CompositeExternalTokenValidator(
    IServiceProvider serviceProvider,
    ILogger<CompositeExternalTokenValidator> logger)
    : IExternalTokenValidator
{
    public async Task<ErrorOr<ExternalUserInfo>> ValidateTokenAsync(
        string provider,
        string? accessToken,
        string? idToken,
        string? authorizationCode,
        string? redirectUri,
        CancellationToken cancellationToken = default)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(provider))
        {
            return Error.Validation("Provider.Required", "Provider is required");
        }

        var normalizedProvider = provider.ToLowerInvariant();
        
        var validator = normalizedProvider switch
        {
            "google" => serviceProvider.GetService<GoogleTokenValidator>() as IExternalTokenValidator,
            "facebook" => serviceProvider.GetService<FacebookTokenValidator>() as IExternalTokenValidator,
            _ => null
        };

        if (validator == null)
        {
            logger.LogWarning("No validator found for provider: {Provider}", provider);
            return Error.NotFound("Provider.ValidatorNotFound", $"No validator configured for provider '{provider}'. Supported providers: google, facebook");
        }

        try
        {
            logger.LogDebug("Validating token for provider: {Provider}", provider);
            var result = await validator.ValidateTokenAsync(provider, accessToken, idToken, authorizationCode, redirectUri, cancellationToken);
            
            if (result.IsError)
            {
                logger.LogWarning("Token validation failed for provider {Provider}: {Errors}", 
                    provider, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            else
            {
                logger.LogDebug("Token validation successful for provider {Provider}, user: {Email}", 
                    provider, result.Value.Email);
            }
            
            return result;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Token validation cancelled for provider {Provider}", provider);
            return Error.Failure("Token.ValidationCancelled", "Token validation was cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "External token validation failed for provider {Provider}", provider);
            return Error.Failure("Token.ValidationError", $"Token validation failed for provider '{provider}': {ex.Message}");
        }
    }
}