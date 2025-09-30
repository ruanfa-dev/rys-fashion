using System.Collections.Generic;

namespace SharedKernel.Domain.Attributes.TranslatableResource;

/// <summary>
/// Provides helpers to access translations from translatable resources.
/// This generic provider uses the strongly-typed ITranslatable<TTranslation> contract.
/// </summary>
public interface ITranslationProvider<TResource, TTranslation>
    where TTranslation : class, ITranslation
    where TResource : ITranslatable<TTranslation>
{
    IEnumerable<TTranslation>? GetTranslations(TResource resource);

    string? GetField(TResource resource, string fieldName, string culture, bool fallback = true);
}

/// <summary>
/// Default implementation of <see cref="ITranslationProvider{TResource,TTranslation}"/>.
/// </summary>
public sealed class TranslationProvider<TResource, TTranslation> : ITranslationProvider<TResource, TTranslation>
    where TTranslation : class, ITranslation
    where TResource : ITranslatable<TTranslation>
{
    public IEnumerable<TTranslation>? GetTranslations(TResource resource)
    {
        return resource?.Translations;
    }

    public string? GetField(TResource resource, string fieldName, string culture, bool fallback = true)
    {
        if (resource == null) return null;
        return resource.GetField(fieldName, culture, fallback);
    }
}
