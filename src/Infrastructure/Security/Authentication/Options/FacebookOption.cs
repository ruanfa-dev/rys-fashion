using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Security.Authentication.Options;

public sealed class FacebookOption
{
    public const string Section = "Authentication:Facebook";

    [Required(ErrorMessage = "FacebookOption.AppId is required.")]
    public string AppId { get; init; } = string.Empty;

    [Required(ErrorMessage = "FacebookOption.AppSecret is required.")]
    public string AppSecret { get; init; } = string.Empty;

    [Required(ErrorMessage = "FacebookOption.CallbackPath is required.")]
    [RegularExpression(@"^\/.*", ErrorMessage = "CallbackPath must start with a '/'.")]
    public string CallbackPath { get; init; } = "/auth/facebook/callback";
}
