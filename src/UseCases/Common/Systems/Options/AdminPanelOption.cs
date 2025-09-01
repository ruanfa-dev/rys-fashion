namespace UseCases.Common.Systems.Options;
public sealed class AdminPanelOption
{
    public const string Section = "AdminPanel";
    public string SystemName { get; set; } = "Ruanfa.AdminPanel";
    public string BaseUrl { get; set; } = string.Empty;
    public string DefaultPage { get; set; } = "/dashboard";
    public string SupportEmail { get; set; } = string.Empty;
}
