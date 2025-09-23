using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Catalogs;

/// <summary>
/// Domain model inspired by Spree::TaxCategory (simplified).
/// Holds collection of TaxRates and relations to products/variants.
/// Cache/lookup helpers and side-effects belong to infrastructure layer.
/// </summary>
public sealed class TaxCategory : AuditableEntity
{
    public string Name { get; set; } = default!;
    public bool IsDefault { get; set; }

    public ICollection<TaxRate> TaxRates { get; set; } = new List<TaxRate>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Variant> Variants { get; set; } = new List<Variant>();

    private TaxCategory() { }

    public static TaxCategory Create(string name, bool isDefault = false)
    {
        return new TaxCategory
        {
            Name = name,
            IsDefault = isDefault
        };
    }

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
        MarkAsUpdated();
    }

    public IEnumerable<TaxRate> RatesForZone(Zone? zone)
    {
        if (zone == null) return Enumerable.Empty<TaxRate>();
        return TaxRates.Where(r => r.ZoneId == null || r.ZoneId == zone.Id);
    }

    #region Validation / Errors

    public static List<Error> ValidateModel(string? name)
    {
        var errors = new List<Error>();
        if (string.IsNullOrWhiteSpace(name)) errors.Add(Errors.NameRequired);
        return errors;
    }

    public static class Errors
    {
        public static Error NameRequired => Error.Validation("TaxCategory.NameRequired", "Tax category name is required.");
        public static Error NotFound(Guid id) => Error.NotFound("TaxCategory.NotFound", $"TaxCategory with ID '{id}' was not found.");
    }

    #endregion
}
