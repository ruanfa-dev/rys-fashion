using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

using System;
using System.Collections.Generic;

namespace Core.Catalogs;

/// <summary>
/// Join entity between Store and Promotion (port of Spree::StorePromotion / spree_promotions_stores).
/// DB-level uniqueness (StoreId, PromotionId) should be enforced in EF configuration/migrations.
/// </summary>
public sealed class StorePromotion : AuditableEntity
{
    public Guid StoreId { get; set; }
    public Store? Store { get; set; }

    public Guid PromotionId { get; set; }
    public Promotion? Promotion { get; set; }

    private StorePromotion() { }

    public static StorePromotion Create(Guid storeId, Guid promotionId)
        => new StorePromotion
        {
            StoreId = storeId,
            PromotionId = promotionId
        };

    /// <summary>
    /// Lightweight validation used by handlers before persistence.
    /// Uniqueness must be enforced/checked by repository layer.
    /// </summary>
    public static List<Error> ValidateModel(Guid storeId, Guid promotionId)
    {
        var errors = new List<Error>();
        if (storeId == Guid.Empty) errors.Add(Errors.StoreRequired);
        if (promotionId == Guid.Empty) errors.Add(Errors.PromotionRequired);
        return errors;
    }

    public static class Errors
    {
        public static Error StoreRequired =>
            Error.Validation("StorePromotion.StoreRequired", "Store is required.");

        public static Error PromotionRequired =>
            Error.Validation("StorePromotion.PromotionRequired", "Promotion is required.");

        public static Error DuplicateAssignment(Guid storeId, Guid promotionId) =>
            Error.Conflict("StorePromotion.DuplicateAssignment", $"Promotion '{promotionId}' is already assigned to store '{storeId}'.");

        public static Error NotFound(Guid id) =>
            Error.NotFound("StorePromotion.NotFound", $"StorePromotion with ID '{id}' was not found.");
    }

    public static class Events
    {
        public record Created(Guid StorePromotionId) : DomainEvent;
        public record Deleted(Guid StorePromotionId) : DomainEvent;
    }
}