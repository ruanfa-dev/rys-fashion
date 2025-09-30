using Core.Catalog.Taxonomies;

using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalog.Products;

// Minimal placeholders to satisfy references from Taxon.cs when full domain types are not yet implemented.
// These are intentionally small and can be replaced with full implementations later.

public sealed class Classification : AuditableEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid TaxonId { get; set; }
    public Taxon? Taxon { get; set; }

    // acts_as_list position
    public int Position { get; set; }

    public static ErrorOr<Classification> Create(Guid productId, Guid taxonId, int position = 0)
    {
        if (productId == Guid.Empty) return Errors.ProductRequired;
        if (taxonId == Guid.Empty) return Errors.TaxonRequired;

        var c = new Classification
        {
            ProductId = productId,
            TaxonId = taxonId,
            Position = Math.Max(0, position)
        };

        c.AddDomainEvent(new Events.Created(c.Id));
        return c;
    }

    public ErrorOr<Classification> UpdatePosition(int? position)
    {
        if (position.HasValue && position.Value != Position)
        {
            if (position.Value < 0) return Errors.InvalidPosition;
            Position = position.Value;
            MarkAsUpdated();
            AddDomainEvent(new Events.Updated(Id));
        }

        return this;
    }

    public ErrorOr<Deleted> Delete()
    {
        AddDomainEvent(new Events.Deleted(Id));
        return Result.Deleted;
    }

    #region Constraints
    public static class Constraints
    {
        public const int MaxPosition = 10000;
    }
    #endregion

    #region Errors
    public static class Errors
    {
        public static Error ProductRequired => Error.Validation("Classification.ProductRequired", "Product is required for a classification.");
        public static Error TaxonRequired => Error.Validation("Classification.TaxonRequired", "Taxon is required for a classification.");
        public static Error InvalidPosition => Error.Validation("Classification.InvalidPosition", "Position must be a non-negative integer.");
        public static Error NotFound(Guid id) => Error.NotFound("Classification.NotFound", $"Classification with ID '{id}' was not found.");
    }
    #endregion
    #region Events
    public static class Events
    {
        public record Created(Guid ClassificationId) : DomainEvent;
        public record Updated(Guid ClassificationId) : DomainEvent;
        public record Deleted(Guid ClassificationId) : DomainEvent;
    }
    #endregion
}

