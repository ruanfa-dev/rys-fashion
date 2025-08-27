using SharedKernel.Domain.Attributes;

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
    public AuditableEntity() : base()
    {
        MarkAsCreated();
    }

    public AuditableEntity(TId id) : base(id)
    {
        MarkAsCreated();
    }
    #endregion

    #region Auditable Methods
    public void MarkAsCreated(string? userId = null)
    {
        CreatedAt = DateTimeOffset.UtcNow;
        CreatedBy = userId;
    }

    public void MarkAsUpdated(string? userId = null)
    {
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = userId;
    }
    #endregion
}

/// <summary>
/// Base auditable entity class with Guid ID, domain events, and auditing capabilities
/// </summary>
public abstract class AuditableEntity : AuditableEntity<Guid>
{
    public AuditableEntity() : base()
    {
        Id = Guid.NewGuid();
    }

    public AuditableEntity(Guid id) : base(id)
    {
    }
}
