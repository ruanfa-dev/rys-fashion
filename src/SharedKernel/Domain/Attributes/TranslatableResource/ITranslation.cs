namespace SharedKernel.Domain.Attributes.TranslatableResource;

/// <summary>
/// Minimal contract for a translation entity used by translatable resources.
/// Use `Culture` (eg. "en", "en-US") and `IsDefault` to mark default translation.
/// Implementations may expose a `Fields` dictionary to store translated values by key
/// (e.g. "Presentation", "Name", "Slug") instead of many explicit properties.
/// </summary>
public interface ITranslation
{
    string Culture { get; set; }
    bool IsDefault { get; set; }

    /// <summary>
    /// Dictionary of translated fields by name. Implementations should populate this
    /// when using a generic key/value translation storage strategy.
    /// </summary>
    IDictionary<string, string?>? Fields { get; set; }
}


public abstract class BaseTranslation : ITranslation
{
    public string Culture { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public IDictionary<string, string?>? Fields { get; set; }
}