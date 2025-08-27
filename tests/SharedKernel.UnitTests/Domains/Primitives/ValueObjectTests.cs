using SharedKernel.Domain.Primitives;

using Shouldly;

namespace SharedKernel.UnitTests.Domains.Primitives;

/// <summary>
/// Unit tests for ValueObject base class functionality including equality, comparison, and hashing.
/// </summary>
public sealed class ValueObjectTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region Test Implementations

    private sealed class TestValueObject : ValueObject
    {
        public string Value1 { get; }
        public int Value2 { get; }

        public TestValueObject(string value1, int value2)
        {
            Value1 = value1;
            Value2 = value2;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value1;
            yield return Value2;
        }
    }

    private sealed class SingleValueObject : ValueObject
    {
        public string Value { get; }

        public SingleValueObject(string value)
        {
            Value = value;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }
    }

    private sealed class ComplexValueObject : ValueObject
    {
        public string StringValue { get; }
        public int IntValue { get; }
        public decimal DecimalValue { get; }
        public DateTime DateValue { get; }
        public bool BoolValue { get; }
        public string? NullableValue { get; }

        public ComplexValueObject(string stringValue, int intValue, decimal decimalValue,
            DateTime dateValue, bool boolValue, string? nullableValue = null)
        {
            StringValue = stringValue;
            IntValue = intValue;
            DecimalValue = decimalValue;
            DateValue = dateValue;
            BoolValue = boolValue;
            NullableValue = nullableValue;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return StringValue;
            yield return IntValue;
            yield return DecimalValue;
            yield return DateValue;
            yield return BoolValue;
            yield return NullableValue;
        }
    }

    private sealed class EmptyValueObject : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield break;
        }
    }

    private sealed class DifferentTypeValueObject : ValueObject
    {
        public string Value { get; }

        public DifferentTypeValueObject(string value)
        {
            Value = value;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }
    }

    // Value object using record syntax but not inheriting from ValueObject
    private sealed class RecordLikeValueObject : ValueObject
    {
        public string Value1 { get; }
        public int Value2 { get; }

        public RecordLikeValueObject(string value1, int value2)
        {
            Value1 = value1;
            Value2 = value2;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            return this.ToEqualityComponents();
        }
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        // Arrange
        var value = new TestValueObject("test", 42);
        var sameValue = value;

        // Act & Assert
        value.Equals(sameValue).ShouldBeTrue();
        (value == sameValue).ShouldBeTrue();
        (value != sameValue).ShouldBeFalse();
    }

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var value1 = new TestValueObject("test", 42);
        var value2 = new TestValueObject("test", 42);

        // Act & Assert
        value1.Equals(value2).ShouldBeTrue();
        (value1 == value2).ShouldBeTrue();
        (value1 != value2).ShouldBeFalse();

        _output.WriteLine($"Value objects with same components are equal: {value1} == {value2}");
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var value1 = new TestValueObject("test", 42);
        var value2 = new TestValueObject("test", 43);

        // Act & Assert
        value1.Equals(value2).ShouldBeFalse();
        (value1 == value2).ShouldBeFalse();
        (value1 != value2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_DifferentTypes_ReturnsFalse()
    {
        // Arrange
        var value1 = new TestValueObject("test", 42);
        var value2 = new DifferentTypeValueObject("test");

        // Act & Assert
        value1.Equals(value2).ShouldBeFalse();
        (value1 == value2).ShouldBeFalse();
        (value1 != value2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var value = new TestValueObject("test", 42);

        // Act & Assert
        value.Equals(null).ShouldBeFalse();
        (value == null).ShouldBeFalse();
        (value != null).ShouldBeTrue();
        (null == value).ShouldBeFalse();
        (null != value).ShouldBeTrue();
    }

    [Fact]
    public void Equals_BothNull_ReturnsTrue()
    {
        // Arrange
        TestValueObject? value1 = null;
        TestValueObject? value2 = null;

        // Act & Assert
        (value1 == value2).ShouldBeTrue();
        (value1 != value2).ShouldBeFalse();
    }

    [Theory]
    [InlineData("test", 42, "test", 42, true)]
    [InlineData("test", 42, "test", 43, false)]
    [InlineData("test", 42, "different", 42, false)]
    [InlineData("", 0, "", 0, true)]
    [InlineData(null, 0, null, 0, true)]
    [InlineData("test", 0, null, 0, false)]
    public void Equals_VariousValues_ReturnsExpectedResult(string? value1Str, int value1Int,
        string? value2Str, int value2Int, bool expectedEqual)
    {
        // Arrange
        var value1 = new TestValueObject(value1Str!, value1Int);
        var value2 = new TestValueObject(value2Str!, value2Int);

        // Act
        var result = value1.Equals(value2);

        // Assert
        result.ShouldBe(expectedEqual);
        _output.WriteLine($"({value1Str}, {value1Int}) == ({value2Str}, {value2Int}): {result}");
    }

    [Fact]
    public void Equals_ComplexValueObjects_ComparesAllComponents()
    {
        // Arrange
        var date = DateTime.Now;
        var value1 = new ComplexValueObject("test", 42, 3.14m, date, true, "nullable");
        var value2 = new ComplexValueObject("test", 42, 3.14m, date, true, "nullable");
        var value3 = new ComplexValueObject("test", 42, 3.14m, date, true, null);

        // Act & Assert
        value1.Equals(value2).ShouldBeTrue();
        value1.Equals(value3).ShouldBeFalse();
        value2.Equals(value3).ShouldBeFalse();
    }

    [Fact]
    public void Equals_EmptyValueObjects_ReturnsTrue()
    {
        // Arrange
        var value1 = new EmptyValueObject();
        var value2 = new EmptyValueObject();

        // Act & Assert
        value1.Equals(value2).ShouldBeTrue();
        (value1 == value2).ShouldBeTrue();
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_SameValues_ReturnsSameHashCode()
    {
        // Arrange
        var value1 = new TestValueObject("test", 42);
        var value2 = new TestValueObject("test", 42);

        // Act
        var hash1 = value1.GetHashCode();
        var hash2 = value2.GetHashCode();

        // Assert
        hash1.ShouldBe(hash2);
        _output.WriteLine($"Hash codes for equal values: {hash1}");
    }

    [Fact]
    public void GetHashCode_DifferentValues_ReturnsDifferentHashCode()
    {
        // Arrange
        var value1 = new TestValueObject("test", 42);
        var value2 = new TestValueObject("test", 43);

        // Act
        var hash1 = value1.GetHashCode();
        var hash2 = value2.GetHashCode();

        // Assert
        hash1.ShouldNotBe(hash2);
        _output.WriteLine($"Different hash codes: {hash1} != {hash2}");
    }

    [Fact]
    public void GetHashCode_ConsistentAcrossMultipleCalls()
    {
        // Arrange
        var value = new TestValueObject("test", 42);

        // Act
        var hash1 = value.GetHashCode();
        var hash2 = value.GetHashCode();
        var hash3 = value.GetHashCode();

        // Assert
        hash1.ShouldBe(hash2);
        hash2.ShouldBe(hash3);
    }

    [Fact]
    public void GetHashCode_CachedAfterFirstCall()
    {
        // Arrange
        var value = new TestValueObject("test", 42);

        // Act - First call calculates and caches
        var firstHash = value.GetHashCode();

        // Act - Subsequent calls should return cached value
        var secondHash = value.GetHashCode();
        var thirdHash = value.GetHashCode();

        // Assert
        firstHash.ShouldBe(secondHash);
        secondHash.ShouldBe(thirdHash);
        _output.WriteLine($"Cached hash code: {firstHash}");
    }

    [Fact]
    public void GetHashCode_WithNullComponents_HandlesGracefully()
    {
        // Arrange - Use fixed DateTime to ensure consistent hashing
        var fixedDate = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var value1 = new ComplexValueObject("test", 42, 3.14m, fixedDate, true, null);
        var value2 = new ComplexValueObject("test", 42, 3.14m, fixedDate, true, null);

        // Act
        var hash1 = value1.GetHashCode();
        var hash2 = value2.GetHashCode();

        // Assert
        hash1.ShouldBe(hash2);
        hash1.ShouldNotBe(0); // Should not be zero even with null components
    }

    [Fact]
    public void GetHashCode_EmptyValueObject_ReturnsConsistentValue()
    {
        // Arrange
        var value1 = new EmptyValueObject();
        var value2 = new EmptyValueObject();

        // Act
        var hash1 = value1.GetHashCode();
        var hash2 = value2.GetHashCode();

        // Assert
        hash1.ShouldBe(hash2);
        hash1.ShouldBe(1); // Should be 1 for empty enumerable
    }

    #endregion

    #region Comparison Tests

    [Fact]
    public void CompareTo_SameValues_ReturnsZero()
    {
        // Arrange
        var value1 = new TestValueObject("test", 42);
        var value2 = new TestValueObject("test", 42);

        // Act
        var result = value1.CompareTo(value2);

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public void CompareTo_WithNull_ReturnsPositive()
    {
        // Arrange
        var value = new TestValueObject("test", 42);

        // Act
        var result = value.CompareTo(null);

        // Assert
        result.ShouldBe(1);
    }

    [Fact]
    public void CompareTo_DifferentTypes_ComparesByTypeName()
    {
        // Arrange
        var value1 = new TestValueObject("test", 42);
        var value2 = new DifferentTypeValueObject("test");

        // Act
        var result = value1.CompareTo(value2);

        // Assert
        // TestValueObject vs DifferentTypeValueObject - alphabetical by type name
        result.ShouldNotBe(0);
        _output.WriteLine($"Comparison result for different types: {result}");
    }

    [Theory]
    [InlineData("a", 1, "b", 1, -1)] // String comparison first
    [InlineData("test", 1, "test", 2, -1)] // Int comparison when strings equal
    [InlineData("test", 42, "test", 42, 0)] // Equal values
    [InlineData("z", 1, "a", 999, 1)] // String takes precedence
    public void CompareTo_ComparableValues_ReturnsExpectedResult(string str1, int int1, string str2, int int2, int expectedSign)
    {
        // Arrange
        var value1 = new TestValueObject(str1, int1);
        var value2 = new TestValueObject(str2, int2);

        // Act
        var result = value1.CompareTo(value2);

        // Assert
        Math.Sign(result).ShouldBe(expectedSign);
        _output.WriteLine($"({str1}, {int1}) compared to ({str2}, {int2}): {result}");
    }

    [Fact]
    public void ComparisonOperators_WorkCorrectly()
    {
        // Arrange
        var value1 = new TestValueObject("a", 1);
        var value2 = new TestValueObject("b", 1);
        var value3 = new TestValueObject("a", 1);

        // Act & Assert
        (value1 < value2).ShouldBeTrue();
        (value1 <= value2).ShouldBeTrue();
        (value1 <= value3).ShouldBeTrue();
        (value2 > value1).ShouldBeTrue();
        (value2 >= value1).ShouldBeTrue();
        (value3 >= value1).ShouldBeTrue();

        (value1 == value3).ShouldBeTrue();
        (value1 != value2).ShouldBeTrue();
    }

    [Fact]
    public void CompareTo_DifferentLengthComponents_ComparesCorrectly()
    {
        // Arrange
        var singleValue = new SingleValueObject("test");
        var doubleValue = new TestValueObject("test", 42);

        // Act
        var result = singleValue.CompareTo(doubleValue);

        // Assert
        result.ShouldNotBe(0); // Different number of components
        _output.WriteLine($"Single vs double component comparison: {result}");
    }

    [Fact]
    public void CompareTo_NonComparableComponents_FallsBackToStringComparison()
    {
        // Arrange
        var date1 = DateTime.Now;
        var date2 = date1.AddDays(1);
        var value1 = new ComplexValueObject("test", 42, 3.14m, date1, true);
        var value2 = new ComplexValueObject("test", 42, 3.14m, date2, true);

        // Act
        var result = value1.CompareTo(value2);

        // Assert
        result.ShouldNotBe(0); // Different dates should produce different comparison
        _output.WriteLine($"Date comparison result: {result}");
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var value = new TestValueObject("test", 42);

        // Act
        var result = value.ToString();

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("TestValueObject");
        result.ShouldContain("test");
        result.ShouldContain("42");
        _output.WriteLine($"ToString result: {result}");
    }

    [Fact]
    public void ToString_WithNullComponents_HandlesGracefully()
    {
        // Arrange
        var value = new ComplexValueObject("test", 42, 3.14m, DateTime.Now, true, null);

        // Act
        var result = value.ToString();

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("null");
        _output.WriteLine($"ToString with null: {result}");
    }

    [Fact]
    public void ToString_EmptyValueObject_ReturnsTypeName()
    {
        // Arrange
        var value = new EmptyValueObject();

        // Act
        var result = value.ToString();

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("EmptyValueObject");
        _output.WriteLine($"Empty value object toString: {result}");
    }

    #endregion

    #region ValueObjectExtensions Tests

    [Fact]
    public void ToEqualityComponents_WithClass_ReturnsAllProperties()
    {
        // Arrange
        var recordLike = new RecordLikeValueObject("test", 42);

        // Act
        var components = recordLike.ToEqualityComponents().ToArray();

        // Assert
        components.Length.ShouldBe(2);
        components[0].ShouldBe("test");
        components[1].ShouldBe(42);
        _output.WriteLine($"Record-like equality components: [{string.Join(", ", components)}]");
    }

    [Fact]
    public void RecordLikeValueObject_EqualityWorks()
    {
        // Arrange
        var record1 = new RecordLikeValueObject("test", 42);
        var record2 = new RecordLikeValueObject("test", 42);
        var record3 = new RecordLikeValueObject("different", 42);

        // Act & Assert
        record1.Equals(record2).ShouldBeTrue();
        record1.Equals(record3).ShouldBeFalse();
        record1.GetHashCode().ShouldBe(record2.GetHashCode());
        record1.GetHashCode().ShouldNotBe(record3.GetHashCode());
    }

    #endregion

    #region Proxy Type Tests

    [Fact]
    public void GetUnproxiedType_RegularType_ReturnsSameType()
    {
        // Arrange
        var value = new TestValueObject("test", 42);

        // Act
        var type = ValueObject.GetUnproxiedType(value);

        // Assert
        type.ShouldBe(typeof(TestValueObject));
    }

    [Fact]
    public void GetUnproxiedType_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => ValueObject.GetUnproxiedType(null!));
    }

    #endregion

    #region Integration and Performance Tests

    [Fact]
    public void ValueObject_InHashSet_BehavesCorrectly()
    {
        // Arrange
        var set = new HashSet<TestValueObject>();
        var value1 = new TestValueObject("test", 42);
        var value2 = new TestValueObject("test", 42); // Same content
        var value3 = new TestValueObject("different", 42);

        // Act
        set.Add(value1);
        set.Add(value2); // Should not be added (equal to value1)
        set.Add(value3);

        // Assert
        set.Count.ShouldBe(2);
        set.ShouldContain(value1);
        set.ShouldContain(value2); // Contains should work with equal values
        set.ShouldContain(value3);
        _output.WriteLine($"HashSet contains {set.Count} unique value objects");
    }

    [Fact]
    public void ValueObject_InDictionary_UsesCorrectKeyEquality()
    {
        // Arrange
        var dict = new Dictionary<TestValueObject, string>();
        var key1 = new TestValueObject("test", 42);
        var key2 = new TestValueObject("test", 42); // Same content
        var key3 = new TestValueObject("different", 42);

        // Act
        dict[key1] = "value1";
        dict[key2] = "value2"; // Should overwrite value1
        dict[key3] = "value3";

        // Assert
        dict.Count.ShouldBe(2);
        dict[key1].ShouldBe("value2"); // Overwritten by key2
        dict[key2].ShouldBe("value2");
        dict[key3].ShouldBe("value3");
    }

    [Fact]
    public void ValueObject_ComplexWorkflow_MaintainsIntegrity()
    {
        // Arrange
        var values = new List<TestValueObject>
        {
            new("a", 1),
            new("b", 2),
            new("a", 1), // Duplicate
            new("c", 3),
            new("b", 2)  // Duplicate
        };

        // Act - Remove duplicates and sort
        var uniqueValues = values.Distinct().OrderBy(v => v).ToArray();

        // Assert
        uniqueValues.Length.ShouldBe(3);
        uniqueValues[0].ShouldBe(new TestValueObject("a", 1));
        uniqueValues[1].ShouldBe(new TestValueObject("b", 2));
        uniqueValues[2].ShouldBe(new TestValueObject("c", 3));

        _output.WriteLine($"Processed {values.Count} values into {uniqueValues.Length} unique, sorted values");
    }

    [Fact]
    public void ValueObject_MassEquality_PerformsWell()
    {
        // Arrange
        const int count = 1000;
        var values = Enumerable.Range(0, count)
            .Select(i => new TestValueObject($"test{i % 100}", i % 100))
            .ToArray();

        // Act - Perform many equality operations
        var equalityResults = new bool[count * count];
        var index = 0;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (var i = 0; i < count; i++)
        {
            for (var j = 0; j < count; j++)
            {
                equalityResults[index++] = values[i].Equals(values[j]);
            }
        }

        stopwatch.Stop();

        // Assert
        var equalCount = equalityResults.Count(r => r);
        equalCount.ShouldBeGreaterThan(count); // Should have many equals due to modulo

        _output.WriteLine($"Performed {count * count} equality checks in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Found {equalCount} equal pairs out of {count * count} total comparisons");
    }

    #endregion
}
