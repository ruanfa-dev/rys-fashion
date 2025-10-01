namespace SharedKernel.Domain.Attributes.Positionable;

/// <summary>
/// Contract for entities that can be positioned within a collection.
/// Commonly used to represent ordered lists (menus, options, steps, etc.).
/// </summary>
public interface IPositionable
{
    /// <summary>
    /// Position of the entity within its collection.
    /// Usually starts at 0 or 1 depending on domain convention.
    /// </summary>
    int Position { get; set; }
}