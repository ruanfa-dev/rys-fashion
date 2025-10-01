namespace SharedKernel.Domain.Attributes.SoftDelete;

/// <summary>
/// Contract for entities that support soft deletion.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// When the entity was soft-deleted. Null means not deleted.
    /// </summary>
    DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// Who deleted the entity (optional).
    /// </summary>
    string? DeletedBy { get; set; }

    /// <summary>
    /// Restore a previously deleted entity.
    /// </summary>
    void Restore();
}
