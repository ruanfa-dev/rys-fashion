namespace UseCases.Common.Systems.Options;
public sealed class StorefrontOption
{
    public const string Section = "Storefront";

    public string SystemName { get; set; } = "Ruanfa.Storefront";
    public string BaseUrl { get; set; } = string.Empty;
    public string SupportEmail { get; set; } = string.Empty;
    public string DefaultPage { get; set; } = "/home";
}
