using Core.Identity;

using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.Catalogs;

/// <summary>
/// Domain port of Spree::StoreCredit (simplified).
/// - Business operations (authorize/capture/void/credit) are implemented in-memory; persistence is responsibility of application/infra.
/// - Event recording is done in-memory via StoreCreditEvents collection; infra should persist events and wire callbacks.
/// </summary>
public sealed class StoreCredit : AuditableEntity
{
    public const string VOID_ACTION = "void";
    public const string CANCEL_ACTION = "cancel";
    public const string CREDIT_ACTION = "credit";
    public const string CAPTURE_ACTION = "capture";
    public const string ELIGIBLE_ACTION = "eligible";
    public const string AUTHORIZE_ACTION = "authorize";
    public const string ALLOCATION_ACTION = "allocation";

    // Associations
    public Guid StoreId { get; set; }
    public Store? Store { get; set; }

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public Guid? CategoryId { get; set; }
    public StoreCreditCategory? Category { get; set; }

    public Guid? CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public Guid? TypeId { get; set; }
    public StoreCreditType? CreditType { get; set; }

    // Polymorphic originator
    public Guid? OriginatorId { get; set; }
    public string? OriginatorType { get; set; }
    public object? Originator { get; set; }

    // Money fields
    public decimal Amount { get; set; }
    public decimal AmountUsed { get; set; }
    public decimal AmountAuthorized { get; set; }
    public string Currency { get; set; } = string.Empty;

    public string? Memo { get; set; }

    // action context (mirrors transient attr_accessor in Ruby)
    public string? Action { get; private set; }
    public decimal? ActionAmount { get; private set; }
    public object? ActionOriginator { get; private set; }
    public string? ActionAuthorizationCode { get; private set; }

    public ICollection<StoreCreditEvent> StoreCreditEvents { get; set; } = new List<StoreCreditEvent>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public IEnumerable<Order> Orders => Payments.Select(p => p.Order).Where(o => o != null)!;

    private StoreCredit() { }

    public static StoreCredit Create(
        Guid storeId,
        decimal amount,
        string currency,
        Guid? userId = null,
        Guid? categoryId = null,
        Guid? typeId = null,
        string? memo = null)
    {
        return new StoreCredit
        {
            StoreId = storeId,
            Amount = amount,
            Currency = currency,
            UserId = userId,
            CategoryId = categoryId,
            TypeId = typeId,
            Memo = memo ?? string.Empty
        };
    }

    // Computed
    public decimal AmountRemaining => Amount - AmountUsed - AmountAuthorized;

    // Actions list
    public IEnumerable<string> Actions() => new[] { CAPTURE_ACTION, VOID_ACTION, CREDIT_ACTION };

    // Behavior: returns authorization code when successful, null otherwise
    public string? Authorize(decimal amount, string orderCurrency, object? originator = null, string? providedAuthorizationCode = null)
    {
        var authCode = providedAuthorizationCode ?? GenerateAuthorizationCode();

        if (!ValidateAuthorization(amount, orderCurrency, out var _))
            return null;

        // don't double-authorize the same code
        if (!string.IsNullOrWhiteSpace(providedAuthorizationCode))
        {
            if (StoreCreditEvents.Any(e => string.Equals(e.Action, AUTHORIZE_ACTION, StringComparison.OrdinalIgnoreCase)
                                          && string.Equals(e.AuthorizationCode, providedAuthorizationCode, StringComparison.OrdinalIgnoreCase)))
                return authCode;
        }

        // record transient action context, modify state
        Action = AUTHORIZE_ACTION;
        ActionAmount = amount;
        ActionOriginator = originator;
        ActionAuthorizationCode = authCode;

        AmountAuthorized += amount;
        MarkAsUpdated();

        // create an in-memory event (infra persists later)
        StoreEvent();

        return authCode;
    }

    public bool ValidateAuthorization(decimal amount, string orderCurrency, out string? error)
    {
        error = null;
        if (AmountRemaining < amount)
        {
            error = "insufficient_funds";
            return false;
        }
        if (!string.Equals(Currency, orderCurrency, StringComparison.OrdinalIgnoreCase))
        {
            error = "currency_mismatch";
            return false;
        }
        return true;
    }

