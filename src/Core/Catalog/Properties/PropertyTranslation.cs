using SharedKernel.Domain.Attributes.TranslatableResource;

namespace Core.Catalog.Properties;

public sealed class PropertyTranslation : ITranslation
{
    #region Properties
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string Culture { get; set; } = null!;
    public bool IsDefault { get; set; }

    // Generic fields dictionary for translations
    public IDictionary<string, string?>? Fields { get; set; } = new Dictionary<string, string?>();

    // Relationship navigation
    public Property? Property { get; set; }
    #endregion
}
