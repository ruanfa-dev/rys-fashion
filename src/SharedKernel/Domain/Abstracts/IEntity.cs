using SharedKernel.Domain.Attributes;

namespace SharedKernel.Domain.Abstracts;
public interface IEntity<Id>: IHasId<Id>, IHasDomainEvent
{
}
