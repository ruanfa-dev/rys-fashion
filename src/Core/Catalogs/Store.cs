using ErrorOr;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Core.Catalogs;

/// <summary>
/// Domain-level port of Spree::Store (simplified).
/// - Persistence, translations, attachments and many callbacks belong to infra/application layers.
/// - Methods that require DB queries accept collections or rely on infra to provide repository implementations.
/// </summary>
public sealed class Store : AuditableEntity
{
    #region Core fields

    public string Name { get; set; } = default!;
    public string? Url { get; set; }
    public string Code { get; set; } = default!;
    public string DefaultLocale { get; set; } = CultureInfo.InvariantCulture.Name;
    public string DefaultCurrency { get; set; } = "USD";
    public Guid DefaultCountryId { get; set; }
    public Country? DefaultCountry { get; set; }
    public bool Default { get; set; }

    public string? MailFromAddress { get; set; }

    #endregion

    #region Preferences (store-level simple properties exposing important preferences)

    public bool PreferredGuestCheckout { get; set; } = true;
    public bool PreferredLimitDigitalDownloadCount { get; set; } = true;
    public bool PreferredLimitDigitalDownloadDays { get; set; } = true;
    public int PreferredDigitalAssetAuthorizedClicks { get; set; } = 5;
    public int PreferredDigitalAssetAuthorizedDays { get; set; } = 7;

    public string? SupportedLocales { get; set; } // comma separated
    public string? SupportedCurrencies { get; set; } // comma separated

    #endregion

    #region Navigations (lightweight)

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();
    public ICollection<CustomDomain> CustomDomains { get; set; } = new List<CustomDomain>();

    public CustomDomain? DefaultCustomDomain => CustomDomains.FirstOrDefault(d => d.Default);

    public Zone? CheckoutZone { get; set; }
    public Guid? CheckoutZoneId { get; set; }

    #endregion

    private Store() { }

    public static Store Create(string name, string code, string defaultCurrency, Guid defaultCountryId)
        => new Store { Name = name?.Trim() ?? string.Empty, Code = code?.Trim() ?? string.Empty, DefaultCurrency = defaultCurrency, DefaultCountryId = defaultCountryId };

    #region Helpers & mirrors of common Spree behaviour

    // Best-effort: format host-only URL to include scheme and port for local env; infra should supply site settings.
    public string? FormattedUrl(bool isDevelopment = false, string? protocol = null, int? port = null)
    {
        if (string.IsNullOrWhiteSpace(Url)) return null;

        var host = Url!.Trim().Replace("https://", "").Replace("http://", "").Split(':')[0];
        protocol ??= isDevelopment ? "http" : "https";

        if (isDevelopment && port.HasValue)
            return $"{protocol}://{host}:{port.Value}";
        return $"{protocol}://{host}";
    }

    public string? FormattedCustomDomain(bool isDevelopment = false, string? protocol = null, int? port = null)
    {
        if (DefaultCustomDomain == null) return null;
        return isDevelopment
            ? $"{protocol ?? "http"}://{DefaultCustomDomain.Url}{(port.HasValue ? $":{port}" : string.Empty)}"
            : $"https://{DefaultCustomDomain.Url}";
    }

    public string? UrlOrCustomDomain() => DefaultCustomDomain?.Url ?? Url;

    public string? FormattedUrlOrCustomDomain(bool isDevelopment = false, string? protocol = null, int? port = null)
        => FormattedCustomDomain(isDevelopment, protocol, port) ?? FormattedUrl(isDevelopment, protocol, port);

    public bool MetricUnitSystem() => string.Equals(PreferredUnitSystem(), "metric", StringComparison.OrdinalIgnoreCase);

    // preference helper - infra can persist actual preference store
    public string PreferredUnitSystem() => "imperial";

    // Supported locales as list
    public IEnumerable<string> SupportedLocalesList()
    {
        var list = (SupportedLocales ?? string.Empty).Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
        if (!list.Any() && !string.IsNullOrWhiteSpace(DefaultLocale)) list.Add(DefaultLocale);
        return list.Distinct();
    }

