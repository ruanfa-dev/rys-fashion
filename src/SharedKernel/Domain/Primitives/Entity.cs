using SharedKernel.Domain.Abstracts;
using SharedKernel.Domain.Attributes;
using SharedKernel.Messaging.Abstracts;

namespace SharedKernel.Domain.Primitives;

/// <summary>
/// Base entity class with ID and domain events support, without auditing capabilities
/// </summary>
public abstract class Entity<TId> : IEntity<TId>, IEquatable<Entity<TId>>, IHasDomainEvent
    where TId : notnull
{
    #region Properties
    public TId Id { get; set; }
    
    private readonly List<IDomainEvent> _domainEvents = [];
    protected IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    #endregion

    #region Constructors
    public Entity()
    {
        Id = default!;
    }

    public Entity(TId id)
    {
        Id = id;
    }
    #endregion

    #region Methods
    public virtual bool IsTransient()
    {
        return EqualityComparer<TId>.Default.Equals(Id, default);
    }

    #region Domain Events
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public IReadOnlyCollection<IDomainEvent> GetDomainEvents()
    {
        return _domainEvents.AsReadOnly();
    }
    #endregion

    #region IEquatable Implementation
    public virtual bool Equals(Entity<TId>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        
        if (IsTransient() || other.IsTransient())
            return false;
        
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Entity<TId>);
    }

    public override int GetHashCode()
    {
        if (IsTransient())
            return base.GetHashCode();
        
        return EqualityComparer<TId>.Default.GetHashCode(Id);
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !Equals(left, right);
    }
    #endregion
    #endregion
}

/// <summary>
/// Base entity class with Guid ID and domain events support, without auditing capabilities
/// </summary>
public abstract class Entity : Entity<Guid>
{
    public Entity() : base()
    {
        Id = Guid.NewGuid();
    }

    public Entity(Guid id) : base(id)
    {
    }
}

