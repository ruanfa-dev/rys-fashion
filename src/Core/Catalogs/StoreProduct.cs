using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

using System;
using System.Collections.Generic;

namespace Core.Catalogs;

/// <summary>
/// Join entity between Store and Product (port of Spree::StoreProduct / spree_products_stores).
/// DB-level uniqueness (StoreId, ProductId) should be enforced in EF configuration/migrations.
/// </summary>
public sealed class StoreProduct : AuditableEntity
{
    public Guid StoreId { get; set; }
    public Store? Store { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    private StoreProduct() { }

    public static StoreProduct Create(Guid storeId, Guid productId)
        => new StoreProduct
        {
            StoreId = storeId,
            ProductId = productId
        };

    /// <summary>
    /// Lightweight validation used by handlers before persistence.
    /// Uniqueness must be enforced/checked by repository layer.
    /// </summary>
    public static List<Error> ValidateModel(Guid storeId, Guid productId)
    {
        var errors = new List<Error>();
        if (storeId == Guid.Empty) errors.Add(Errors.StoreRequired);
        if (productId == Guid.Empty) errors.Add(Errors.ProductRequired);
        return errors;
    }

    public static class Errors
    {
        public static Error StoreRequired =>
            Error.Validation("StoreProduct.StoreRequired", "Store is required.");

        public static Error ProductRequired =>
            Error.Validation("StoreProduct.ProductRequired", "Product is required.");

        public static Error DuplicateAssignment(Guid storeId, Guid productId) =>
            Error.Conflict("StoreProduct.DuplicateAssignment", $"Product '{productId}' is already assigned to store '{storeId}'.");

        public static Error NotFound(Guid id) =>
            Error.NotFound("StoreProduct.NotFound", $"StoreProduct with ID '{id}' was not found.");
    }

    public static class Events
    {
        public record Created(Guid StoreProductId) : DomainEvent;
        public record Deleted(Guid StoreProductId) : DomainEvent;
    }
}