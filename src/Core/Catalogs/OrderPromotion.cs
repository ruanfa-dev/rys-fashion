using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Domain model port of Spree::OrderPromotion (simplified).
/// - Links a Promotion to an Order and exposes a convenience Amount() that sums promotion adjustments.
/// - Exact matching between an Adjustment and a Promotion action is best-effort here (uses reflection),
///   infra should provide repositories/queries for accurate computation when needed.
/// </summary>
public sealed class OrderPromotion : AuditableEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public Guid PromotionId { get; set; }
    public Promotion? Promotion { get; set; }

    private OrderPromotion() { }

    public static OrderPromotion Create(Guid orderId, Guid promotionId)
    {
        return new OrderPromotion
        {
            OrderId = orderId,
            PromotionId = promotionId
        };
    }

    // Delegated convenience accessors
    public string? Name => Promotion?.Name;
    public string? Description => Promotion?.Description;
    public string? Code => Promotion?.Code;
    public object? PublicMetadata => Promotion?.PublicMetadata;
    public string? Currency => Order?.Currency;

    /// <summary>
    /// Compute sum of promotion adjustments that originate from this promotion's actions.
    /// This is a best-effort in-memory computation: infrastructure should perform SQL aggregation for correctness/performance.
    /// </summary>
    public decimal Amount()
    {
        if (Order == null || Promotion == null) return 0m;

        var allAdjustments = GetOrderAdjustments();
        if (allAdjustments == null) return 0m;

        var actions = GetPromotionActions();
        if (actions == null || !actions.Any()) return 0m;

        decimal sum = 0m;
        foreach (var adj in allAdjustments.Where(a => IsPromotionAdjustment(a)))
        {
            if (AdjustmentMatchesPromotionActions(adj, actions)) sum += GetAdjustmentAmount(adj);
        }

        return sum;
    }

    // helper: try to read Order.AllAdjustments or Order.Adjustments (best-effort)
    private IEnumerable<Adjustment>? GetOrderAdjustments()
    {
        try
        {
            var prop = Order!.GetType().GetProperty("AllAdjustments") ?? Order.GetType().GetProperty("Adjustments");
            if (prop == null) return null;
            var val = prop.GetValue(Order);
            return val as IEnumerable<Adjustment>;
        }
        catch
        {
            return null;
        }
    }

    // helper: attempt to get Promotion.Actions collection (best-effort)
    private IEnumerable<object>? GetPromotionActions()
    {
        try
        {
            var prop = Promotion!.GetType().GetProperty("Actions") ?? Promotion.GetType().GetProperty("PromotionActions");
            if (prop == null) return null;
            var val = prop.GetValue(Promotion);
            return (val as IEnumerable<object>) ?? (val as IEnumerable<PromotionAction>)?.Cast<object>();
        }
        catch
        {
            return null;
        }
    }

    private static bool IsPromotionAdjustment(Adjustment adj)
    {
        try
        {
            // prefer explicit flag if present
            var isPromoProp = adj.GetType().GetProperty("IsPromotion");
            if (isPromoProp != null && isPromoProp.GetValue(adj) is bool b) return b;

            // fallback to checking Source/SourceType
            var sourceProp = adj.GetType().GetProperty("Source");
            if (sourceProp != null && sourceProp.GetValue(adj) != null) return true;

            var sourceTypeProp = adj.GetType().GetProperty("SourceType");
            if (sourceTypeProp != null && sourceTypeProp.GetValue(adj) is string s && !string.IsNullOrWhiteSpace(s))
                return s.Contains("Promotion", StringComparison.OrdinalIgnoreCase);

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static decimal GetAdjustmentAmount(Adjustment adj)
    {
        try
        {
            return adj.Amount;
        }
        catch
        {
            // fallback via reflection
            var p = adj.GetType().GetProperty("Amount");
            if (p != null && p.GetValue(adj) is decimal d) return d;
            return 0m;
        }
    }

    private static bool AdjustmentMatchesPromotionActions(Adjustment adj, IEnumerable<object> actions)
    {
        try
        {
            // Try to obtain adjustment source object first
            var srcProp = adj.GetType().GetProperty("Source");
            var srcObj = srcProp?.GetValue(adj);

            // Try to obtain SourceId if present
            var srcIdProp = adj.GetType().GetProperty("SourceId");
            var srcId = srcIdProp?.GetValue(adj);

            foreach (var action in actions)
            {
                if (action == null) continue;

                // direct reference equality
                if (ReferenceEquals(action, srcObj)) return true;

                // compare ids if available on both
                var actIdProp = action.GetType().GetProperty("Id") ?? action.GetType().GetProperty("Id");
                var actId = actIdProp?.GetValue(action);
                if (srcId != null && actId != null && srcId.Equals(actId)) return true;

                // compare by type+id (common polymorphic pattern)
                var actTypeName = action.GetType().Name;
                var adjSourceTypeProp = adj.GetType().GetProperty("SourceType");
                var adjSourceType = adjSourceTypeProp?.GetValue(adj) as string;
                if (!string.IsNullOrWhiteSpace(adjSourceType) && adjSourceType.Contains(actTypeName)) return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    #region Validation / Errors

    public static class Errors
    {
        public static Error OrderRequired => Error.Validation("OrderPromotion.OrderRequired", "Order is required.");
        public static Error PromotionRequired => Error.Validation("OrderPromotion.PromotionRequired", "Promotion is required.");
        public static Error Duplicate => Error.Conflict("OrderPromotion.Duplicate", "Promotion is already applied to order.");
        public static Error NotFound(Guid id) => Error.NotFound("OrderPromotion.NotFound", $"OrderPromotion with ID '{id}' was not found.");
    }

    public static List<Error> ValidateModel(Guid orderId, Guid promotionId)
    {
        var errors = new List<Error>();
        if (orderId == Guid.Empty) errors.Add(Errors.OrderRequired);
        if (promotionId == Guid.Empty) errors.Add(Errors.PromotionRequired);
        return errors;
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid OrderPromotionId) : DomainEvent;
        public record Deleted(Guid OrderPromotionId) : DomainEvent;
    }

    #endregion
}