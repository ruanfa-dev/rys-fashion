using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging.Abstracts;

using Shouldly;

namespace SharedKernel.UnitTests.Domains.Primitives;

/// <summary>
/// Unit tests for AuditableEntity functionality including auditing capabilities, inheritance behavior, and domain events.
/// </summary>
public sealed class AuditableEntityTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region Test Implementations

    private sealed class TestAuditableEntity : AuditableEntity<int>
    {
        public string Name { get; set; } = string.Empty;

        public TestAuditableEntity() : base() { }
        public TestAuditableEntity(int id) : base(id) { }
        public TestAuditableEntity(int id, string name) : base(id)
        {
            Name = name;
        }
    }

    private sealed class TestGuidAuditableEntity : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;

        public TestGuidAuditableEntity() : base() { }
        public TestGuidAuditableEntity(Guid id) : base(id) { }
        public TestGuidAuditableEntity(string name) : base()
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

    #endregion

    #region Constructor and Initialization Tests

    [Fact]
    public void Constructor_Default_InitializesAuditFieldsAutomatically()
    {
        // Arrange
        var beforeCreation = DateTimeOffset.UtcNow;

        // Act
        var entity = new TestAuditableEntity();
        var afterCreation = DateTimeOffset.UtcNow;

        // Assert
        entity.Id.ShouldBe(0);
        entity.IsTransient().ShouldBeTrue();
        entity.CreatedAt.ShouldBeGreaterThanOrEqualTo(beforeCreation);
        entity.CreatedAt.ShouldBeLessThanOrEqualTo(afterCreation);
        entity.UpdatedAt.ShouldBeNull();
        entity.CreatedBy.ShouldBeNull();
        entity.UpdatedBy.ShouldBeNull();
        entity.GetDomainEvents().ShouldBeEmpty();

        _output.WriteLine($"Entity created at: {entity.CreatedAt}");
    }

    [Fact]
    public void Constructor_WithId_InitializesAuditFieldsAndId()
    {
        // Arrange
        const int expectedId = 123;
        var beforeCreation = DateTimeOffset.UtcNow;

        // Act
        var entity = new TestAuditableEntity(expectedId);
        var afterCreation = DateTimeOffset.UtcNow;

        // Assert
        entity.Id.ShouldBe(expectedId);
        entity.IsTransient().ShouldBeFalse();
        entity.CreatedAt.ShouldBeGreaterThanOrEqualTo(beforeCreation);
        entity.CreatedAt.ShouldBeLessThanOrEqualTo(afterCreation);
        entity.UpdatedAt.ShouldBeNull();
        entity.CreatedBy.ShouldBeNull();
        entity.UpdatedBy.ShouldBeNull();
    }

    [Fact]
    public void Constructor_GuidEntity_AutoGeneratesGuidAndInitializesAudit()
    {
        // Arrange
        var beforeCreation = DateTimeOffset.UtcNow;

        // Act
        var entity = new TestGuidAuditableEntity();
        var afterCreation = DateTimeOffset.UtcNow;

        // Assert
        entity.Id.ShouldNotBe(Guid.Empty);
        entity.IsTransient().ShouldBeFalse();
        entity.CreatedAt.ShouldBeGreaterThanOrEqualTo(beforeCreation);
        entity.CreatedAt.ShouldBeLessThanOrEqualTo(afterCreation);
        entity.UpdatedAt.ShouldBeNull();
        entity.CreatedBy.ShouldBeNull();
        entity.UpdatedBy.ShouldBeNull();
    }

    [Fact]
    public void Constructor_GuidEntityWithId_UsesProvidedIdAndInitializesAudit()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var beforeCreation = DateTimeOffset.UtcNow;

        // Act
        var entity = new TestGuidAuditableEntity(expectedId);
        var afterCreation = DateTimeOffset.UtcNow;

        // Assert
        entity.Id.ShouldBe(expectedId);
        entity.IsTransient().ShouldBeFalse();
        entity.CreatedAt.ShouldBeGreaterThanOrEqualTo(beforeCreation);
        entity.CreatedAt.ShouldBeLessThanOrEqualTo(afterCreation);
    }

    #endregion

    #region MarkAsCreated Tests

    [Fact]
    public void MarkAsCreated_WithoutUserId_SetsTimestampOnly()
    {
        // Arrange
        var entity = new TestAuditableEntity(1);
        var originalCreatedAt = entity.CreatedAt;

        // Wait a small amount to ensure timestamp difference
        Thread.Sleep(1);
        var beforeMark = DateTimeOffset.UtcNow;

        // Act
        entity.MarkAsCreated();
        var afterMark = DateTimeOffset.UtcNow;

        // Assert
        entity.CreatedAt.ShouldBeGreaterThan(originalCreatedAt);
        entity.CreatedAt.ShouldBeGreaterThanOrEqualTo(beforeMark);
        entity.CreatedAt.ShouldBeLessThanOrEqualTo(afterMark);
        entity.CreatedBy.ShouldBeNull();
        entity.UpdatedAt.ShouldBeNull();
        entity.UpdatedBy.ShouldBeNull();

        _output.WriteLine($"Original: {originalCreatedAt}, New: {entity.CreatedAt}");
    }

    [Fact]
    public void MarkAsCreated_WithUserId_SetsTimestampAndUser()
    {
        // Arrange
        var entity = new TestAuditableEntity(1);
        const string userId = "user123";
        var beforeMark = DateTimeOffset.UtcNow;

        // Act
        entity.MarkAsCreated(userId);
        var afterMark = DateTimeOffset.UtcNow;

        // Assert
        entity.CreatedAt.ShouldBeGreaterThanOrEqualTo(beforeMark);
        entity.CreatedAt.ShouldBeLessThanOrEqualTo(afterMark);
        entity.CreatedBy.ShouldBe(userId);
        entity.UpdatedAt.ShouldBeNull();
        entity.UpdatedBy.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("user123")]
    [InlineData("admin@example.com")]
    public void MarkAsCreated_WithVariousUserIds_HandlesCorrectly(string? userId)
    {
        // Arrange
        var entity = new TestAuditableEntity(1);
        var beforeMark = DateTimeOffset.UtcNow;

        // Act
        entity.MarkAsCreated(userId);
        var afterMark = DateTimeOffset.UtcNow;

        // Assert
        entity.CreatedAt.ShouldBeGreaterThanOrEqualTo(beforeMark);
        entity.CreatedAt.ShouldBeLessThanOrEqualTo(afterMark);
        entity.CreatedBy.ShouldBe(userId);

        _output.WriteLine($"UserId: '{userId}' -> CreatedBy: '{entity.CreatedBy}'");
    }

    #endregion

    #region MarkAsUpdated Tests

    [Fact]
    public void MarkAsUpdated_WithoutUserId_SetsTimestampOnly()
    {
        // Arrange
        var entity = new TestAuditableEntity(1);
        var originalCreatedAt = entity.CreatedAt;
        var beforeUpdate = DateTimeOffset.UtcNow;

        // Act
        entity.MarkAsUpdated();
        var afterUpdate = DateTimeOffset.UtcNow;

        // Assert
        entity.CreatedAt.ShouldBe(originalCreatedAt); // Should not change
        entity.UpdatedAt.ShouldNotBeNull();
        entity.UpdatedAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeUpdate);
        entity.UpdatedAt.Value.ShouldBeLessThanOrEqualTo(afterUpdate);
        entity.UpdatedBy.ShouldBeNull();
        entity.CreatedBy.ShouldBeNull(); // Should remain unchanged

        _output.WriteLine($"Updated at: {entity.UpdatedAt}");
    }

    [Fact]
    public void MarkAsUpdated_WithUserId_SetsTimestampAndUser()
    {
        // Arrange
        var entity = new TestAuditableEntity(1);
        const string userId = "updater456";
        var beforeUpdate = DateTimeOffset.UtcNow;

        // Act
        entity.MarkAsUpdated(userId);
        var afterUpdate = DateTimeOffset.UtcNow;

        // Assert
        entity.UpdatedAt.ShouldNotBeNull();
        entity.UpdatedAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeUpdate);
        entity.UpdatedAt.Value.ShouldBeLessThanOrEqualTo(afterUpdate);
        entity.UpdatedBy.ShouldBe(userId);
    }

    [Fact]
    public void MarkAsUpdated_MultipleUpdates_UpdatesTimestampEachTime()
    {
        // Arrange
        var entity = new TestAuditableEntity(1);

        // Act & Assert - First update
        entity.MarkAsUpdated("user1");
        var firstUpdate = entity.UpdatedAt;
        firstUpdate.ShouldNotBeNull();
        entity.UpdatedBy.ShouldBe("user1");

        // Wait to ensure timestamp difference
        Thread.Sleep(1);

        // Act & Assert - Second update
        entity.MarkAsUpdated("user2");
        var secondUpdate = entity.UpdatedAt;
        secondUpdate.ShouldNotBeNull();
        secondUpdate.Value.ShouldBeGreaterThan(firstUpdate!.Value);
        entity.UpdatedBy.ShouldBe("user2");

        _output.WriteLine($"First: {firstUpdate}, Second: {secondUpdate}");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("admin")]
    [InlineData("system@example.com")]
    public void MarkAsUpdated_WithVariousUserIds_HandlesCorrectly(string? userId)
    {
        // Arrange
        var entity = new TestAuditableEntity(1);
        var beforeUpdate = DateTimeOffset.UtcNow;

        // Act
        entity.MarkAsUpdated(userId);
        var afterUpdate = DateTimeOffset.UtcNow;

        // Assert
        entity.UpdatedAt.ShouldNotBeNull();
        entity.UpdatedAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeUpdate);
        entity.UpdatedAt!.Value.ShouldBeLessThanOrEqualTo(afterUpdate);
        entity.UpdatedBy.ShouldBe(userId);

        _output.WriteLine($"UserId: '{userId}' -> UpdatedBy: '{entity.UpdatedBy}'");
    }

    #endregion

    #region Inheritance and Entity Behavior Tests

    [Fact]
    public void AuditableEntity_InheritsEntityBehavior_DomainEventsWork()
    {
        // Arrange
        var entity = new TestAuditableEntity(1);
        var domainEvent = new TestDomainEvent();

        // Act
        entity.AddDomainEvent(domainEvent);

        // Assert
        entity.GetDomainEvents().Count.ShouldBe(1);
        entity.GetDomainEvents().ShouldContain(domainEvent);
    }

    [Fact]
    public void AuditableEntity_InheritsEntityBehavior_EqualityWorks()
    {
        // Arrange
        var entity1 = new TestAuditableEntity(1, "Test1");
        var entity2 = new TestAuditableEntity(1, "Test2");

        // Simulate different audit times
        Thread.Sleep(1);
        entity2.MarkAsUpdated("different-user");

        // Act & Assert
        entity1.Equals(entity2).ShouldBeTrue();
        (entity1 == entity2).ShouldBeTrue();
        entity1.GetHashCode().ShouldBe(entity2.GetHashCode());

        _output.WriteLine("Entities are equal despite different audit information");
    }

    [Fact]
    public void AuditableEntity_InheritsEntityBehavior_IsTransientWorks()
    {
        // Arrange & Act
        var transientEntity = new TestAuditableEntity(); // ID = 0
        var persistedEntity = new TestAuditableEntity(1);

        // Assert
        transientEntity.IsTransient().ShouldBeTrue();
        persistedEntity.IsTransient().ShouldBeFalse();
    }

    #endregion

    #region Complete Workflow and Integration Tests

    [Fact]
    public void AuditableEntity_CompleteLifecycle_BehavesCorrectly()
    {
        // Arrange
        var entity = new TestAuditableEntity(1, "Initial Name");
        var originalCreatedAt = entity.CreatedAt;

        // Act & Assert - Initial state
        entity.IsTransient().ShouldBeFalse();
        entity.CreatedAt.ShouldNotBe(default);
        entity.UpdatedAt.ShouldBeNull();
        entity.CreatedBy.ShouldBeNull();
        entity.UpdatedBy.ShouldBeNull();

        // Wait to ensure timestamp difference
        Thread.Sleep(1);

        // Act & Assert - Manual creation marking
        entity.MarkAsCreated("creator123");
        entity.CreatedAt.ShouldBeGreaterThan(originalCreatedAt);
        entity.CreatedBy.ShouldBe("creator123");
        entity.UpdatedAt.ShouldBeNull();

        // Wait to ensure timestamp difference
        Thread.Sleep(1);

        // Act & Assert - First update
        entity.MarkAsUpdated("updater456");
        entity.UpdatedAt.ShouldNotBeNull();
        entity.UpdatedAt.Value.ShouldBeGreaterThan(entity.CreatedAt);
        entity.UpdatedBy.ShouldBe("updater456");

        // Act & Assert - Domain events still work
        var domainEvent = new TestDomainEvent();
        entity.AddDomainEvent(domainEvent);
        entity.GetDomainEvents().ShouldContain(domainEvent);

        // Act & Assert - Second update
        Thread.Sleep(1);
        var firstUpdateTime = entity.UpdatedAt;
        entity.MarkAsUpdated("finalUpdater");
        entity.UpdatedAt.ShouldNotBeNull();
        entity.UpdatedAt.Value.ShouldBeGreaterThan(firstUpdateTime!.Value);
        entity.UpdatedBy.ShouldBe("finalUpdater");

        _output.WriteLine($"Complete lifecycle: Created={entity.CreatedAt}, Updated={entity.UpdatedAt}");
    }

    [Fact]
    public void AuditableEntity_ConcurrentCreation_GeneratesUniqueTimestamps()
    {
        // Arrange & Act
        var entities = Enumerable.Range(0, 10)
            .Select(i => new TestAuditableEntity(i))
            .ToArray();

        // Assert
        var createdTimes = entities.Select(e => e.CreatedAt).ToArray();
        var uniqueTimes = createdTimes.Distinct().Count();

        // Allow for some timestamp collisions in fast execution
        uniqueTimes.ShouldBeGreaterThan(5);
        _output.WriteLine($"Created {entities.Length} entities with {uniqueTimes} unique timestamps");
    }

    [Fact]
    public void AuditableEntity_WithComplexScenario_MaintainsDataIntegrity()
    {
        // Arrange
        var entity = new TestAuditableEntity(42, "Complex Test");
        var domainEvent1 = new TestDomainEvent();
        var domainEvent2 = new TestDomainEvent();

        // Act - Complex scenario
        entity.AddDomainEvent(domainEvent1);
        entity.MarkAsCreated("system");

        Thread.Sleep(1);
        entity.MarkAsUpdated("user1");
        entity.AddDomainEvent(domainEvent2);

        Thread.Sleep(1);
        entity.MarkAsUpdated("user2");
        entity.Name = "Updated Name";

        // Assert - All behaviors work together
        entity.Id.ShouldBe(42);
        entity.Name.ShouldBe("Updated Name");
        entity.IsTransient().ShouldBeFalse();

        entity.CreatedBy.ShouldBe("system");
        entity.UpdatedBy.ShouldBe("user2");
        entity.UpdatedAt.ShouldNotBeNull();
        entity.UpdatedAt.Value.ShouldBeGreaterThan(entity.CreatedAt);

        entity.GetDomainEvents().Count.ShouldBe(2);
        entity.GetDomainEvents().ShouldContain(domainEvent1);
        entity.GetDomainEvents().ShouldContain(domainEvent2);

        // Test equality with another entity
        var otherEntity = new TestAuditableEntity(42, "Different Name");
        entity.Equals(otherEntity).ShouldBeTrue();

        _output.WriteLine("Complex scenario completed successfully");
    }

    #endregion

    #region Edge Cases and Boundary Tests

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void AuditableEntity_WithVariousIds_InitializesCorrectly(int id)
    {
        // Arrange & Act
        var entity = new TestAuditableEntity(id);

        // Assert
        entity.Id.ShouldBe(id);
        entity.IsTransient().ShouldBe(id == 0);
        entity.CreatedAt.ShouldNotBe(default);
        entity.UpdatedAt.ShouldBeNull();

        _output.WriteLine($"Entity with ID {id} - IsTransient: {entity.IsTransient()}");
    }

    [Fact]
    public void GuidAuditableEntity_AutoGeneratedIds_AreUniqueAndNonTransient()
    {
        // Arrange & Act
        var entities = Enumerable.Range(0, 50)
            .Select(_ => new TestGuidAuditableEntity())
            .ToArray();

        // Assert
        var uniqueIds = entities.Select(e => e.Id).Distinct().ToArray();
        uniqueIds.Length.ShouldBe(entities.Length);

        entities.ShouldAllBe(e => !e.IsTransient());
        entities.ShouldAllBe(e => e.CreatedAt != default);
        entities.ShouldAllBe(e => e.UpdatedAt == null);

        _output.WriteLine($"Generated {entities.Length} unique auditable GUID entities");
    }

    [Fact]
    public void AuditableEntity_AuditFieldsPrecision_MaintainsAccuracy()
    {
        // Arrange
        var entity = new TestAuditableEntity(1);
        var precision = TimeSpan.FromMilliseconds(1);

        // Act - Multiple rapid updates
        var updates = new List<DateTimeOffset>();
        for (var i = 0; i < 5; i++)
        {
            entity.MarkAsUpdated($"user{i}");
            updates.Add(entity.UpdatedAt!.Value);
            Thread.Sleep(1);
        }

        // Assert
        for (var i = 1; i < updates.Count; i++)
        {
            updates[i].ShouldBeGreaterThan(updates[i - 1]);
        }

        _output.WriteLine($"Audit precision test completed with {updates.Count} sequential updates");
    }

    #endregion
}
