using SharedKernel.Messaging.Abstracts;

namespace SharedKernel.Messaging;

public abstract record class DomainEvent : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTimeOffset? OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    public string EventType => GetType().Name;
}