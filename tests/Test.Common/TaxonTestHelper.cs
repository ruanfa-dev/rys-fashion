using Core.Catalog.Taxonomies;
using System.Collections.Concurrent;

namespace Test.Common;

public static class TaxonTestHelper
{
    static readonly ConcurrentDictionary<Guid, Taxon> _registry = new();

    public static void Register(Taxon taxon)
    {
        if (taxon == null) return;
        _registry[taxon.Id] = taxon;
    }

    public static void Unregister(Guid id)
    {
        _registry.TryRemove(id, out _);
    }

    public static void Clear() => _registry.Clear();

    public static void WireResolver()
    {
        Taxon.InstanceResolver = id => _registry.TryGetValue(id, out var t) ? t : null;
    }
}
