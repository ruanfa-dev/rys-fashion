namespace SharedKernel.Domain.Attributes.Auditable;

/// <summary>
/// Lightweight, null-safe helper extensions for <see cref="IAuditable"/>.
/// These provide common implementation helpers that implementers can call from
/// their concrete `MarkAsCreated` / `MarkAsUpdated` methods (or use directly
/// where appropriate).
/// </summary>
public static class IAuditableExtensions
{
    /// <summary>
    /// Core implementation that marks the instance as created using UTC now
    /// and the provided <paramref name="userId"/> (trimmed). Safe to call when
    /// <paramref name="target"/> is null (no-op).
    /// Recommended usage inside an implementer:
    ///   public void MarkAsCreated(string? userId = default) => this.ApplyMarkAsCreated(userId);
    /// </summary>
    public static void ApplyMarkAsCreated(this IAuditable? target, string? userId = null)
    {
        if (target is null) return;

        target.CreatedAt = DateTimeOffset.UtcNow;
        target.CreatedBy = NormalizeUserId(userId);
    }

    /// <summary>
    /// Marks as created only when CreatedAt is still the default value.
    /// </summary>
    public static void MarkAsCreatedIfBlank(this IAuditable? target, string? userId = null)
    {
        if (target is null) return;
        if (target.CreatedAt != default) return;

        target.ApplyMarkAsCreated(userId);
    }

    /// <summary>
    /// Core implementation that marks the instance as updated using UTC now
    /// and the provided <paramref name="userId"/> (trimmed). Safe to call when
    /// <paramref name="target"/> is null (no-op).
    /// Recommended usage inside an implementer:
    ///   public void MarkAsUpdated(string? userId = default) => this.ApplyMarkAsUpdated(userId);
    /// </summary>
    public static void ApplyMarkAsUpdated(this IAuditable? target, string? userId = null)
    {
        if (target is null) return;

        target.UpdatedAt = DateTimeOffset.UtcNow;
        target.UpdatedBy = NormalizeUserId(userId);
    }

    /// <summary>
    /// Marks as updated only when not already updated.
    /// </summary>
    public static void MarkAsUpdatedIfBlank(this IAuditable? target, string? userId = null)
    {
        if (target is null) return;
        if (target.UpdatedAt.HasValue) return;

        target.ApplyMarkAsUpdated(userId);
    }

    /// <summary>
    /// Returns true when the entity has an update timestamp.
    /// </summary>
    public static bool IsUpdated(this IAuditable? target) =>
        target is not null && target.UpdatedAt.HasValue;

    /// <summary>
    /// Returns the audit info tuple (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy). Safe for null target.
    /// </summary>
    public static (DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt, string? CreatedBy, string? UpdatedBy) GetAuditInfo(this IAuditable? target) =>
        target is null ? (default, null, null, null) : (target.CreatedAt, target.UpdatedAt, target.CreatedBy, target.UpdatedBy);

    private static string? NormalizeUserId(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;
        return userId.Trim();
    }
}
