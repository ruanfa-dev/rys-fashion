using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Domain model port of Spree::DigitalLink (simplified).
/// - Token generation and persistence are infra responsibilities; domain ensures token exists and exposes helpers.
/// - Authorization / access counting logic is implemented in-domain; store preference lookups are best-effort via reflection.
/// - Application layer must persist changes (Authorize/Reset) after calling these methods.
/// </summary>
public sealed class DigitalLink : AuditableEntity
{
    public Guid DigitalId { get; set; }
    public Digital? Digital { get; set; }

    public Guid LineItemId { get; set; }
    public LineItem? LineItem { get; set; }

    // Secure token (generated when missing)
    public string Token { get; private set; } = string.Empty;

    // Number of times link was used
    public int AccessCounter { get; private set; }

    private DigitalLink() { }

    public static DigitalLink Create(Guid digitalId, Guid lineItemId, string? token = null)
    {
        var dl = new DigitalLink
        {
            DigitalId = digitalId,
            LineItemId = lineItemId,
            AccessCounter = 0
        };
        if (!string.IsNullOrWhiteSpace(token)) dl.Token = token!.Trim();
        dl.EnsureToken();
        return dl;
    }

    private void EnsureToken()
    {
        if (!string.IsNullOrWhiteSpace(Token)) return;
        Token = GenerateSecureToken();
        MarkAsUpdated();
    }

    private static string GenerateSecureToken(int bytes = 16)
    {
        var buffer = new byte[bytes];
        RandomNumberGenerator.Fill(buffer);
        // hex representation (compact and URL-safe)
        var sb = new StringBuilder(buffer.Length * 2);
        foreach (var b in buffer) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    // ---- Authorization helpers ----

    public bool Authorizable()
    {
        return !IsExpired() && !IsAccessLimitExceeded();
    }

    public bool IsExpired()
    {
        var store = GetStore();
        if (store == null) return false;

        // Look for preference flag enabling expiry and number of days
        var limitEnabled = GetBoolPref(store, "PreferredLimitDigitalDownloadDays", "preferred_limit_digital_download_days");
        if (!limitEnabled) return false;

        var days = GetIntPref(store, "PreferredDigitalAssetAuthorizedDays", "preferred_digital_asset_authorized_days");
        if (days == null) return false;

        var createdAt = CreatedAt == default ? DateTimeOffset.UtcNow : CreatedAt;
        return createdAt <= DateTimeOffset.UtcNow.AddDays(-days.Value);
    }

    public bool IsAccessLimitExceeded()
    {
        var store = GetStore();
        if (store == null) return false;

        var limitEnabled = GetBoolPref(store, "PreferredLimitDigitalDownloadCount", "preferred_limit_digital_download_count");
        if (!limitEnabled) return false;

        var maxClicks = GetIntPref(store, "PreferredDigitalAssetAuthorizedClicks", "preferred_digital_asset_authorized_clicks");
        if (maxClicks == null) return false;

        return AccessCounter >= maxClicks.Value;
    }

    /// <summary>
    /// Attempt to authorize (consumes one access). Returns true if authorization granted and counter incremented.
    /// Application layer should persist the entity after calling this method.
    /// </summary>
    public bool Authorize()
    {
        if (!Authorizable()) return false;

        AccessCounter++;
        MarkAsUpdated();
        AddDomainEvent(new Events.Authorized(Id));
        return true;
    }

    public void Reset()
    {
        AccessCounter = 0;
        CreatedAt = DateTimeOffset.UtcNow;
        MarkAsUpdated();
        AddDomainEvent(new Events.Reset(Id));
    }

    private object? GetStore()
    {
        // Try to reach LineItem -> Order -> Store via properties if present.
        try
        {
            Order? order = LineItem?.Order;
            if (order == null) return null;

            var prop = order.GetType().GetProperty("Store", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop != null) return prop.GetValue(order);
            // fallback to DefaultStore/StoreId patterns not attempted here
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static bool GetBoolPref(object store, string pascalName, string snakeName)
    {
        var prop = store.GetType().GetProperty(pascalName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                   ?? store.GetType().GetProperty(ToPascal(snakeName), BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop == null) return false;
        var val = prop.GetValue(store);
        if (val is bool b) return b;
        return false;
    }

    private static int? GetIntPref(object store, string pascalName, string snakeName)
    {
        var prop = store.GetType().GetProperty(pascalName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                   ?? store.GetType().GetProperty(ToPascal(snakeName), BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop == null) return null;
        var val = prop.GetValue(store);
        if (val is int i) return i;
        if (val is long l) return (int)l;
        if (val is string s && int.TryParse(s, out var parsed)) return parsed;
        return null;
    }

    private static string ToPascal(string snake)
    {
        return string.Join("", snake.Split('_').Select(s => char.ToUpperInvariant(s[0]) + s.Substring(1)));
    }

    #region Validation / Constraints / Errors

    public static class Constraints
    {
        public const int TokenMaxLength = 255;
    }

    public static class Errors
    {
        public static Error DigitalRequired => Error.Validation("DigitalLink.DigitalRequired", "Digital is required.");
        public static Error LineItemRequired => Error.Validation("DigitalLink.LineItemRequired", "Line item is required.");
        public static Error AccessCounterInvalid => Error.Validation("DigitalLink.AccessCounterInvalid", "Access counter must be >= 0.");
        public static Error NotFound(Guid id) => Error.NotFound("DigitalLink.NotFound", $"DigitalLink with ID '{id}' was not found.");
    }

    public static System.Collections.Generic.List<Error> ValidateModel(Guid digitalId, Guid lineItemId, int? accessCounter = null)
    {
        var errors = new System.Collections.Generic.List<Error>();
        if (digitalId == Guid.Empty) errors.Add(Errors.DigitalRequired);
        if (lineItemId == Guid.Empty) errors.Add(Errors.LineItemRequired);
        if (accessCounter.HasValue && accessCounter < 0) errors.Add(Errors.AccessCounterInvalid);
        return errors;
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid DigitalLinkId) : DomainEvent;
        public record Authorized(Guid DigitalLinkId) : DomainEvent;
        public record Reset(Guid DigitalLinkId) : DomainEvent;
        public record Deleted(Guid DigitalLinkId) : DomainEvent;
    }

    #endregion
}