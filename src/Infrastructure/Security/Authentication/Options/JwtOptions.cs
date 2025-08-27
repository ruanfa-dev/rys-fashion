using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Security.Authentication.Options;

public sealed class JwtOptions
{
    public const string Section = "Authentication:Jwt";

    [Required(ErrorMessage = "JwtOptions.Issuer is required.")]
    public string Issuer { get; init; } = string.Empty;

    [Required(ErrorMessage = "JwtOptions.Audience is required.")]
    public string Audience { get; init; } = string.Empty;

    [Required(ErrorMessage = "JwtOptions.Secret is required.")]
    public string Secret { get; init; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "JwtOptions.ExpiryInMinutes must be greater than zero.")]
    public int ExpiryInMinutes { get; init; } = 60;

    [Range(1, int.MaxValue, ErrorMessage = "JwtOptions.RefreshTokenExpiryInDays must be greater than zero.")]
    public int RefreshTokenExpiryInDays { get; init; } = 7;

    [Range(1, int.MaxValue, ErrorMessage = "JwtOptions.RefreshTokenExpiryRememberMeInDays must be greater than zero.")]
    public int RefreshTokenExpiryRememberMeInDays { get; init; } = 30;

    [Range(1, int.MaxValue, ErrorMessage = "JwtOptions.MaxTokensSystemUser must be greater than zero.")]
    public int MaxTokensSystemUser { get; init; } = 1;

    [Range(1, int.MaxValue, ErrorMessage = "JwtOptions.MaxTokensCustomer must be greater than zero.")]
    public int MaxTokensCustomer { get; init; } = 5;
}
