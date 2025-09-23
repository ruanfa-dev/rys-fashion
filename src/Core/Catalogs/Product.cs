using ErrorOr;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Catalogs;

/// <summary>
/// Domain model inspired by Spree::Product (simplified).
/// Persistence-specific behaviour (scopes, callbacks, translations, file attachments, background jobs)
/// belongs to infrastructure/application layers.
/// </summary>
public partial class Product : AuditableEntity
{
    #region Core properties

    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Slug { get; set; }
    public string Status { get; set; } = "draft"; // draft | active | archived
    public DateTimeOffset? AvailableOn { get; set; }
    public DateTimeOffset? MakeActiveAt { get; set; }
    public DateTimeOffset? DiscontinueOn { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public Guid ShippingCategoryId { get; set; }
    public Guid TaxCategoryId { get; set; }

    #endregion

    #region Relationships / navigation

    // Non-master variants
    public ICollection<Variant> Variants { get; set; } = new List<Variant>();

    // All variants (including master). Infrastructure should keep ordering / soft-delete semantics.
    public ICollection<Variant> VariantsIncludingMaster { get; set; } = new List<Variant>();

    // Master: variant with IsMaster == true (may be null until ensured)
    public Variant? Master => VariantsIncludingMaster.FirstOrDefault(v => v.IsMaster);

    // ProductOptionTypes / OptionTypes
    public ICollection<ProductOptionType> ProductOptionTypes { get; set; } = new List<ProductOptionType>();
    public IEnumerable<OptionType> OptionTypes => ProductOptionTypes.Select(pot => pot.OptionType);

    // ProductProperties / Properties
    public ICollection<ProductProperty> ProductProperties { get; set; } = new List<ProductProperty>();
    public IEnumerable<Property> Properties => ProductProperties.Select(pp => pp.Property);

    // Classifications / Taxons
    public ICollection<Classification> Classifications { get; set; } = new List<Classification>();
    public IEnumerable<Taxon> Taxons => Classifications.Select(c => c.Taxon);

    // Convenience: prices collected from variants (including master)
    public IEnumerable<Price> Prices => VariantsIncludingMaster.SelectMany(v => v.Prices);

    // Variant images convenience (infrastructure to populate ProductImages)
    public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    #endregion

    #region Construction / lifecycle helpers

    public Product() { }

    /// <summary>
    /// Ensure there is a master variant for this product.
    /// Application layer should call/persist as part of creation flow.
    /// </summary>
    public void EnsureMaster()
    {
        if (Master != null) return;

        var master = Variant.Create(this.Id, sku: null, isMaster: true);
        master.ProductId = this.Id;
        VariantsIncludingMaster.Add(master);
    }

    #endregion

    #region Variant selection / delegation helpers

    public bool HasVariants() => Variants.Any();

    /// <summary>
    /// Default variant selection:
    /// - prefer a purchasable variant (in stock or backorderable)
    /// - otherwise prefer first non-master variant when available
    /// - otherwise return master (ensure master exists)
    /// </summary>
    public Variant DefaultVariant()
    {
        // prefer purchasable non-master variant
        var purchasable = Variants.FirstOrDefault(v => v.Purchasable);
        if (purchasable != null) return purchasable;

        if (HasVariants())
            return Variants.First();

        if (Master == null)
            EnsureMaster();

        return Master!;
    }

    public Guid DefaultVariantId() => DefaultVariant().Id;

    #endregion

    #region Stock / availability / pricing

    /// <summary>
    /// Aggregate on-hand quantity.
    /// If any variant doesn't track inventory, treat as effectively infinite (mirrors Spree behaviour).
    /// </summary>
    public long TotalOnHand()
    {
        if (VariantsIncludingMaster.Any(v => !v.TrackInventory))
            return long.MaxValue;

        return VariantsIncludingMaster.Sum(v => v.StockItems?.Sum(si => (long)si.CountOnHand) ?? 0L);
    }

    public bool Purchasable() => DefaultVariant().Purchasable || Variants.Any(v => v.Purchasable);

    public bool InStock() => DefaultVariant().InStock || Variants.Any(v => v.InStock);

    public bool Backorderable() => DefaultVariant().Backorderable || Variants.Any(v => v.Backorderable);

    /// <summary>
    /// Lowest price for product in a currency across variants (including master).
    /// </summary>
    public Price? LowestPrice(string? currency)
    {
        var currencyNormalized = currency?.ToUpperInvariant();
        var pricesForCurrency = Prices
            .Where(p => string.Equals(p.Currency, currencyNormalized, StringComparison.OrdinalIgnoreCase) && p.Amount.HasValue)
            .ToList();

        return pricesForCurrency.OrderBy(p => p.Amount).FirstOrDefault();
    }

    public bool PriceVaries(string? currency)
    {
        var amounts = Prices
            .Where(p => string.Equals(p.Currency, currency?.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase) && p.Amount.HasValue)
            .Select(p => p.Amount!.Value)
            .Distinct()
            .ToList();
        return amounts.Count > 1;
    }

    #endregion

    #region Property helpers

    /// <summary>
    /// Retrieve product property value by property name.
    /// Uses in-memory collection if loaded; otherwise repository/infra should provide efficient lookup.
    /// </summary>
    public string? GetProperty(string propertyName)
    {
        if (ProductProperties != null && ProductProperties.Any())
        {
            var pp = ProductProperties.FirstOrDefault(x => string.Equals(x.Property?.Name, propertyName, StringComparison.OrdinalIgnoreCase));
            return pp?.Value;
        }

        return null;
    }

    /// <summary>
    /// Set or update a product property. Application layer should persist afterwards.
    /// </summary>
    public void SetProperty(Property property, string value)
    {
        var existing = ProductProperties.FirstOrDefault(pp => pp.PropertyId == property.Id);
        if (existing != null)
        {
            existing.Value = value;
        }
        else
        {
            var pp = new ProductProperty
            {
                ProductId = this.Id,
                PropertyId = property.Id,
                Value = value
            };
            ProductProperties.Add(pp);
        }
    }

    #endregion

    #region Prototype helpers

    /// <summary>
    /// Apply associations from a prototype: copy prototype properties and attach option types/taxons.
    /// Application layer should look up prototype and persist changes.
    /// </summary>
    public void AddAssociationsFromPrototype(Prototype prototype)
    {
        if (prototype == null) return;

        // copy properties as placeholders
        foreach (var pp in prototype.PrototypeProperties.Select(x => x.Property).Where(p => p != null))
        {
            if (!ProductProperties.Any(x => x.PropertyId == pp.Id))
            {
                ProductProperties.Add(new ProductProperty
                {
                    ProductId = this.Id,
                    PropertyId = pp.Id,
                    Value = "Placeholder"
                });
            }
        }

        // copy option types
        foreach (var opt in prototype.OptionTypes)
        {
            if (!ProductOptionTypes.Any(x => x.OptionTypeId == opt.Id))
            {
                ProductOptionTypes.Add(ProductOptionType.Create(this.Id, opt.Id));
            }
        }

        // copy taxons (via classifications)
        foreach (var pt in prototype.PrototypeTaxons)
        {
            var taxon = pt.Taxon;
            if (!Classifications.Any(c => c.TaxonId == taxon.Id))
            {
                Classifications.Add(Classification.Create(this.Id, taxon.Id));
            }
        }
    }

    #endregion

    #region Validation / errors (lightweight)

    public static class Errors
    {
        public static Error NameRequired => Error.Validation("Product.NameRequired", "Product name is required.");
        public static Error NotFound(Guid id) => Error.NotFound("Product.NotFound", $"Product '{id}' not found.");
    }

    public static List<Error> ValidateModel(string? name)
    {
        var errors = new List<Error>();
        if (string.IsNullOrWhiteSpace(name))
            errors.Add(Errors.NameRequired);
        return errors;
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid ProductId) : DomainEvent;
        public record Updated(Guid ProductId) : DomainEvent;
        public record Deleted(Guid ProductId) : DomainEvent;
    }

    #endregion
}