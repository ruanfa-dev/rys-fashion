using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Domain port of Spree::CustomDomain (simplified).
/// - Persistence (uniqueness, DB constraints), callbacks and side-effects belong to infra/application layers.
/// - Call SanitizeUrl() before persisting to mirror before_validation callback.
/// - Use EnsureDefault/EnsureHasOneDefault from application code when creating/updating to keep invariants.
/// </summary>
public sealed class CustomDomain : AuditableEntity
{
    private static readonly Regex DomainRegex = new(
        @"\A(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\z",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public Guid StoreId { get; set; }
    public Store? Store { get; set; }

    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// If true this domain is the default custom domain for the store.
    /// </summary>
    public bool Default { get; set; }

    private CustomDomain() { }

    public static CustomDomain Create(Guid storeId, string url, bool isDefault = false)
    {
        var cd = new CustomDomain
        {
            StoreId = storeId,
            Url = url?.Trim() ?? string.Empty,
            Default = isDefault
        };
        cd.SanitizeUrl();
        return cd;
    }

    public void Update(string? url = null, bool? isDefault = null)
    {
        if (url != null) Url = url.Trim();
        if (isDefault.HasValue) Default = isDefault.Value;
        SanitizeUrl();
        MarkAsUpdated();
    }

    /// <summary>
    /// Removes any leading http(s):// from Url. Call before validation/persistence.
    /// </summary>
    public void SanitizeUrl()
    {
        if (string.IsNullOrWhiteSpace(Url)) return;
        Url = Regex.Replace(Url, @"^https?://", string.Empty, RegexOptions.IgnoreCase).Trim();
    }

    /// <summary>
    /// Best-effort check that domain looks like a valid host/subdomain (mirrors ruby validation).
    /// Application should still rely on repository uniqueness and length constraints.
    /// </summary>
    public bool UrlLooksValid()
    {
        if (string.IsNullOrWhiteSpace(Url)) return false;

        var parts = Url.Split('.');
        if (parts.Length < 2 || parts.Length > 4) return false;

        return DomainRegex.IsMatch(Url);
    }

    /// <summary>
    /// Convenience accessor used by UI code.
    /// </summary>
    public string Name() => Url;

    /// <summary>
    /// Set default flag to true when there are no existing domains for the store.
    /// Application should call with the store's current domains (excluding this instance if creating).
    /// </summary>
    public void EnsureDefault(IEnumerable<CustomDomain> existingStoreDomains)
    {
        if (existingStoreDomains == null) throw new ArgumentNullException(nameof(existingStoreDomains));
        if (!existingStoreDomains.Any()) Default = true;
    }

    /// <summary>
    /// Ensure only this domain is default by clearing Default on other domains.
    /// Application should persist changes to other domains after calling this method.
    /// </summary>
    public void EnsureHasOneDefault(IEnumerable<CustomDomain> otherDomains)
    {
        if (otherDomains == null) throw new ArgumentNullException(nameof(otherDomains));
        if (!Default) return;

        foreach (var d in otherDomains.Where(d => d.Id != this.Id && d.Default))
        {
            d.Default = false;
            d.MarkAsUpdated();
        }
    }

    public bool Active() => true;

    #region Validation / Errors

    public static class Errors
    {
        public static Error UrlRequired => Error.Validation("CustomDomain.UrlRequired", "URL is required.");
        public static Error UrlInvalid => Error.Validation("CustomDomain.UrlInvalid", "URL is not a valid domain.");
        public static Error UrlTooShortOrLong => Error.Validation("CustomDomain.UrlLength", "URL must be between 1 and 63 characters.");
        public static Error StoreRequired => Error.Validation("CustomDomain.StoreRequired", "Store is required.");
        public static Error NotFound(Guid id) => Error.NotFound("CustomDomain.NotFound", $"CustomDomain with ID '{id}' was not found.");
    }

    /// <summary>
    /// Lightweight validation mirroring model-level rules. Uniqueness must be enforced by repository.
    /// </summary>
    public static List<Error> ValidateModel(string? url, Guid storeId)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(url))
        {
            errors.Add(Errors.UrlRequired);
            return errors;
        }

        var trimmed = url.Trim();
        if (trimmed.Length < 1 || trimmed.Length > 63)
        {
            errors.Add(Errors.UrlTooShortOrLong);
        }

        // sanitize candidate (remove scheme)
        var candidate = Regex.Replace(trimmed, @"^https?://", string.Empty, RegexOptions.IgnoreCase);

        var parts = candidate.Split('.');
        if (parts.Length < 2 || parts.Length > 4 || !DomainRegex.IsMatch(candidate))
        {
            errors.Add(Errors.UrlInvalid);
        }

        if (storeId == Guid.Empty) errors.Add(Errors.StoreRequired);

        return errors;
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid CustomDomainId) : DomainEvent;
        public record Updated(Guid CustomDomainId) : DomainEvent;
        public record Deleted(Guid CustomDomainId) : DomainEvent;

        /// <summary>
        /// Raised when infrastructure should clear default flags on other domains for the given store.
        /// Handlers may accept (StoreId, CustomDomainId) payload.
        /// </summary>
        public record EnsureSingleDefaultRequested(Guid StoreId, Guid CustomDomainId) : DomainEvent;
    }

    #endregion
}