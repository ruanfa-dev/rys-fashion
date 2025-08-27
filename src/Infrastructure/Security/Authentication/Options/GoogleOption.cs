using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Security.Authentication.Options;

public sealed class GoogleOption
{
    public const string Section = "Authentication:Google";

    [Required(ErrorMessage = "GoogleOption.ClientId is required.")]
    public string ClientId { get; init; } = string.Empty;

    [Required(ErrorMessage = "GoogleOption.ClientSecret is required.")]
    public string ClientSecret { get; init; } = string.Empty;

    [Required(ErrorMessage = "GoogleOption.CallbackPath is required.")]
    [RegularExpression(@"^\/.*", ErrorMessage = "CallbackPath must start with a '/'.")]
    public string CallbackPath { get; init; } = "/auth/google/callback";
}
