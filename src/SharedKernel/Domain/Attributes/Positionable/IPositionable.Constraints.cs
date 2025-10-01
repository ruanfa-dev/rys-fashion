namespace SharedKernel.Domain.Attributes.Positionable;

public static class PositionableConstraints
{
    // Minimum allowed position (normalized to zero-based indexes)
    public const int PositionMin = 0;

    // Practical maximum to avoid absurd values in collections
    public const int PositionMax = 1_000_000;

    // Default starting position when creating new items
    public const int DefaultStartPosition = 0;
}
