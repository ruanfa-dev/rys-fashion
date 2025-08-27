using SharedKernel.Messaging.Abstracts;

namespace SharedKernel.Domain.Attributes;

public interface IHasDomainEvent
{
    void AddDomainEvent(IDomainEvent domainEvent);
    void RemoveDomainEvent(IDomainEvent domainEvent);
    void ClearDomainEvents();
    IReadOnlyCollection<IDomainEvent> GetDomainEvents();
}
