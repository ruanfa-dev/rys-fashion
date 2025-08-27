using SharedKernel.Messaging;

namespace Core.Todos;

public sealed record TodoItemCompletedEvent(TodoItem Item) : DomainEvent;
public sealed record TodoItemCreatedEvent(TodoItem Item) : DomainEvent;
public sealed record TodoItemUpdatedEvent(TodoItem Item) : DomainEvent;
public sealed record TodoItemDeletedEvent(TodoItem Item) : DomainEvent;
