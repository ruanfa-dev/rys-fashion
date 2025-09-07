using ErrorOr;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using UseCases.Common.Security.Authentication.Externals;

namespace Infrastructure.Security.Authentication.Externals.Validators;

public sealed class CompositeExternalTokenValidator : IExternalTokenValidator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CompositeExternalTokenValidator> _logger;

    public CompositeExternalTokenValidator(
        IServiceProvider serviceProvider,
        ILogger<CompositeExternalTokenValidator> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

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
            "google" => _serviceProvider.GetService<GoogleTokenValidator>() as IExternalTokenValidator,
            "facebook" => _serviceProvider.GetService<FacebookTokenValidator>() as IExternalTokenValidator,
            _ => null
        };

        if (validator == null)
        {
            _logger.LogWarning("No validator found for provider: {Provider}", provider);
            return Error.NotFound("Provider.ValidatorNotFound", $"No validator configured for provider '{provider}'. Supported providers: google, facebook");
        }

        try
        {
            _logger.LogDebug("Validating token for provider: {Provider}", provider);
            var result = await validator.ValidateTokenAsync(provider, accessToken, idToken, authorizationCode, redirectUri, cancellationToken);
            
            if (result.IsError)
            {
                _logger.LogWarning("Token validation failed for provider {Provider}: {Errors}", 
                    provider, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            else
            {
                _logger.LogDebug("Token validation successful for provider {Provider}, user: {Email}", 
                    provider, result.Value.Email);
            }
            
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Token validation cancelled for provider {Provider}", provider);
            return Error.Failure("Token.ValidationCancelled", "Token validation was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "External token validation failed for provider {Provider}", provider);
            return Error.Failure("Token.ValidationError", $"Token validation failed for provider '{provider}': {ex.Message}");
        }
    }
}