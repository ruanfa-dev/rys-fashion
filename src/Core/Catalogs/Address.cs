using System.Text.RegularExpressions;

using Core.Identity;

using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Simplified domain model converted from Spree::Address (ruby).
/// Persistence, advanced validation (zipcode formats), geocoding and background jobs belong to infra layer.
/// </summary>
public sealed class Address : AuditableEntity
{
    // Countries that do not use postal codes (best-effort list from Spree)
    private static readonly HashSet<string> NO_ZIPCODE_ISO_CODES = new(StringComparer.OrdinalIgnoreCase)
    {
        "AO","AG","AW","BS","BZ","BJ","BM","BO","BW","BF","BI","CM","CF","KM","CG",
        "CD","CK","CUW","CI","DJ","DM","GQ","ER","FJ","TF","GAB","GM","GH","GD","GN",
        "GY","HK","IE","KI","KP","LY","MO","MW","ML","MR","NR","AN","NU","KP","PA",
        "QA","RW","KN","LC","ST","SC","SL","SB","SO","SR","SY","TZ","TL","TK","TG",
        "TO","TV","UG","AE","VU","YE","ZW"
    };

    // Countries that require a state by default for certain validations (best-effort)
    private static readonly HashSet<string> STATES_REQUIRED = new(StringComparer.OrdinalIgnoreCase)
    {
        "AU","AE","BR","CA","CN","ES","HK","IE","IN","IT","MY","MX","NZ","PT","RO","TH","US","ZA"
    };

    // Core fields
    public string? Firstname { get; set; }
    public string? Lastname { get; set; }
    public string? Company { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? StateName { get; set; }
    public Guid? StateId { get; set; }
    public Guid CountryId { get; set; }
    public string? Zipcode { get; set; }
    public string? Phone { get; set; }
    public string? AlternativePhone { get; set; }

    // Flags / metadata
    public bool QuickCheckout { get; set; }
    public string? Label { get; set; }

    // Serialized stores approximations
    public IDictionary<string, string?> Preferences { get; set; } = new Dictionary<string, string?>();
    public IDictionary<string, string?> PublicMetadata { get; set; } = new Dictionary<string, string?>();
    public IDictionary<string, string?> PrivateMetadata { get; set; } = new Dictionary<string, string?>();

    // Soft-delete
    public DateTimeOffset? DeletedAt { get; set; }

    // Relationships
    public Country? Country { get; set; }
    public State? State { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();

    private Address() { }

    public static Address Create(
        Guid countryId,
        string? firstname = null,
        string? lastname = null,
        string? address1 = null,
        string? city = null,
        string? zipcode = null)
        => new Address
        {
            CountryId = countryId,
            Firstname = firstname?.Trim(),
            Lastname = lastname?.Trim(),
            Address1 = address1?.Trim(),
            City = city?.Trim(),
            Zipcode = zipcode?.Trim()
        };

    // Convenience accessors
    public string FirstName
    {
        get => Firstname ?? string.Empty;
        set => Firstname = value?.Trim();
    }

    public string LastName
    {
        get => Lastname ?? string.Empty;
        set => Lastname = value?.Trim();
    }

    public string FullName() => string.Join(" ", new[] { Firstname, Lastname }.Where(s => !string.IsNullOrWhiteSpace(s)));

    public string Street() => string.Join(" ", new[] { Address1, Address2 }.Where(s => !string.IsNullOrWhiteSpace(s)));

    public IDictionary<string, object?> ActiveMerchantHash()
    {
        return new Dictionary<string, object?>
        {
            ["name"] = FullName(),
            ["address1"] = Address1,
            ["address2"] = Address2,
            ["city"] = City,
            ["state"] = StateText(),
            ["zip"] = Zipcode,
            ["country"] = Country?.Iso,
            ["phone"] = Phone
        };
    }

    public string StateText()
        => State?.Abbr ?? State?.Name ?? StateName ?? string.Empty;

    public bool IsEmpty()
    {
        // treat country as required in domain, so ignore country in emptiness check
        return new[] { Firstname, Lastname, Company, Address1, Address2, City, StateName, Zipcode, Phone }
            .All(s => string.IsNullOrWhiteSpace(s));
    }

    public Address Clone()
    {
        return new Address
        {
            Firstname = Firstname,
            Lastname = Lastname,
            Company = Company,
            Address1 = Address1,
            Address2 = Address2,
            City = City,
            StateId = StateId,
            StateName = StateName,
            CountryId = CountryId,
            Zipcode = Zipcode,
            Phone = Phone,
            AlternativePhone = AlternativePhone,
            QuickCheckout = QuickCheckout,
            Label = Label,
            Preferences = new Dictionary<string, string?>(Preferences),
            PublicMetadata = new Dictionary<string, string?>(PublicMetadata),
            PrivateMetadata = new Dictionary<string, string?>(PrivateMetadata)
        };
    }

    public override string ToString()
    {
        // simple textual rendering (no HTML escaping here)
        var parts = new[]
        {
            FullName(),
            Company,
            Address1,
            Address2,
            string.IsNullOrWhiteSpace(City) ? null : $"{City}, {StateText()} {Zipcode}",
            Country?.ToString()
        }.Where(s => !string.IsNullOrWhiteSpace(s));
        return string.Join(Environment.NewLine, parts);
    }

    public IDictionary<string, object?> ValueAttributes()
    {
        // Return attributes used to compare addresses (exclude ids, timestamps, etc.)
        return new Dictionary<string, object?>
        {
            ["firstname"] = Firstname,
            ["lastname"] = Lastname,
            ["company"] = Company,
            ["address1"] = Address1,
            ["address2"] = Address2,
            ["city"] = City,
            ["state_id"] = StateId,
            ["state_name"] = StateName,
            ["country_id"] = CountryId,
            ["zipcode"] = Zipcode,
            ["phone"] = Phone,
            ["alternative_phone"] = AlternativePhone
        };
    }

    public bool ValueEquals(Address? other)
    {
        if (other == null) return false;
        var a = ValueAttributes();
        var b = other.ValueAttributes();
        return a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var v) && Equals(kv.Value, v));
    }

