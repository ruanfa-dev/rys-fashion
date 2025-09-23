using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Join entity between Product and Taxon (maps to spree_products_taxons in Spree).
/// DB-level constraints (unique composite index on product_id + taxon_id, table name) should be configured in EF configuration.
/// </summary>
public sealed class Classification : AuditableEntity
{
    // Join keys
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;

    public Guid TaxonId { get; set; }
    public Taxon Taxon { get; set; } = default!;

    // acts_as_list -> position (nullable to match Rails allow_blank/allow_nil)
    public int? Position { get; set; }

    private Classification() { }

    public static Classification Create(Guid productId, Guid taxonId, int? position = null)
    {
        return new Classification
        {
            ProductId = productId,
            TaxonId = taxonId,
            Position = position
        };
    }

    public void UpdatePosition(int? position)
    {
        Position = position;
    }

    /// <summary>
    /// Lightweight validation used by handlers before persistence. Uniqueness must be enforced/checked at persistence layer.
    /// </summary>
    public static List<Error> ValidateModel(Guid productId, Guid taxonId, int? position)
    {
        var errors = new List<Error>();

        if (productId == Guid.Empty)
            errors.Add(Errors.ProductRequired);

        if (taxonId == Guid.Empty)
            errors.Add(Errors.TaxonRequired);

        if (position.HasValue && position < 0)
            errors.Add(Errors.InvalidPosition);

        return errors;
    }

    public static class Errors
    {
        public static Error ProductRequired =>
            Error.Validation("Classification.ProductRequired", "Product is required.");

        public static Error TaxonRequired =>
            Error.Validation("Classification.TaxonRequired", "Taxon is required.");

        public static Error InvalidPosition =>
            Error.Validation("Classification.InvalidPosition", "Position must be a non-negative integer.");

        public static Error AlreadyLinked(Guid productId, Guid taxonId) =>
            Error.Conflict("Classification.AlreadyLinked", $"Product '{productId}' is already linked to Taxon '{taxonId}'.");
    }

    public static class Events
    {
        public record Created(Guid ClassificationId) : DomainEvent;
        public record Updated(Guid ClassificationId) : DomainEvent;
        public record Deleted(Guid ClassificationId) : DomainEvent;
    }
}
