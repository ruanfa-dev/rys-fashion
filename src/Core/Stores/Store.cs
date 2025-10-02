using SharedKernel.Domain.Primitives;

namespace Core.Stores;
public class Store : AuditableEntity
{
    // Comma separated or list-like support could be used by persistence mapping in Infrastructure
    public string[]? SupportedLocalesList { get; set; } = [];

    public void SetSupportedLocales(params string[] locales)
    {
        SupportedLocalesList = locales.Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
    }
}
