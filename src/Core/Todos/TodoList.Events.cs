using Core.Todos;

using SharedKernel.Messaging;

namespace Core.Todos;

public record TodoListCreatedEvent(TodoList TodoList) : DomainEvent;
public record TodoListUpdatedEvent(TodoList TodoList) : DomainEvent;
public record TodoListDeletedEvent(TodoList TodoList) : DomainEvent;