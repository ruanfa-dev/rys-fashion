using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

public sealed class Price : AuditableEntity
{
    public const decimal MAXIMUM_AMOUNT = 99_999_999.99m;

    #region Properties

    public decimal? Amount { get; set; }
    public decimal? CompareAtAmount { get; set; }
    public string Currency { get; set; } = default!;

    public Guid VariantId { get; set; }
    public Variant Variant { get; set; } = default!;

    /// <summary>
    /// Helper flag used by application layer to decide whether price change should trigger taxon matching jobs.
    /// Mirrors Spree's attribute :eligible_for_taxon_matching
    /// </summary>
    public bool EligibleForTaxonMatching { get; set; }

    #endregion

    #region Factory / Update

    private Price() { }

    public static Price Create(Guid variantId, decimal? amount, string currency, decimal? compareAtAmount = null)
    {
        return new Price
        {
            VariantId = variantId,
            Amount = amount,
            Currency = currency,
            CompareAtAmount = compareAtAmount
        };
    }

    public void Update(decimal? amount = null, string? currency = null, decimal? compareAtAmount = null)
    {
        if (amount.HasValue) Amount = amount;
        if (!string.IsNullOrWhiteSpace(currency)) Currency = currency!;
        CompareAtAmount = compareAtAmount;
    }

    #endregion

    #region Helpers

    public bool Discounted => CompareAtAmount.HasValue && CompareAtAmount.Value > (Amount ?? 0m);

    public static bool WasDiscounted(decimal? previousCompareAtAmount, decimal? previousAmount)
    {
        if (!previousCompareAtAmount.HasValue) return false;
        var prevAmt = previousAmount ?? 0m;
        return previousCompareAtAmount.Value > prevAmt;
    }

    /// <summary>
    /// Ensures currency is set (application should pass store default when available).
    /// </summary>
    public void EnsureCurrency(string defaultCurrency)
    {
        if (string.IsNullOrWhiteSpace(Currency))
            Currency = defaultCurrency;
    }

    /// <summary>
    /// Remove compare_at amount if it equals amount — mirrors Spree callback.
    /// </summary>
    public void RemoveCompareAtAmountIfEqualsAmount()
    {
        if (CompareAtAmount.HasValue && Amount.HasValue && CompareAtAmount.Value == Amount.Value)
            CompareAtAmount = null;
    }

    public string Name() => $"{Variant?.Name ?? "variant"} - {Currency?.ToUpperInvariant()}";

    #endregion

    #region Validation

    public static List<Error> ValidateModel(decimal? amount, decimal? compareAtAmount, string? currency, bool allowEmptyPriceAmount = false)
    {
        var errors = new List<Error>();

        if (!allowEmptyPriceAmount)
        {
            if (!amount.HasValue)
                errors.Add(Errors.AmountRequired);
        }

        if (amount.HasValue)
        {
            if (amount < 0 || amount > MAXIMUM_AMOUNT)
                errors.Add(Errors.InvalidAmountRange);
        }

        if (compareAtAmount.HasValue)
        {
            if (compareAtAmount < 0 || compareAtAmount > MAXIMUM_AMOUNT)
                errors.Add(Errors.InvalidCompareAtAmountRange);
        }

        if (string.IsNullOrWhiteSpace(currency))
            errors.Add(Errors.CurrencyRequired);

        return errors;
    }

    public static class Errors
    {
        public static Error AmountRequired => Error.Validation("Price.AmountRequired", "Price amount is required.");
        public static Error InvalidAmountRange => Error.Validation("Price.InvalidAmountRange", $"Amount must be between 0 and {MAXIMUM_AMOUNT:N2}.");
        public static Error InvalidCompareAtAmountRange => Error.Validation("Price.InvalidCompareAtAmountRange", $"Compare at amount must be between 0 and {MAXIMUM_AMOUNT:N2}.");
        public static Error CurrencyRequired => Error.Validation("Price.CurrencyRequired", "Currency is required.");
        public static Error NotFound(Guid id) => Error.NotFound("Price.NotFound", $"Price with ID '{id}' not found.");
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid PriceId) : DomainEvent;
        public record Updated(Guid PriceId) : DomainEvent;
        public record Deleted(Guid PriceId) : DomainEvent;
    }

    #endregion
}