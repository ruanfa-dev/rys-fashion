namespace SharedKernel.Domain.Attributes.Assignable;

/// <summary>
/// Lightweight, null-safe helper extensions for <see cref="IAssignable"/>.
/// Note: the interface itself declares <c>MarkAsAssigned</c> — these extensions
/// provide common implementations helpers that implementers can call from their
/// concrete <c>MarkAsAssigned</c> method (or use directly where appropriate).
/// </summary>
public static class IAssignableExtensions
{
    /// <summary>
    /// Core implementation that marks the instance as assigned using UTC now
    /// and the provided <paramref name="userId"/> (trimmed). Safe to call when
    /// <paramref name="target"/> is null (no-op).
    /// Recommended usage inside an implementer:
    ///   public void MarkAsAssigned(string? userId = default) => this.ApplyMarkAsAssigned(userId);
    /// </summary>
    public static void ApplyMarkAsAssigned(this IAssignable? target, string? userId = null)
    {
        if (target is null) return;

        target.AssignedAt = DateTimeOffset.UtcNow;
        target.AssignedBy = NormalizeUserId(userId);
    }

    /// <summary>
    /// Marks as assigned only when not already assigned.
    /// </summary>
    public static void MarkAsAssignedIfBlank(this IAssignable? target, string? userId = null)
    {
        if (target is null) return;
        if (target.AssignedAt.HasValue) return;

        target.ApplyMarkAsAssigned(userId);
    }

    /// <summary>
    /// Unassign the entity (clear assigned metadata).
    /// </summary>
    public static void Unassign(this IAssignable? target)
    {
        if (target is null) return;

        target.AssignedAt = null;
        target.AssignedBy = null;
    }

    /// <summary>
    /// Returns true when the entity is assigned.
    /// </summary>
    public static bool IsAssigned(this IAssignable? target) =>
        target is not null && target.AssignedAt.HasValue;

    /// <summary>
    /// Returns the assigned info tuple (AssignedAt, AssignedBy). Safe for null target.
    /// </summary>
    public static (DateTimeOffset? AssignedAt, string? AssignedBy) GetAssignedInfo(this IAssignable? target) =>
        target is null ? (null, null) : (target.AssignedAt, target.AssignedBy);

    private static string? NormalizeUserId(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;
        return userId.Trim();
    }
}