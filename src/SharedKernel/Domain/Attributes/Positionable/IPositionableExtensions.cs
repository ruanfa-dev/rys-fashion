namespace SharedKernel.Domain.Attributes.Positionable;

/// <summary>
/// Null-safe helpers for <see cref="IPositionable"/>.
/// </summary>
public static class IPositionableExtensions
{
    /// <summary>
    /// Sets the position safely, clamping to >= 0.
    /// </summary>
    public static void ApplyPosition(this IPositionable? target, int newPosition)
    {
        if (target is null) return;
        target.Position = Math.Max(0, newPosition);
    }

    /// <summary>
    /// Moves the entity one step up (lower index).
    /// </summary>
    public static void MoveUp(this IPositionable? target)
    {
        if (target is null) return;
        target.Position = Math.Max(0, target.Position - 1);
    }

    /// <summary>
    /// Moves the entity one step down (higher index).
    /// </summary>
    public static void MoveDown(this IPositionable? target)
    {
        if (target is null) return;
        target.Position += 1;
    }

    /// <summary>
    /// Re-orders a collection of positionable entities and
    /// reassigns normalized positions (0..n).
    /// </summary>
    public static void NormalizePositions<T>(this IEnumerable<T>? items)
        where T : IPositionable
    {
        if (items is null) return;

        var ordered = items.OrderBy(i => i.Position).ToList();
        for (var i = 0; i < ordered.Count; i++)
            ordered[i].Position = i;
    }
}