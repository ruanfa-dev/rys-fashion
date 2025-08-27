using MediatR;

namespace SharedKernel.Messaging.Abstracts;

public interface IDomainEvent : INotification
{
    public Guid Id { get; }
    public DateTimeOffset? OccurredOn { get; init; }
    public string EventType { get; }
}

public interface IDomainEventHandler<in TDomainEvent> : INotificationHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent;