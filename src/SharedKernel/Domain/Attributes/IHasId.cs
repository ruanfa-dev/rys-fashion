namespace SharedKernel.Domain.Attributes;
public interface IHasId<TId>
{
    /// <summary>
    /// Gets the unique identifier of the entity.
    /// </summary>
    TId Id { get; set; }
    /// <summary>
    /// Checks if the entity is transient (not yet persisted).
    /// </summary>
    bool IsTransient();
}