    // Capture: returns authorization code when successful, null otherwise
    public string? Capture(decimal amount, string authorizationCode, string orderCurrency, object? originator = null)
    {
        // Ensure the authorization exists (or authorize inline)
        var existingAuth = StoreCreditEvents.FirstOrDefault(e =>
            string.Equals(e.Action, AUTHORIZE_ACTION, StringComparison.OrdinalIgnoreCase)
            && string.Equals(e.AuthorizationCode, authorizationCode, StringComparison.OrdinalIgnoreCase));

        if (existingAuth == null)
        {
            // attempt to authorize first (will generate code if null)
            var auth = Authorize(amount, orderCurrency, originator, authorizationCode);
            if (auth == null) return null;
        }

        if (amount > AmountAuthorized)
        {
            // insufficient authorized amount
            return null;
        }

        if (!string.Equals(Currency, orderCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        Action = CAPTURE_ACTION;
        ActionAmount = amount;
        ActionOriginator = originator;
        ActionAuthorizationCode = authorizationCode;

        AmountUsed += amount;
        AmountAuthorized -= amount;
        MarkAsUpdated();

        StoreEvent();

        return authorizationCode;
    }

    public bool Void(string authorizationCode, object? originator = null)
    {
        var authEvent = StoreCreditEvents.FirstOrDefault(e =>
            string.Equals(e.Action, AUTHORIZE_ACTION, StringComparison.OrdinalIgnoreCase)
            && string.Equals(e.AuthorizationCode, authorizationCode, StringComparison.OrdinalIgnoreCase));

        if (authEvent == null) return false;

        Action = VOID_ACTION;
        ActionAmount = authEvent.Amount;
        ActionAuthorizationCode = authorizationCode;
        ActionOriginator = originator;

        AmountAuthorized -= authEvent.Amount;
        MarkAsUpdated();

        StoreEvent();
        return true;
    }

    public bool Credit(decimal amount, string authorizationCode, string orderCurrency, object? originator = null)
    {
        if (!string.Equals(Currency, orderCurrency, StringComparison.OrdinalIgnoreCase))
            return false;

        var captureEvent = StoreCreditEvents.FirstOrDefault(e =>
            string.Equals(e.Action, CAPTURE_ACTION, StringComparison.OrdinalIgnoreCase)
            && string.Equals(e.AuthorizationCode, authorizationCode, StringComparison.OrdinalIgnoreCase));

        if (captureEvent == null || amount > captureEvent.Amount) return false;

        Action = CREDIT_ACTION;
        ActionAmount = amount;
        ActionAuthorizationCode = authorizationCode;
        ActionOriginator = originator;

        // By default we update amount_used (return money back). If application wants a new allocation it can create a new StoreCredit.
        AmountUsed -= amount;
        if (AmountUsed < 0m) AmountUsed = 0m;
        MarkAsUpdated();

        StoreEvent();
        return true;
    }

    public bool CanCapture(Payment payment) => payment.Pending || payment.Checkout;
    public bool CanVoid(Payment payment) => payment.Pending || (payment.Checkout && !payment.Order.Completed());
    public bool CanCredit(Payment payment) => payment.Completed && payment.CreditAllowed > 0m;

    public bool Editable() => AmountUsed == 0m && AmountAuthorized == 0m;
    public bool CanBeDeleted() => AmountUsed == 0m && AmountAuthorized == 0m;

    public string GenerateAuthorizationCode()
    {
        // deterministic-ish but unique per Id/time
        return $"{Id}-SC-{DateTime.UtcNow:yyyyMMddHHmmssffffff}";
    }

    // Create credit record: returns new StoreCredit when a new allocation is requested or null when adjusted in place
    public StoreCredit? CreateCreditRecord(decimal amount, bool creditToNewAllocation = false)
    {
        var actionAttributes = new
        {
            Action = CREDIT_ACTION,
            ActionAmount = amount,
            ActionOriginator = ActionOriginator,
            ActionAuthorizationCode = ActionAuthorizationCode
        };

        if (creditToNewAllocation)
        {
            var credit = Create(
                storeId: StoreId,
                amount: amount,
                currency: Currency,
                userId: UserId,
                categoryId: CategoryId,
                typeId: TypeId,
                memo: CreditAllocationMemo());
            // apply transient attributes
            credit.Action = CREDIT_ACTION;
            credit.ActionAmount = amount;
            credit.ActionAuthorizationCode = ActionAuthorizationCode;
            credit.ActionOriginator = ActionOriginator;
            credit.MarkAsUpdated();
            credit.StoreEvent();
            return credit;
        }
        else
        {
            // update this record in-place (refund the used amount)
            AmountUsed -= amount;
            if (AmountUsed < 0m) AmountUsed = 0m;
            MarkAsUpdated();
            StoreEvent();
            return null;
        }
    }

    private string CreditAllocationMemo()
        => $"This is a credit from store credit ID {Id}";

    /// <summary>
    /// Build or update an in-memory StoreCreditEvent reflecting the last action.
    /// Infra should persist events after save/commit.
    /// </summary>
    public void StoreEvent()
    {
        // Determine the event to update or create
        StoreCreditEvent? ev;
        if (!string.IsNullOrWhiteSpace(Action))
        {
            // new action -> build event
            ev = new StoreCreditEvent
            {
                StoreCreditId = Id,
                Action = Action,
            };
            StoreCreditEvents.Add(ev);
        }
        else
        {
            // fallback to allocation event
            ev = StoreCreditEvents.FirstOrDefault(e => string.Equals(e.Action, ALLOCATION_ACTION, StringComparison.OrdinalIgnoreCase))
                 ?? new StoreCreditEvent { StoreCreditId = Id, Action = ALLOCATION_ACTION };
            if (!StoreCreditEvents.Contains(ev)) StoreCreditEvents.Add(ev);
        }

        ev.Amount = ActionAmount ?? Amount;
        ev.AuthorizationCode = ActionAuthorizationCode ?? ev.AuthorizationCode ?? GenerateAuthorizationCode();
        ev.UserTotalAmount = User?.TotalAvailableStoreCredit() ?? 0m;
        ev.Originator = ActionOriginator ?? Originator;

        // clear transient action context after building event
        Action = null;
        ActionAmount = null;
        ActionAuthorizationCode = null;
        ActionOriginator = null;

        MarkAsUpdated();
        AddDomainEvent(new Events.Updated(Id));
    }

    #region Validation helpers (lightweight)

    public static List<Error> ValidateModel(decimal? amount, string? currency)
    {
        var errors = new List<Error>();
        if (!amount.HasValue || amount <= 0m) errors.Add(Error.Validation("StoreCredit.AmountInvalid", "Amount must be greater than 0."));
        if (string.IsNullOrWhiteSpace(currency)) errors.Add(Error.Validation("StoreCredit.CurrencyRequired", "Currency is required."));
        return errors;
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid StoreCreditId) : DomainEvent;
        public record Updated(Guid StoreCreditId) : DomainEvent;
        public record Deleted(Guid StoreCreditId) : DomainEvent;
    }

    #endregion
}