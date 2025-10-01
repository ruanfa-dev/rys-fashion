using SharedKernel.Domain.Attributes.Auditable;

namespace SharedKernel.Domain.Primitives;

/// <summary>
/// Base auditable entity class with ID, domain events, and auditing capabilities
/// </summary>
public abstract class AuditableEntity<TId> : Entity<TId>, IAuditable
    where TId : notnull
{
    #region Auditable Properties
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    #endregion

    #region Constructors

    protected AuditableEntity() : base()
    {
        this.ApplyMarkAsCreated();
    }

    protected AuditableEntity(TId id) : base(id)
    {
        this.ApplyMarkAsCreated();
    }
    #endregion
}

/// <summary>
/// Base auditable entity class with Guid ID, domain events, and auditing capabilities
/// </summary>
public abstract class AuditableEntity : AuditableEntity<Guid>
{
    protected AuditableEntity() : base()
    {
        Id = Guid.NewGuid();
    }

    protected AuditableEntity(Guid id) : base(id)
    {
    }
}
