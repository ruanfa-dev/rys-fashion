namespace SharedKernel.Domain.Attributes.Status;

/// <summary>
/// Contract for entities that carry a status field.
/// </summary>
public interface IHasStatus<TStatus>
    where TStatus : struct, Enum
{
    /// <summary>
    /// Current status of the entity.
    /// </summary>
    TStatus Status { get; set; }
}
