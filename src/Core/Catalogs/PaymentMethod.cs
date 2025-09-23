using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Domain port of Spree::PaymentMethod (simplified).
/// - Persistence, gateway integrations and provider registration belong to infra.
/// - This class exposes the behavioural surface used by application code.
/// </summary>
public class PaymentMethod : AuditableEntity
{
    #region Core fields

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Active flag (scope :active).
    /// </summary>
    public bool Active { get; set; } = true;

    /// <summary>
    /// Position used by acts_as_list
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// DisplayOn (frontend/back end/both) — infra may map constants.
    /// </summary>
    public int DisplayOn { get; set; }

    /// <summary>
    /// Optional flag: whether this payment method auto-captures payments.
    /// Null = defer to application default provider.
    /// </summary>
    public bool? AutoCaptureFlag { get; set; }

    /// <summary>
    /// Preferences bag (simple domain-level representation).
    /// </summary>
    public IDictionary<string, object?> Preferences { get; set; } = new Dictionary<string, object?>();

    #endregion

    #region Navigations / joins

    public ICollection<StorePaymentMethod> StorePaymentMethods { get; set; } = new List<StorePaymentMethod>();
    public IEnumerable<Store> Stores => StorePaymentMethods.Select(spm => spm.Store).Where(s => s != null)!;

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<CreditCard> CreditCards { get; set; } = new List<CreditCard>();
    public ICollection<GatewayCustomer> GatewayCustomers { get; set; } = new List<GatewayCustomer>();

    #endregion

    #region Infra hooks / configuration

    /// <summary>
    /// Infra can assign a provider that returns available payment providers (e.g. configured gateways).
    /// </summary>
    public static Func<IEnumerable<string>>? ProvidersRegistry { get; set; }

    /// <summary>
    /// Infra can provide application default for auto-capture behavior.
    /// </summary>
    public static Func<bool>? DefaultAutoCaptureProvider { get; set; }

    #endregion

    private PaymentMethod() { }

    public static PaymentMethod Create(string name, bool active = true, int position = 0)
    {
        return new PaymentMethod
        {
            Name = (name ?? string.Empty).Trim(),
            Active = active,
            Position = position
        };
    }

    #region Behavioural helpers (mirror Spree methods)

    public static IEnumerable<string> Providers() => ProvidersRegistry?.Invoke() ?? Enumerable.Empty<string>();

    /// <summary>
    /// Concrete gateway implementations should override and return the provider/class that performs gateway calls.
    /// </summary>
    public virtual Type ProviderClass()
    {
        throw new NotImplementedException("You must implement ProviderClass for this payment method.");
    }

    /// <summary>
    /// If the payment method requires a payment source (credit card, token) return its Type here.
    /// Null means no external source required (e.g. Check).
    /// </summary>
    public virtual Type? PaymentSourceClass()
    {
        return null;
    }

    /// <summary>
    /// Returns the method type key derived from CLR type name (demodulize/downcase equivalent).
    /// </summary>
    public virtual string MethodType()
        => GetType().Name.ToLowerInvariant();

    /// <summary>
    /// Default human name inferred from class name, removing 'Gateway' suffix when present.
    /// </summary>
    public virtual string DefaultName()
    {
        var name = GetType().Name;
        if (name.EndsWith("Gateway", StringComparison.OrdinalIgnoreCase))
            name = name.Substring(0, name.Length - "Gateway".Length);
        // split PascalCase to words
        return System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2").Trim();
    }

    /// <summary>
    /// Icon name used by UI. Best-effort transformation of the CLR type.
    /// </summary>
    public virtual string PaymentIconName()
    {
        var n = GetType().Name;
        n = n.Replace("SpreeGateway", "", StringComparison.OrdinalIgnoreCase);
        n = n.Replace("Gateway", "", StringComparison.OrdinalIgnoreCase);
        return n.ToLowerInvariant().Replace(" ", "").Trim();
    }

    public virtual bool PaymentProfilesSupported() => false;

    public virtual bool SourceRequired() => true;

    public virtual bool ShowInAdmin() => true;

    /// <summary>
    /// Gateway implementations may return reusable sources for an order (tokens, saved cards).
    /// </summary>
    public virtual IEnumerable<object> ReusableSources(Order? order) => Enumerable.Empty<object>();

    public virtual bool AutoCapture()
        => AutoCaptureFlag ?? DefaultAutoCaptureProvider?.Invoke() ?? false;

    public virtual bool Supports(object? source) => true;

    public virtual void Cancel(object? response)
    {
        throw new NotImplementedException("You must implement Cancel for this payment method.");
    }

    public virtual bool IsStoreCredit()
        => GetType().Name.Equals("StoreCredit", StringComparison.OrdinalIgnoreCase) ||
           GetType().Name.EndsWith("StoreCredit", StringComparison.OrdinalIgnoreCase);

    public virtual bool AvailableForOrder(Order? order)
    {
        if (order == null) return true;
        // if order indicates it is covered by store credit, exclude methods that shouldn't apply
        try
        {
            var prop = order.GetType().GetProperty("CoveredByStoreCredit") ?? order.GetType().GetProperty("covered_by_store_credit", System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (prop != null && prop.GetValue(order) is bool covered && covered) return false;
        }
        catch { /* best-effort */ }

        return true;
    }

    public virtual bool AvailableForStore(Store? store)
    {
        if (store == null) return true;
        return StoreIds().Contains(store.Id);
    }

    public IDictionary<string, object?> PublicPreferences()
    {
        var keys = PublicPreferenceKeys();
        var result = new Dictionary<string, object?>();
        foreach (var k in keys)
        {
            if (Preferences.TryGetValue(k, out var v)) result[k] = v;
        }
        return result;
    }

    protected virtual IEnumerable<string> PublicPreferenceKeys() => Enumerable.Empty<string>();

    #endregion

    #region Convenience / helpers

    public IEnumerable<Guid> StoreIds() => StorePaymentMethods.Select(spm => spm.StoreId);

    #endregion

    #region Validation / Errors

    public static class Constraints
    {
        public const int NameMaxLength = 255;
    }

    public static class Errors
    {
        public static Error NameRequired => Error.Validation("PaymentMethod.NameRequired", "Payment method name is required.");
        public static Error NotFound(Guid id) => Error.NotFound("PaymentMethod.NotFound", $"PaymentMethod with ID '{id}' was not found.");
    }

    public static List<Error> ValidateModel(string? name)
    {
        var errors = new List<Error>();
        if (string.IsNullOrWhiteSpace(name)) errors.Add(Errors.NameRequired);
        return errors;
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid PaymentMethodId) : DomainEvent;
        public record Updated(Guid PaymentMethodId) : DomainEvent;
        public record Deleted(Guid PaymentMethodId) : DomainEvent;
    }

    #endregion
}
