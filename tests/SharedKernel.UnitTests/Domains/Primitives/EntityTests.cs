using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging.Abstracts;

using Shouldly;

namespace SharedKernel.UnitTests.Domains.Primitives;

/// <summary>
/// Unit tests for Entity base class functionality including domain events, equality, and state management.
/// </summary>
public sealed class EntityTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region Test Implementations

    private sealed class TestEntity : Entity<int>
    {
        public string Name { get; set; } = string.Empty;

        public TestEntity() : base() { }
        public TestEntity(int id) : base(id) { }
        public TestEntity(int id, string name) : base(id)
        {
            Name = name;
        }
    }

    private sealed class TestGuidEntity : Entity
    {
        public string Name { get; set; } = string.Empty;

        public TestGuidEntity() : base() { }
        public TestGuidEntity(Guid id) : base(id) { }
        public TestGuidEntity(string name) : base()
        {
            Name = name;
        }
    }

    private sealed class TestDomainEvent : IDomainEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTimeOffset? OccurredOn { get; init; } = DateTimeOffset.UtcNow;
        public string EventType { get; } = nameof(TestDomainEvent);
    }

    private sealed class AnotherTestDomainEvent : IDomainEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTimeOffset? OccurredOn { get; init; } = DateTimeOffset.UtcNow;
        public string EventType { get; } = nameof(AnotherTestDomainEvent);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_Default_CreatesEntityWithDefaultId()
    {
        // Act
        var entity = new TestEntity();

        // Assert
        entity.Id.ShouldBe(0);
        entity.IsTransient().ShouldBeTrue();
        entity.GetDomainEvents().ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_WithId_CreatesEntityWithSpecifiedId()
    {
        // Arrange
        const int expectedId = 123;

        // Act
        var entity = new TestEntity(expectedId);

        // Assert
        entity.Id.ShouldBe(expectedId);
        entity.IsTransient().ShouldBeFalse();
        entity.GetDomainEvents().ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_GuidEntity_AutoGeneratesGuid()
    {
        // Act
        var entity = new TestGuidEntity();

        // Assert
        entity.Id.ShouldNotBe(Guid.Empty);
        entity.IsTransient().ShouldBeFalse();
        entity.GetDomainEvents().ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_GuidEntityWithId_UsesProvidedId()
    {
        // Arrange
        var expectedId = Guid.NewGuid();

        // Act
        var entity = new TestGuidEntity(expectedId);

        // Assert
        entity.Id.ShouldBe(expectedId);
        entity.IsTransient().ShouldBeFalse();
    }

    #endregion

    #region IsTransient Tests

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(-1, false)]
    [InlineData(int.MaxValue, false)]
    public void IsTransient_WithIntId_ReturnsExpectedResult(int id, bool expectedIsTransient)
    {
        // Arrange
        var entity = new TestEntity(id);

        // Act
        var isTransient = entity.IsTransient();

        // Assert
        isTransient.ShouldBe(expectedIsTransient);
        _output.WriteLine($"Entity with ID {id} is transient: {isTransient}");
    }

    [Fact]
    public void IsTransient_WithEmptyGuid_ReturnsTrue()
    {
        // Arrange
        var entity = new TestGuidEntity(Guid.Empty);

        // Act
        var isTransient = entity.IsTransient();

        // Assert
        isTransient.ShouldBeTrue();
    }

    [Fact]
    public void IsTransient_WithValidGuid_ReturnsFalse()
    {
        // Arrange
        var entity = new TestGuidEntity(Guid.NewGuid());

        // Act
        var isTransient = entity.IsTransient();

        // Assert
        isTransient.ShouldBeFalse();
    }

    #endregion

    #region Domain Events Tests

    [Fact]
    public void AddDomainEvent_SingleEvent_AddsEventSuccessfully()
    {
        // Arrange
        var entity = new TestEntity(1);
        var domainEvent = new TestDomainEvent();

        // Act
        entity.AddDomainEvent(domainEvent);

        // Assert
        var events = entity.GetDomainEvents();
        events.Count.ShouldBe(1);
        events.ShouldContain(domainEvent);
    }

    [Fact]
    public void AddDomainEvent_MultipleEvents_AddsAllEventsInOrder()
    {
        // Arrange
        var entity = new TestEntity(1);
        var event1 = new TestDomainEvent();
        var event2 = new AnotherTestDomainEvent();
        var event3 = new TestDomainEvent();

        // Act
        entity.AddDomainEvent(event1);
        entity.AddDomainEvent(event2);
        entity.AddDomainEvent(event3);

        // Assert
        var events = entity.GetDomainEvents();
        events.Count.ShouldBe(3);
        events.ElementAt(0).ShouldBe(event1);
        events.ElementAt(1).ShouldBe(event2);
        events.ElementAt(2).ShouldBe(event3);
    }

    [Fact]
    public void RemoveDomainEvent_ExistingEvent_RemovesEventSuccessfully()
    {
        // Arrange
        var entity = new TestEntity(1);
        var event1 = new TestDomainEvent();
        var event2 = new AnotherTestDomainEvent();
        entity.AddDomainEvent(event1);
        entity.AddDomainEvent(event2);

        // Act
        entity.RemoveDomainEvent(event1);

        // Assert
        var events = entity.GetDomainEvents();
        events.Count.ShouldBe(1);
        events.ShouldNotContain(event1);
        events.ShouldContain(event2);
    }

    [Fact]
    public void RemoveDomainEvent_NonExistingEvent_DoesNothing()
    {
        // Arrange
        var entity = new TestEntity(1);
        var existingEvent = new TestDomainEvent();
        var nonExistingEvent = new AnotherTestDomainEvent();
        entity.AddDomainEvent(existingEvent);

        // Act
        entity.RemoveDomainEvent(nonExistingEvent);

        // Assert
        var events = entity.GetDomainEvents();
        events.Count.ShouldBe(1);
        events.ShouldContain(existingEvent);
    }

    [Fact]
    public void ClearDomainEvents_WithMultipleEvents_RemovesAllEvents()
    {
        // Arrange
        var entity = new TestEntity(1);
        entity.AddDomainEvent(new TestDomainEvent());
        entity.AddDomainEvent(new AnotherTestDomainEvent());
        entity.AddDomainEvent(new TestDomainEvent());

        // Act
        entity.ClearDomainEvents();

        // Assert
        entity.GetDomainEvents().ShouldBeEmpty();
    }

    [Fact]
    public void GetDomainEvents_ReturnsReadOnlyCollection()
    {
        // Arrange
        var entity = new TestEntity(1);
        var domainEvent = new TestDomainEvent();
        entity.AddDomainEvent(domainEvent);

        // Act
        var events = entity.GetDomainEvents();

        // Assert
        events.ShouldBeOfType<System.Collections.ObjectModel.ReadOnlyCollection<IDomainEvent>>();
        // ReadOnlyCollection from AsReadOnly() is indeed read-only
        events.ShouldNotBeNull();
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        // Arrange
        var entity = new TestEntity(1, "Test");
        var sameEntity = entity; // Same reference

        // Act & Assert
        entity.Equals(sameEntity).ShouldBeTrue();
        (entity == sameEntity).ShouldBeTrue();
        (entity != sameEntity).ShouldBeFalse();
    }

    [Fact]
    public void Equals_SameIdSameType_ReturnsTrue()
    {
        // Arrange
        var entity1 = new TestEntity(1, "Test1");
        var entity2 = new TestEntity(1, "Test2");

        // Act & Assert
        entity1.Equals(entity2).ShouldBeTrue();
        (entity1 == entity2).ShouldBeTrue();
        (entity1 != entity2).ShouldBeFalse();
        _output.WriteLine($"Entities with same ID ({entity1.Id}) are considered equal even with different properties");
    }

    [Fact]
    public void Equals_DifferentId_ReturnsFalse()
    {
        // Arrange
        var entity1 = new TestEntity(1, "Test");
        var entity2 = new TestEntity(2, "Test");

        // Act & Assert
        entity1.Equals(entity2).ShouldBeFalse();
        (entity1 == entity2).ShouldBeFalse();
        (entity1 != entity2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        // Arrange
        var intEntity = new TestEntity(1);
        var guidEntity = new TestGuidEntity();

        // Act & Assert
        intEntity.Equals(guidEntity).ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var entity = new TestEntity(1);

        // Act & Assert
        entity.Equals(null).ShouldBeFalse();
        (entity == null).ShouldBeFalse();
        (entity != null).ShouldBeTrue();
        (null == entity).ShouldBeFalse();
        (null != entity).ShouldBeTrue();
    }

    [Fact]
    public void Equals_BothTransient_ReturnsFalse()
    {
        // Arrange
        var entity1 = new TestEntity(); // ID = 0 (transient)
        var entity2 = new TestEntity(); // ID = 0 (transient)

        // Act & Assert
        entity1.Equals(entity2).ShouldBeFalse();
        _output.WriteLine("Transient entities are never equal, even with same default ID");
    }

    [Fact]
    public void Equals_OneTransient_ReturnsFalse()
    {
        // Arrange
        var transientEntity = new TestEntity(); // ID = 0 (transient)
        var persistedEntity = new TestEntity(0); // ID = 0 but explicitly set

        // Act & Assert
        transientEntity.Equals(persistedEntity).ShouldBeFalse();
        persistedEntity.Equals(transientEntity).ShouldBeFalse();
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_TransientEntity_UsesReferenceHashCode()
    {
        // Arrange
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();

        // Act
        var hash1 = entity1.GetHashCode();
        var hash2 = entity2.GetHashCode();

        // Assert
        hash1.ShouldNotBe(hash2);
        _output.WriteLine($"Transient entity hashes: {hash1}, {hash2}");
    }

    [Fact]
    public void GetHashCode_SameId_ReturnsSameHashCode()
    {
        // Arrange
        var entity1 = new TestEntity(42);
        var entity2 = new TestEntity(42);

        // Act
        var hash1 = entity1.GetHashCode();
        var hash2 = entity2.GetHashCode();

        // Assert
        hash1.ShouldBe(hash2);
        _output.WriteLine($"Entities with same ID have same hash code: {hash1}");
    }

    [Fact]
    public void GetHashCode_DifferentId_ReturnsDifferentHashCode()
    {
        // Arrange
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(2);

        // Act
        var hash1 = entity1.GetHashCode();
        var hash2 = entity2.GetHashCode();

        // Assert
        hash1.ShouldNotBe(hash2);
    }

    [Fact]
    public void GetHashCode_ConsistentAcrossMultipleCalls()
    {
        // Arrange
        var entity = new TestEntity(123);

        // Act
        var hash1 = entity.GetHashCode();
        var hash2 = entity.GetHashCode();
        var hash3 = entity.GetHashCode();

        // Assert
        hash1.ShouldBe(hash2);
        hash2.ShouldBe(hash3);
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public void Entity_CompleteWorkflow_BehavesCorrectly()
    {
        // Arrange
        var entity = new TestEntity(1, "Initial");
        var event1 = new TestDomainEvent();
        var event2 = new AnotherTestDomainEvent();

        // Act & Assert - Initial state
        entity.IsTransient().ShouldBeFalse();
        entity.GetDomainEvents().ShouldBeEmpty();

        // Act & Assert - Add events
        entity.AddDomainEvent(event1);
        entity.AddDomainEvent(event2);
        entity.GetDomainEvents().Count.ShouldBe(2);

        // Act & Assert - Modify and verify equality still works
        entity.Name = "Modified";
        var sameEntity = new TestEntity(1, "Different Name");
        entity.Equals(sameEntity).ShouldBeTrue();

        // Act & Assert - Clear events
        entity.ClearDomainEvents();
        entity.GetDomainEvents().ShouldBeEmpty();

        _output.WriteLine("Complete entity workflow test passed");
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void Entity_WithVariousIds_MaintainsConsistency(int id)
    {
        // Arrange & Act
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        // Assert
        entity1.Equals(entity2).ShouldBe(!entity1.IsTransient());
        if (!entity1.IsTransient())
        {
            entity1.GetHashCode().ShouldBe(entity2.GetHashCode());
        }

        _output.WriteLine($"Entity with ID {id} - IsTransient: {entity1.IsTransient()}");
    }

    [Fact]
    public void GuidEntity_AutoGeneratedIds_AreUnique()
    {
        // Arrange & Act
        var entities = Enumerable.Range(0, 100)
            .Select(_ => new TestGuidEntity())
            .ToArray();

        // Assert
        var uniqueIds = entities.Select(e => e.Id).Distinct().ToArray();
        uniqueIds.Length.ShouldBe(entities.Length);
        _output.WriteLine($"Generated {entities.Length} unique GUIDs");
    }

    #endregion
}
