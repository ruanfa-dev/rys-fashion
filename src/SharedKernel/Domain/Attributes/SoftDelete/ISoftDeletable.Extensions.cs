namespace SharedKernel.Domain.Attributes.SoftDelete;

/// <summary>
/// Lightweight, null-safe helper extensions for <see cref="ISoftDeletable"/>.
/// These provide default implementations similar to how IAuditable works.
/// </summary>
public static class ISoftDeletableExtensions
{
    /// <summary>
    /// Core implementation that marks as deleted using UTC now
    /// and the provided userId (trimmed). Safe to call when target is null.
    /// </summary>
    public static void ApplyMarkAsDeleted(this ISoftDeletable? target, string? userId = null)
    {
        if (target is null) return;

        target.DeletedAt = DateTimeOffset.UtcNow;
        target.DeletedBy = NormalizeUserId(userId);
    }

    /// <summary>
    /// Marks as deleted only if not already deleted.
    /// </summary>
    public static void MarkAsDeletedIfNot(this ISoftDeletable? target, string? userId = null)
    {
        if (target is null) return;
        if (target.DeletedAt.HasValue) return;

        target.ApplyMarkAsDeleted(userId);
    }

    /// <summary>
    /// Restore the entity by clearing delete markers.
    /// </summary>
    public static void ApplyRestore(this ISoftDeletable? target)
    {
        if (target is null) return;

        target.DeletedAt = null;
        target.DeletedBy = null;
    }

    /// <summary>
    /// Returns true if the entity is marked as deleted.
    /// </summary>
    public static bool IsDeleted(this ISoftDeletable? target) =>
        target is not null && target.DeletedAt.HasValue;

    /// <summary>
    /// Returns a tuple of soft-delete metadata.
    /// </summary>
    public static (DateTimeOffset? DeletedAt, string? DeletedBy) GetDeleteInfo(this ISoftDeletable? target) =>
        target is null ? (null, null) : (target.DeletedAt, target.DeletedBy);

    private static string? NormalizeUserId(string? userId) =>
        string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
}