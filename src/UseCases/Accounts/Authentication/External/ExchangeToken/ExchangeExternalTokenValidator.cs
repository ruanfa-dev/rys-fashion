using FluentValidation;

namespace UseCases.Accounts.Authentication.External.ExchangeToken;

/// <summary>
/// Validator for external token exchange command
/// </summary>
public sealed class ExchangeExternalTokenValidator : AbstractValidator<ExchangeExternalTokenCommand>
{
    private static readonly HashSet<string> ValidProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "google",
        "facebook"
    };

    public ExchangeExternalTokenValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty()
            .WithMessage("OAuth provider is required")
            .Must(BeValidProvider)
            .WithMessage("Provider must be either 'google' or 'facebook'");

        RuleFor(x => x.AccessToken)
            .NotEmpty()
            .WithMessage("Access token is required")
            .MinimumLength(10)
            .WithMessage("Access token appears to be invalid");

        // If linking to existing user, validate user ID
        RuleFor(x => x.ExistingUserId)
            .NotEmpty()
            .When(x => x.LinkToExistingUser)
            .WithMessage("User ID is required when linking to existing user");

        RuleFor(x => x.ExistingUserId)
            .Must(BeValidGuid)
            .When(x => x.LinkToExistingUser && !string.IsNullOrEmpty(x.ExistingUserId))
            .WithMessage("User ID must be a valid GUID");
    }

    private static bool BeValidProvider(string provider)
    {
        return ValidProviders.Contains(provider);
    }

    private static bool BeValidGuid(string? value)
    {
        return Guid.TryParse(value, out _);
    }
}