    // Supported currencies as list (Money picking left to infra)
    public IEnumerable<string> SupportedCurrenciesList()
    {
        var list = (SupportedCurrencies ?? string.Empty).Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
        if (!list.Any() && !string.IsNullOrWhiteSpace(DefaultCurrency)) list.Add(DefaultCurrency);
        return list.Distinct();
    }

    public bool CanBeDeleted(IEnumerable<Store> otherStores)
        => otherStores != null && otherStores.Any(s => s.Id != Id);

    // Countries available for checkout derived from checkout zone or supplied fallback collection
    public IEnumerable<Country> CountriesAvailableForCheckout(IEnumerable<Country>? allCountries = null)
    {
        var list = CheckoutZone?.CountryIds().Select(cid => allCountries?.FirstOrDefault(c => c.Id == cid)).Where(c => c != null).Cast<Country>()?.ToList();
        if (list != null && list.Any()) return list;
        return allCountries ?? Enumerable.Empty<Country>();
    }

    public IEnumerable<State> StatesAvailableForCheckout(Country country, IEnumerable<State>? allStates = null)
    {
        if (CheckoutZone != null)
        {
            var states = CheckoutZone.StateIds().Select(id => allStates?.FirstOrDefault(s => s.Id == id)).Where(s => s != null).Cast<State>();
            return states.Any() ? states : (allStates?.Where(s => s.CountryId == country.Id) ?? Enumerable.Empty<State>());
        }

        return allStates?.Where(s => s.CountryId == country.Id) ?? Enumerable.Empty<State>();
    }

    //
    // Ported helpers / domain-level implementations of commonly-used callbacks from Spree::Store
    //

    /// <summary>
    /// Ensure only one store is marked as default. Caller should provide other stores (repository-level collection).
    /// If this store is default, clear default flag on others; otherwise ensure at least one store remains default.
    /// </summary>
    public void EnsureDefaultExistsAndIsUnique(IEnumerable<Store> otherStores)
    {
        if (otherStores == null) throw new ArgumentNullException(nameof(otherStores));

        if (Default)
        {
            foreach (var s in otherStores.Where(s => s.Default && s.Id != this.Id))
            {
                s.Default = false;
                s.MarkAsUpdated();
            }
        }
        else
        {
            // if no other default stores are present, make this default
            if (!otherStores.Any(s => s.Default))
            {
                Default = true;
                MarkAsUpdated();
            }
        }

        // emit domain event so infra can clear caches
        AddDomainEvent(new Events.DefaultFlagConsistencyEnsured(Id));
    }

    /// <summary>
    /// Set Url automatically from Code when appropriate.
    /// - rootDomain: e.g. "example.com" (infrastructure should provide)
    /// - codeChanged: whether the repository/application observed the code has changed (prevents overwriting user-provided URL)
    /// - urlChanged: whether the repository/application observed the url has changed
    /// Returns true when Url was set/updated.
    /// </summary>
    public bool SetUrlFromCode(string? rootDomain, bool codeChanged = true, bool urlChanged = false)
    {
        if (string.IsNullOrWhiteSpace(rootDomain)) return false;
        if (urlChanged) return false; // respect explicit url changes

        if (!codeChanged && !string.IsNullOrWhiteSpace(Url)) return false;

        var sanitized = Parameterize(Code);
        if (string.IsNullOrWhiteSpace(sanitized)) return false;

        Url = $"{sanitized}.{rootDomain}";
        MarkAsUpdated();
        AddDomainEvent(new Events.CodeDerivedUrlSet(Id, Url));
        return true;
    }

