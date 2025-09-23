using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Domain port of Spree::StoreCreditEvent (simplified).
/// - Records lifecycle events for a StoreCredit (allocations, captures, authorizations, credits, voids, etc.).
/// - Polymorphic originator is modelled with Id/Type; infra may map concrete originator instances.
/// - Payment / Order associations are navigational and should be populated by repositories when needed.
/// </summary>
public sealed class StoreCreditEvent : AuditableEntity
{
    // Common action names used by handlers/UI (best-effort defaults matching Spree)
    public const string CAPTURE_ACTION = "capture";
    public const string AUTHORIZE_ACTION = "authorize";
    public const string ALLOCATION_ACTION = "allocation";
    public const string ELIGIBLE_ACTION = "eligible";
    public const string VOID_ACTION = "void";
    public const string CREDIT_ACTION = "credit";

    public Guid StoreCreditId { get; set; }
    public StoreCredit? StoreCredit { get; set; }

    // Polymorphic originator (type + id). Infra may populate Originator navigation.
    public Guid? OriginatorId { get; set; }
    public string? OriginatorType { get; set; }
    public object? Originator { get; set; }

    // Monetary / business fields
    public string? Action { get; set; }
    public decimal Amount { get; set; }
    public decimal UserTotalAmount { get; set; }
    public string? AuthorizationCode { get; set; }

    // Navigations populated by infra/repository when available
    public Payment? Payment { get; set; }

    // Convenience: Order via Payment when available
    public Order? Order => Payment?.Order;

    private StoreCreditEvent() { }

    public static StoreCreditEvent Create(Guid storeCreditId, string action, decimal amount = 0m, decimal userTotalAmount = 0m, string? authorizationCode = null)
        => new StoreCreditEvent
        {
            StoreCreditId = storeCreditId,
            Action = action,
            Amount = amount,
            UserTotalAmount = userTotalAmount,
            AuthorizationCode = authorizationCode
        };

    // ---- scopes / helpers (in-memory equivalents) ----

    public static IEnumerable<StoreCreditEvent> ExposedEvents(IEnumerable<StoreCreditEvent> events)
    {
        if (events == null) return Enumerable.Empty<StoreCreditEvent>();
        return events.Where(e => !string.Equals(e.Action, ELIGIBLE_ACTION, StringComparison.OrdinalIgnoreCase)
                                 && !string.Equals(e.Action, AUTHORIZE_ACTION, StringComparison.OrdinalIgnoreCase));
    }

    public static IEnumerable<StoreCreditEvent> ReverseChronological(IEnumerable<StoreCreditEvent> events)
    {
        if (events == null) return Enumerable.Empty<StoreCreditEvent>();
        return events.OrderByDescending(e => e.CreatedAt);
    }

    // ---- display / convenience ----

    public string DisplayAction()
    {
        if (string.IsNullOrWhiteSpace(Action)) return string.Empty;

        return Action.ToLowerInvariant() switch
        {
            CAPTURE_ACTION => "Captured",
            AUTHORIZE_ACTION => "Authorized",
            ALLOCATION_ACTION => "Allocated",
            ELIGIBLE_ACTION => "Eligible",
            VOID_ACTION => "Credit",
            CREDIT_ACTION => "Credit",
            _ => Action
        };
    }

    public bool Allocation() => string.Equals(Action, ALLOCATION_ACTION, StringComparison.OrdinalIgnoreCase);
    public bool Credit() => string.Equals(Action, CREDIT_ACTION, StringComparison.OrdinalIgnoreCase);
    public bool Captured() => string.Equals(Action, CAPTURE_ACTION, StringComparison.OrdinalIgnoreCase);
    public bool Voided() => string.Equals(Action, VOID_ACTION, StringComparison.OrdinalIgnoreCase);
    public bool Authorized() => string.Equals(Action, AUTHORIZE_ACTION, StringComparison.OrdinalIgnoreCase);

    public string DisplayAmount(string? currency = null)
        => FormatMoney(Amount, currency ?? StoreCredit?.Currency);

    public string DisplayUserTotalAmount(string? currency = null)
        => FormatMoney(UserTotalAmount, currency ?? StoreCredit?.Currency);

    private static string FormatMoney(decimal amount, string? currency)
    {
        var formatted = amount.ToString("N2", CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(currency) ? formatted : $"{formatted} {currency.ToUpperInvariant()}";
    }

    #region Validation / Errors

    public static class Errors
    {
        public static Error StoreCreditRequired => Error.Validation("StoreCreditEvent.StoreCreditRequired", "Store credit is required.");
        public static Error InvalidAmount => Error.Validation("StoreCreditEvent.InvalidAmount", "Amount must be a valid monetary value.");
        public static Error NotFound(Guid id) => Error.NotFound("StoreCreditEvent.NotFound", $"StoreCreditEvent with ID '{id}' was not found.");
    }

    public static List<Error> ValidateModel(Guid storeCreditId, string? action, decimal? amount)
    {
        var errors = new List<Error>();
        if (storeCreditId == Guid.Empty) errors.Add(Errors.StoreCreditRequired);
        if (string.IsNullOrWhiteSpace(action)) errors.Add(Error.Validation("StoreCreditEvent.ActionRequired", "Action is required."));
        if (amount.HasValue && (decimal.IsNaN(amount.Value) || decimal.IsInfinity(amount.Value))) errors.Add(Errors.InvalidAmount);
        return errors;
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid StoreCreditEventId) : DomainEvent;
        public record Deleted(Guid StoreCreditEventId) : DomainEvent;
    }

    #endregion
}