    // -- basic normalization helpers (mirrors remove_emoji_and_normalize / auto_strip_attributes)
    public void NormalizeFields()
    {
        foreach (var field in new[] { nameof(Firstname), nameof(Lastname), nameof(Company), nameof(Address1), nameof(Address2), nameof(City), nameof(Zipcode), nameof(Phone), nameof(AlternativePhone) })
        {
            var prop = GetType().GetProperty(field);
            if (prop == null) continue;
            var val = prop.GetValue(this) as string;
            if (val == null) continue;
            var normalized = NormalizeString(val);
            prop.SetValue(this, normalized);
        }
    }

    private static string NormalizeString(string input)
    {
        // remove emoji (best-effort) and trim; keep this simple to avoid heavy deps
        // remove non-printable control chars and surrogate emojis range
        var cleaned = Regex.Replace(input, @"\p{Cs}+", string.Empty); // surrogate chars
        cleaned = Regex.Replace(cleaned, @"[^\u0020-\u007E\u00A0-\u00FF\p{L}\p{N}\s\p{P}]+", string.Empty);
        return cleaned.Trim();
    }

    /// <summary>
    /// Clear state if it doesn't belong to the country or if the country's configuration doesn't require states.
    /// Infra/repositories are expected to populate Country.States when available.
    /// </summary>
    public void ClearInvalidStateEntities()
    {
        if (State != null && Country != null && State.CountryId != Country.Id)
        {
            State = null;
            StateId = null;
        }

        if (!string.IsNullOrWhiteSpace(StateName) && Country != null && (Country.States == null || !Country.States.Any()))
        {
            StateName = null;
        }
    }

    #region Postal code helpers