    /// <summary>
    /// Port of set_code generator logic:
    /// - parameterize existing code or name
    /// - ensure uniqueness using existsPredicate (infra should check DB for conflicts)
    /// The existsPredicate should return true when a candidate code already exists (including soft-deleted if appropriate).
    /// </summary>
    public void SetCode(Func<string, bool> existsPredicate)
    {
        if (existsPredicate == null) throw new ArgumentNullException(nameof(existsPredicate));

        if (!string.IsNullOrWhiteSpace(Code))
        {
            Code = Parameterize(Code);
        }
        else if (!string.IsNullOrWhiteSpace(Name))
        {
            Code = Parameterize(Name);
        }

        if (string.IsNullOrWhiteSpace(Code)) return;

        // Ensure uniqueness by appending random suffix when existsPredicate indicates conflict
        var candidate = Code;
        while (existsPredicate(candidate))
        {
            candidate = $"{Parameterize(Name)}-{Random.Shared.Next(0, 9999):D4}";
        }

        var changed = !string.Equals(Code, candidate, StringComparison.Ordinal);
        Code = candidate;
        if (changed) AddDomainEvent(new Events.CodeChanged(Id, Code));
        MarkAsUpdated();
    }

    private static string Parameterize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var s = input.Trim().ToLowerInvariant();
        // replace spaces and underscores with dash
        s = Regex.Replace(s, @"[\s_]+", "-");
        // remove invalid chars (keep alnum and dash)
        s = Regex.Replace(s, @"[^a-z0-9\-]+", string.Empty);
        s = s.Trim('-');
        return s;
    }

    /// <summary>
    /// Import payment methods by ids from another store. Returns list of StorePaymentMethod join entities to be persisted by infra.
    /// Repository should enforce uniqueness of (StoreId, PaymentMethodId).
    /// </summary>
    public IEnumerable<StorePaymentMethod> ImportPaymentMethodsFromStore(IEnumerable<Guid> paymentMethodIds, IEnumerable<Guid>? existingPaymentMethodIds = null)
    {
        if (paymentMethodIds == null) return Enumerable.Empty<StorePaymentMethod>();

        var existing = new HashSet<Guid>(existingPaymentMethodIds ?? PaymentMethods.Select(pm => pm.Id));
        var result = new List<StorePaymentMethod>();

        foreach (var pmId in paymentMethodIds.Where(id => id != Guid.Empty && !existing.Contains(id)))
        {
            var spm = StorePaymentMethod.Create(Id, pmId);
            result.Add(spm);
        }

        // emit event for infra to perform bulk insert if desired
        if (result.Any())
            AddDomainEvent(new Events.PaymentMethodsImportRequested(Id, result.Select(s => s.PaymentMethodId).ToList()));

        return result;
    }

    /// <summary>
    /// Import products into store by ids. Returns StoreProduct join records to persist.
    /// </summary>
    public IEnumerable<StoreProduct> ImportProductsFromStore(IEnumerable<Guid> productIds, IEnumerable<Guid>? existingProductIds = null)
    {
        if (productIds == null) return Enumerable.Empty<StoreProduct>();

        var existing = new HashSet<Guid>(existingProductIds ?? Enumerable.Empty<Guid>());
        var result = new List<StoreProduct>();

        foreach (var pid in productIds.Where(id => id != Guid.Empty && !existing.Contains(id)))
        {
            var sp = StoreProduct.Create(Id, pid);
            result.Add(sp);
        }

        if (result.Any())
            AddDomainEvent(new Events.ProductsImportRequested(Id, result.Select(p => p.ProductId).ToList()));

        return result;
    }

    /// <summary>
    /// Ensure supported_locales/supported_currencies defaults when attributes exist but are empty.
    /// Application layer should call before saving when appropriate.
    /// </summary>
    public void EnsureSupportedLocalesAndCurrencies()
    {
        if (string.IsNullOrWhiteSpace(SupportedLocales) && !string.IsNullOrWhiteSpace(DefaultLocale))
            SupportedLocales = DefaultLocale;

        if (string.IsNullOrWhiteSpace(SupportedCurrencies) && !string.IsNullOrWhiteSpace(DefaultCurrency))
            SupportedCurrencies = DefaultCurrency;

        MarkAsUpdated();
    }

    /// <summary>
    /// Ensure default country for the store using provided countries collection as lookup.
    /// If checkoutZone has countries, prefer the first of them; otherwise fallback to country with iso 'US' if present in provided collection.
    /// </summary>
    public void EnsureDefaultCountry(IEnumerable<Country>? countries = null)
    {
        // If already set and checkout zone not restrictive, do nothing
        if (DefaultCountry != null && (CheckoutZone == null || CheckoutZone.CountryIds().Any()))
            return;

        if (CheckoutZone != null)
        {
            var firstId = CheckoutZone.CountryIds().FirstOrDefault();
            if (firstId != Guid.Empty)
            {
                var found = countries?.FirstOrDefault(c => c.Id == firstId);
                if (found != null)
                {
                    DefaultCountry = found;
                    DefaultCountryId = found.Id;
                    MarkAsUpdated();
                    return;
                }
            }
        }

        if (countries != null)
        {
            var us = countries.FirstOrDefault(c => string.Equals(c.Iso, "US", StringComparison.OrdinalIgnoreCase));
            if (us != null)
            {
                DefaultCountry = us;
                DefaultCountryId = us.Id;
                MarkAsUpdated();
                return;
            }

            var first = countries.FirstOrDefault();
            if (first != null)
            {
                DefaultCountry = first;
                DefaultCountryId = first.Id;
                MarkAsUpdated();
            }
        }
    }

    /// <summary>
    /// Sanitize Url (remove scheme) — call prior to persisting when mirroring Rails before_validation :set_url behavior.
    /// </summary>
    public void SanitizeUrl()
    {
        if (string.IsNullOrWhiteSpace(Url)) return;
        Url = Regex.Replace(Url!.Trim(), @"^https?://", string.Empty, RegexOptions.IgnoreCase);
        MarkAsUpdated();
    }

    #endregion

    #region Validation / Errors / Events

    public static class Errors
    {
        public static Error NameRequired => Error.Validation("Store.NameRequired", "Store name is required.");
        public static Error UrlRequired => Error.Validation("Store.UrlRequired", "Store URL is required.");
        public static Error CodeRequired => Error.Validation("Store.CodeRequired", "Store code is required.");
        public static Error DefaultCountryRequired => Error.Validation("Store.DefaultCountryRequired", "Default country is required.");
        public static Error NotFound(Guid id) => Error.NotFound("Store.NotFound", $"Store with ID '{id}' was not found.");
    }

    public static List<Error> ValidateModel(string? name, string? url, string? code, Guid defaultCountryId)
    {
        var errors = new List<Error>();
        if (string.IsNullOrWhiteSpace(name)) errors.Add(Errors.NameRequired);
        if (string.IsNullOrWhiteSpace(url)) errors.Add(Errors.UrlRequired);
        else
        {
            if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
                errors.Add(Error.Validation("Store.UrlInvalid", "Store URL is not valid."));
        }
        if (string.IsNullOrWhiteSpace(code)) errors.Add(Errors.CodeRequired);
        if (defaultCountryId == Guid.Empty) errors.Add(Errors.DefaultCountryRequired);
        return errors;
    }

    public static class Events
    {
        public record Created(Guid StoreId) : DomainEvent;
        public record Updated(Guid StoreId) : DomainEvent;
        public record Deleted(Guid StoreId) : DomainEvent;

        // Additional domain events to request infra actions
        public record DefaultFlagConsistencyEnsured(Guid StoreId) : DomainEvent;
        public record CodeChanged(Guid StoreId, string NewCode) : DomainEvent;
        public record CodeDerivedUrlSet(Guid StoreId, string? Url) : DomainEvent;
        public record PaymentMethodsImportRequested(Guid StoreId, IReadOnlyList<Guid> PaymentMethodIds) : DomainEvent;
        public record ProductsImportRequested(Guid StoreId, IReadOnlyList<Guid> ProductIds) : DomainEvent;
    }

    #endregion
}
