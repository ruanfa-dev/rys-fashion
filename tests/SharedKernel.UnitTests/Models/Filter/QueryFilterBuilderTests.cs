using Shouldly;
using SharedKernel.Models.Filter;

namespace SharedKernel.UnitTests.Models.Filter;

/// <summary>
/// Unit tests for QueryFilterBuilder fluent API functionality.
/// </summary>
public sealed class QueryFilterBuilderTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    // Test data class (same as in QueryFilterExtensionsTests)
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public string Department { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
    }

    private static List<User> GetTestUsers() =>
    [
        new User { Id = 1, Name = "John Doe", Email = "john@example.com", Age = 25, IsActive = true, Department = "IT", CreatedAt = DateTimeOffset.UtcNow.AddDays(-10) },
        new User { Id = 2, Name = "Jane Smith", Email = "jane@example.com", Age = 30, IsActive = true, Department = "HR", CreatedAt = DateTimeOffset.UtcNow.AddDays(-5) },
        new User { Id = 3, Name = "Bob Johnson", Email = "bob@example.com", Age = 35, IsActive = false, Department = "IT", CreatedAt = DateTimeOffset.UtcNow.AddDays(-20) },
        new User { Id = 4, Name = "Alice Brown", Email = "alice@example.com", Age = 28, IsActive = true, Department = "Finance", CreatedAt = DateTimeOffset.UtcNow.AddDays(-15) },
        new User { Id = 5, Name = "Charlie Wilson", Email = "charlie@example.com", Age = 32, IsActive = false, Department = "IT", CreatedAt = DateTimeOffset.UtcNow.AddDays(-8) }
    ];

    [Fact]
    public void Create_ReturnsNewBuilder()
    {
        // Act
        var builder = QueryFilterBuilder.Create();

        // Assert
        builder.ShouldNotBeNull();
        builder.Build().ShouldBeEmpty();
    }

    [Fact]
    public void Create_WithDefaultLogicalOperator_SetsDefaultCorrectly()
    {
        // Act
        var builder = QueryFilterBuilder.Create(FilterLogicalOperator.Any);

        // Assert
        builder.ShouldNotBeNull();
    }

    [Fact]
    public void Add_SingleFilter_BuildsCorrectly()
    {
        // Arrange
        var builder = QueryFilterBuilder.Create();

        // Act
        var filters = builder
            .Add("name", FilterOperator.Equal, "John Doe")
            .Build();

        // Assert
        filters.Count.ShouldBe(1);
        filters[0].Field.ShouldBe("name");
        filters[0].Operator.ShouldBe(FilterOperator.Equal);
        filters[0].Value.ShouldBe("John Doe");
        filters[0].LogicalOperator.ShouldBe(FilterLogicalOperator.All);
    }

    [Fact]
    public void And_AddsAndFilter()
    {
        // Arrange
        var builder = QueryFilterBuilder.Create();

        // Act
        var filters = builder
            .And("name", FilterOperator.Equal, "John")
            .And("age", FilterOperator.GreaterThan, "25")
            .Build();

        // Assert
        filters.Count.ShouldBe(2);
        filters.All(f => f.LogicalOperator == FilterLogicalOperator.All).ShouldBeTrue();
    }

    [Fact]
    public void Or_AddsOrFilter()
    {
        // Arrange
        var builder = QueryFilterBuilder.Create();

        // Act
        var filters = builder
            .Or("name", FilterOperator.Equal, "John")
            .Or("age", FilterOperator.GreaterThan, "30")
            .Build();

        // Assert
        filters.Count.ShouldBe(2);
        filters.All(f => f.LogicalOperator == FilterLogicalOperator.Any).ShouldBeTrue();
    }

    [Fact]
    public void Equal_AddsEqualityFilter()
    {
        // Arrange
        var builder = QueryFilterBuilder.Create();

        // Act
        var filters = builder
            .Equal("name", "John Doe")
            .Build();

        // Assert
        filters.Count.ShouldBe(1);
        filters[0].Operator.ShouldBe(FilterOperator.Equal);
        filters[0].Value.ShouldBe("John Doe");
    }

    [Fact]
    public void Contains_AddsContainsFilter()
    {
        // Arrange
        var builder = QueryFilterBuilder.Create();

        // Act
        var filters = builder
            .Contains("name", "john")
            .Build();

        // Assert
        filters.Count.ShouldBe(1);
        filters[0].Operator.ShouldBe(FilterOperator.Contains);
        filters[0].Value.ShouldBe("john");
    }

    [Fact]
    public void GreaterThan_AddsGreaterThanFilter()
    {
        // Arrange
        var builder = QueryFilterBuilder.Create();

        // Act
        var filters = builder
            .GreaterThan("age", "25")
            .Build();

        // Assert
        filters.Count.ShouldBe(1);
        filters[0].Operator.ShouldBe(FilterOperator.GreaterThan);
        filters[0].Value.ShouldBe("25");
    }

    [Fact]
    public void LessThan_AddsLessThanFilter()
    {
        // Arrange
        var builder = QueryFilterBuilder.Create();

        // Act
        var filters = builder
            .LessThan("age", "35")
            .Build();

        // Assert
        filters.Count.ShouldBe(1);
        filters[0].Operator.ShouldBe(FilterOperator.LessThan);
        filters[0].Value.ShouldBe("35");
    }

    [Fact]
    public void In_WithStringValues_AddsInFilter()
    {
        // Arrange
        var builder = QueryFilterBuilder.Create();

        // Act
        var filters = builder
            .In("department", "IT,HR,Finance")
            .Build();

        // Assert
        filters.Count.ShouldBe(1);
        filters[0].Operator.ShouldBe(FilterOperator.In);
        filters[0].Value.ShouldBe("IT,HR,Finance");
    }

    [Fact]
    public void In_WithEnumerableValues_AddsInFilter()
    {
        // Arrange
        var builder = QueryFilterBuilder.Create();
        var departments = new[] { "IT", "HR", "Finance" };

        // Act
        var filters = builder
            .In("department", departments)
            .Build();

        // Assert
        filters.Count.ShouldBe(1);
        filters[0].Operator.ShouldBe(FilterOperator.In);
        filters[0].Value.ShouldBe("IT,HR,Finance");
    }

    [Fact]
    public void Range_AddsRangeFilter()
    {
        // Arrange
        var builder = QueryFilterBuilder.Create();

        // Act
        var filters = builder
            .Range("age", "25", "35")
            .Build();

        // Assert
        filters.Count.ShouldBe(1);
        filters[0].Operator.ShouldBe(FilterOperator.Range);
        filters[0].Value.ShouldBe("25,35");
    }

    [Fact]
    public void IsNull_AddsNullCheckFilter()
    {
        // Arrange
        var builder = QueryFilterBuilder.Create();

        // Act
        var filters = builder
            .IsNull("address")
            .Build();

        // Assert
        filters.Count.ShouldBe(1);
        filters[0].Operator.ShouldBe(FilterOperator.IsNull);
        filters[0].Value.ShouldBe(string.Empty);
    }

    [Fact]
    public void IsNotNull_AddsNotNullCheckFilter()
    {
        // Arrange
        var builder = QueryFilterBuilder.Create();

        // Act
        var filters = builder
            .IsNotNull("address")
            .Build();

        // Assert
        filters.Count.ShouldBe(1);
        filters[0].Operator.ShouldBe(FilterOperator.IsNotNull);
        filters[0].Value.ShouldBe(string.Empty);
    }

    [Fact]
    public void InGroup_SetsGroupForSubsequentFilters()
    {
        // Arrange
        var builder = QueryFilterBuilder.Create();

        // Act
        var filters = builder
            .InGroup(1)
            .Equal("name", "John")
            .Equal("age", "25")
            .InGroup(2)
            .Equal("department", "IT")
            .Build();

        // Assert
        filters.Count.ShouldBe(3);
        filters[0].Group.ShouldBe(1);
        filters[1].Group.ShouldBe(1);
        filters[2].Group.ShouldBe(2);
    }

    [Fact]
    public void WithDefaultLogic_SetsDefaultLogicalOperator()
    {
        // Arrange
        var builder = QueryFilterBuilder.Create();

        // Act
        var filters = builder
            .WithDefaultLogic(FilterLogicalOperator.Any)
            .Equal("name", "John")
            .Equal("department", "IT")
            .Build();

        // Assert
        filters.Count.ShouldBe(2);
        filters.All(f => f.LogicalOperator == FilterLogicalOperator.Any).ShouldBeTrue();
    }

    [Fact]
    public void ComplexBuilder_WithMixedOperators_BuildsCorrectly()
    {
        // Arrange
        var builder = QueryFilterBuilder.Create();

        // Act
        var filters = builder
            .WithDefaultLogic(FilterLogicalOperator.All)
            .InGroup(0)
            .Or("name", FilterOperator.Contains, "john")
            .Or("age", FilterOperator.GreaterThanOrEqual, "30")
            .InGroup(1)
            .And("department", FilterOperator.Equal, "IT")
            .And("isactive", FilterOperator.Equal, "true")
            .Build();

        // Assert
        filters.Count.ShouldBe(4);

        // Group 0 filters should use OR
        filters.Where(f => f.Group == 0).All(f => f.LogicalOperator == FilterLogicalOperator.Any).ShouldBeTrue();

        // Group 1 filters should use AND
        filters.Where(f => f.Group == 1).All(f => f.LogicalOperator == FilterLogicalOperator.All).ShouldBeTrue();
    }

    [Fact]
    public void ApplyTo_AppliesFiltersToQueryable()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var builder = QueryFilterBuilder.Create();

        // Act
        var result = builder
            .Equal("department", "IT")
            .And("isactive", FilterOperator.Equal, "true")
            .ApplyTo(query);

        // Assert
        result.Count().ShouldBe(1);
        result.First().Name.ShouldBe("John Doe");
    }

    [Fact]
    public void ApplyTo_WithOrLogic_AppliesFiltersCorrectly()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var builder = QueryFilterBuilder.Create();

        // Act
        var result = builder
            .Or("name", FilterOperator.Equal, "John Doe")
            .Or("department", FilterOperator.Equal, "HR")
            .ApplyTo(query);

        // Assert
        result.Count().ShouldBe(2); // John and Jane
        result.Select(u => u.Name).OrderBy(n => n).ShouldBe(["Jane Smith", "John Doe"]);
    }

    [Fact]
    public void ApplyTo_WithComplexGrouping_AppliesFiltersCorrectly()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var builder = QueryFilterBuilder.Create();

        _output.WriteLine("Testing complex grouping: (name contains john OR age >= 30) AND department = IT");

        // Act
        var result = builder
            .InGroup(0)
            .Or("name", FilterOperator.Contains, "john")
            .Or("age", FilterOperator.GreaterThanOrEqual, "30")
            .InGroup(1)
            .And("department", FilterOperator.Equal, "IT")
            .ApplyTo(query);

        // Assert
        // Should get users where (name contains john OR age >= 30) AND department = IT
        // John: contains john + IT = ✓
        // Bob: age 35 + IT = ✓  
        // Charlie: age 32 + IT = ✓
        result.Count().ShouldBe(3);
        result.All(u => u.Department == "IT").ShouldBeTrue();
        result.Select(u => u.Name).OrderBy(n => n).ShouldBe(["Bob Johnson", "Charlie Wilson", "John Doe"]);
    }

    [Fact]
    public void Build_WithInvalidFilter_ThrowsException()
    {
        // Arrange
        var builder = QueryFilterBuilder.Create();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            builder
                .Add(string.Empty, FilterOperator.Equal, "value")
                .Build()
        );
    }

    [Fact]
    public void Build_WithNullCheckAndValue_ThrowsException()
    {
        // Arrange
        var builder = QueryFilterBuilder.Create();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            builder
                .Add("field", FilterOperator.IsNull, "should-not-have-value")
                .Build()
        );
    }

    [Fact]
    public void MethodChaining_AllowsFluentInterface()
    {
        // Arrange & Act
        var filters = QueryFilterBuilder
            .Create(FilterLogicalOperator.Any)
            .InGroup(1)
            .Equal("name", "John")
            .Contains("email", "@example.com")
            .GreaterThan("age", "18")
            .In("department", ["IT", "HR"])
            .Range("createdAt", "2024-01-01", "2024-12-31")
            .IsNotNull("address")
            .Build();

        // Assert
        filters.Count.ShouldBe(6);
        filters.All(f => f.Group == 1).ShouldBeTrue();
        filters.All(f => f.LogicalOperator == FilterLogicalOperator.Any).ShouldBeTrue();
    }
}
