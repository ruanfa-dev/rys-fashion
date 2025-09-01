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
        var validator = provider.ToLowerInvariant() switch
        {
            "google" => _serviceProvider.GetService<GoogleTokenValidator>() as IExternalTokenValidator,
            "facebook" => _serviceProvider.GetService<FacebookTokenValidator>() as IExternalTokenValidator,
            _ => null
        };

        if (validator == null)
        {
            _logger.LogWarning("No validator found for provider: {Provider}", provider);
            return Error.NotFound("Provider.ValidatorNotFound", $"No validator configured for provider '{provider}'");
        }

        return await validator.ValidateTokenAsync(provider, accessToken, idToken, authorizationCode, redirectUri, cancellationToken);
    }
}