    /// <summary>
    /// Best-effort postal code validation:
    /// - If country has no postal codes (static list) treat as valid.
    /// - Otherwise perform a lightweight pattern check (alphanumeric, spaces, dashes).
    /// Infra should plug a richer validator (CLDR-based) if required.
    /// </summary>
    public bool PostalCodeValid()
    {
        if (Country == null || string.IsNullOrWhiteSpace(Country.Iso)) return true;
        var iso = Country.Iso.Trim().ToUpperInvariant();
        if (NO_ZIPCODE_ISO_CODES.Contains(iso)) return true;
        if (string.IsNullOrWhiteSpace(Zipcode)) return false;

        var z = Zipcode.Trim();
        // allow letters, digits, spaces, dash, dot, pound sign
        return Regex.IsMatch(z, @"^[A-Za-z0-9\-\s\.\#]+$");
    }

    /// <summary>
    /// Whether the country requires a state value for stronger validation rules.
    /// </summary>
    public bool CountryRequiresState()
    {
        if (Country == null || string.IsNullOrWhiteSpace(Country.Iso)) return false;
        return STATES_REQUIRED.Contains(Country.Iso.Trim().ToUpperInvariant());
    }

    #endregion

    #region Validation / Constraints / Errors

    public static class Constraints
    {
        public const int MaxFieldLength = 255;
    }

    public static class Errors
    {
        public static Error FirstnameRequired => Error.Validation("Address.FirstnameRequired", "Firstname is required.");
        public static Error LastnameRequired => Error.Validation("Address.LastnameRequired", "Lastname is required.");
        public static Error Address1Required => Error.Validation("Address.Address1Required", "Address1 is required.");
        public static Error CityRequired => Error.Validation("Address.CityRequired", "City is required.");
        public static Error CountryRequired => Error.Validation("Address.CountryRequired", "Country is required.");
        public static Error ZipcodeRequired => Error.Validation("Address.ZipcodeRequired", "Zipcode is required.");
        public static Error PhoneRequired => Error.Validation("Address.PhoneRequired", "Phone is required.");
        public static Error InvalidZipcode => Error.Validation("Address.InvalidZipcode", "Zipcode format appears invalid.");
        public static Error NotFound(Guid id) => Error.NotFound("Address.NotFound", $"Address with ID '{id}' was not found.");
    }

    /// <summary>
    /// Lightweight validation. Caller controls which presence rules apply (mirrors Spree config flags).
    /// </summary>
    public List<Error> Validate(
        bool requireName = true,
        bool requireStreet = true,
        bool requireCity = true,
        bool requireCountry = true,
        bool requireZipcode = true,
        bool requirePhone = false)
    {
        var errors = new List<Error>();

        if (requireName)
        {
            if (string.IsNullOrWhiteSpace(Firstname)) errors.Add(Errors.FirstnameRequired);
            if (string.IsNullOrWhiteSpace(Lastname)) errors.Add(Errors.LastnameRequired);
        }

        if (requireStreet && string.IsNullOrWhiteSpace(Address1)) errors.Add(Errors.Address1Required);
        if (requireCity && string.IsNullOrWhiteSpace(City)) errors.Add(Errors.CityRequired);
        if (requireCountry && CountryId == Guid.Empty) errors.Add(Errors.CountryRequired);
        if (requireZipcode)
        {
            if (string.IsNullOrWhiteSpace(Zipcode)) errors.Add(Errors.ZipcodeRequired);
            else if (!PostalCodeValid()) errors.Add(Errors.InvalidZipcode);
        }
        if (requirePhone && string.IsNullOrWhiteSpace(Phone)) errors.Add(Errors.PhoneRequired);

        return errors;
    }

    #endregion

    #region Async geocoding support

    /// <summary>
    /// Domain event producer to request async geocoding by infra when appropriate.
    /// Application layer should call this after commit (or wire an event handler).
    /// </summary>
    public void AsyncGeocode()
    {
        // Emit domain event; infra decides whether to enqueue geocoding job.
        AddDomainEvent(new Events.GeocodeRequested(Id));
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid AddressId) : DomainEvent;
        public record Updated(Guid AddressId) : DomainEvent;
        public record Deleted(Guid AddressId) : DomainEvent;

        // Raised when infrastructure should geocode this address (e.g. background job)
        public record GeocodeRequested(Guid AddressId) : DomainEvent;
    }

    #endregion
}