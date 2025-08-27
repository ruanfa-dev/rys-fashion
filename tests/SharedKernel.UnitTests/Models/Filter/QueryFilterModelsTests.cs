using Shouldly;
using SharedKernel.Models.Filter;

namespace SharedKernel.UnitTests.Models.Filter;

/// <summary>
/// Unit tests for QueryFilterParameter model validation and functionality.
/// </summary>
public sealed class QueryFilterParameterTests
{
    [Fact]
    public void Validate_ValidParameter_DoesNotThrow()
    {
        // Arrange
        var parameter = new QueryFilterParameter
        {
            Field = "name",
            Operator = FilterOperator.Equal,
            Value = "test"
        };

        // Act & Assert
        Should.NotThrow(() => parameter.Validate());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_InvalidField_ThrowsArgumentException(string field)
    {
        // Arrange
        var parameter = new QueryFilterParameter
        {
            Field = field,
            Operator = FilterOperator.Equal,
            Value = "test"
        };

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => parameter.Validate());
        exception.ParamName.ShouldBe("Field");
    }

    [Fact]
    public void Validate_NullField_ThrowsArgumentException()
    {
        // Arrange
        var parameter = new QueryFilterParameter
        {
            Field = null!,
            Operator = FilterOperator.Equal,
            Value = "test"
        };

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => parameter.Validate());
        exception.ParamName.ShouldBe("Field");
    }

    [Fact]
    public void Validate_IsNullOperator_DoesNotRequireValue()
    {
        // Arrange
        var parameter = new QueryFilterParameter
        {
            Field = "field",
            Operator = FilterOperator.IsNull,
            Value = string.Empty
        };

        // Act & Assert
        Should.NotThrow(() => parameter.Validate());
    }

    [Fact]
    public void Validate_IsNotNullOperator_DoesNotRequireValue()
    {
        // Arrange
        var parameter = new QueryFilterParameter
        {
            Field = "field",
            Operator = FilterOperator.IsNotNull,
            Value = string.Empty
        };

        // Act & Assert
        Should.NotThrow(() => parameter.Validate());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_NonNullOperatorWithEmptyValue_ThrowsArgumentException(string value)
    {
        // Arrange
        var parameter = new QueryFilterParameter
        {
            Field = "field",
            Operator = FilterOperator.Equal,
            Value = value
        };

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => parameter.Validate());
        exception.ParamName.ShouldBe("Value");
    }

    [Fact]
    public void Validate_NonNullOperatorWithNullValue_ThrowsArgumentException()
    {
        // Arrange
        var parameter = new QueryFilterParameter
        {
            Field = "field",
            Operator = FilterOperator.Equal,
            Value = null!
        };

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => parameter.Validate());
        exception.ParamName.ShouldBe("Value");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_InOperatorWithEmptyValues_ThrowsArgumentException(string value)
    {
        // Arrange
        var parameter = new QueryFilterParameter
        {
            Field = "field",
            Operator = FilterOperator.In,
            Value = value,
            Values = null
        };

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => parameter.Validate());
        exception.ParamName.ShouldBe("Value");
    }

    [Fact]
    public void Validate_InOperatorWithNullValues_ThrowsArgumentException()
    {
        // Arrange
        var parameter = new QueryFilterParameter
        {
            Field = "field",
            Operator = FilterOperator.In,
            Value = null!,
            Values = null
        };

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => parameter.Validate());
        exception.ParamName.ShouldBe("Value");
    }

    [Fact]
    public void Validate_InOperatorWithValidValues_DoesNotThrow()
    {
        // Arrange
        var parameter = new QueryFilterParameter
        {
            Field = "field",
            Operator = FilterOperator.In,
            Value = "value1,value2,value3"
        };

        // Act & Assert
        Should.NotThrow(() => parameter.Validate());
    }

    [Fact]
    public void Validate_InOperatorWithValuesProperty_DoesNotThrow()
    {
        // Arrange
        var parameter = new QueryFilterParameter
        {
            Field = "field",
            Operator = FilterOperator.In,
            Value = string.Empty,
            Values = "value1,value2,value3"
        };

        // Act & Assert
        Should.NotThrow(() => parameter.Validate());
    }

    [Theory]
    [InlineData("single-value")]
    [InlineData("")]
    public void Validate_RangeOperatorWithInvalidFormat_ThrowsArgumentException(string value)
    {
        // Arrange
        var parameter = new QueryFilterParameter
        {
            Field = "field",
            Operator = FilterOperator.Range,
            Value = value
        };

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => parameter.Validate());
        exception.ParamName.ShouldBe("Value");
    }

    [Fact]
    public void Validate_RangeOperatorWithNullValue_ThrowsArgumentException()
    {
        // Arrange
        var parameter = new QueryFilterParameter
        {
            Field = "field",
            Operator = FilterOperator.Range,
            Value = null!
        };

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => parameter.Validate());
        exception.ParamName.ShouldBe("Value");
    }

    [Fact]
    public void Validate_RangeOperatorWithValidFormat_DoesNotThrow()
    {
        // Arrange
        var parameter = new QueryFilterParameter
        {
            Field = "field",
            Operator = FilterOperator.Range,
            Value = "1,10"
        };

        // Act & Assert
        Should.NotThrow(() => parameter.Validate());
    }

    [Fact]
    public void DefaultValues_SetCorrectly()
    {
        // Arrange & Act
        var parameter = new QueryFilterParameter
        {
            Field = "test",
            Value = "value"
        };

        // Assert
        parameter.LogicalOperator.ShouldBe(FilterLogicalOperator.All);
        parameter.Group.ShouldBeNull();
        parameter.Values.ShouldBeNull();
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var parameter = new QueryFilterParameter
        {
            Field = "name",
            Operator = FilterOperator.Contains,
            Value = "test",
            Values = "test1,test2",
            LogicalOperator = FilterLogicalOperator.Any,
            Group = 5
        };

        // Assert
        parameter.Field.ShouldBe("name");
        parameter.Operator.ShouldBe(FilterOperator.Contains);
        parameter.Value.ShouldBe("test");
        parameter.Values.ShouldBe("test1,test2");
        parameter.LogicalOperator.ShouldBe(FilterLogicalOperator.Any);
        parameter.Group.ShouldBe(5);
    }
}

/// <summary>
/// Unit tests for QueryFilterGroup model validation and functionality.
/// </summary>
public sealed class QueryFilterGroupTests
{
    [Fact]
    public void Validate_EmptyGroup_ThrowsArgumentException()
    {
        // Arrange
        var group = new QueryFilterGroup();

        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => group.Validate());
        exception.ParamName.ShouldBe("Filters");
    }

    [Fact]
    public void Validate_GroupWithFilters_DoesNotThrow()
    {
        // Arrange
        var group = new QueryFilterGroup
        {
            Filters =
            [
                new QueryFilterParameter { Field = "name", Value = "test" }
            ]
        };

        // Act & Assert
        Should.NotThrow(() => group.Validate());
    }

    [Fact]
    public void Validate_GroupWithSubGroups_DoesNotThrow()
    {
        // Arrange
        var group = new QueryFilterGroup
        {
            SubGroups =
            [
                new QueryFilterGroup
                {
                    Filters = [new QueryFilterParameter { Field = "name", Value = "test" }]
                }
            ]
        };

        // Act & Assert
        Should.NotThrow(() => group.Validate());
    }

    [Fact]
    public void Validate_InvalidFilterInGroup_ThrowsArgumentException()
    {
        // Arrange
        var group = new QueryFilterGroup
        {
            Filters =
            [
                new QueryFilterParameter { Field = "", Value = "test" } // Invalid field
            ]
        };

        // Act & Assert
        Should.Throw<ArgumentException>(() => group.Validate());
    }

    [Fact]
    public void Validate_InvalidSubGroup_ThrowsArgumentException()
    {
        // Arrange
        var group = new QueryFilterGroup
        {
            SubGroups =
            [
                new QueryFilterGroup() // Empty subgroup
            ]
        };

        // Act & Assert
        Should.Throw<ArgumentException>(() => group.Validate());
    }

    [Fact]
    public void GetTotalFilterCount_EmptyGroup_ReturnsZero()
    {
        // Arrange
        var group = new QueryFilterGroup();

        // Act
        var count = group.GetTotalFilterCount();

        // Assert
        count.ShouldBe(0);
    }

    [Fact]
    public void GetTotalFilterCount_GroupWithFilters_ReturnsCorrectCount()
    {
        // Arrange
        var group = new QueryFilterGroup
        {
            Filters =
            [
                new QueryFilterParameter { Field = "name1", Value = "test1" },
                new QueryFilterParameter { Field = "name2", Value = "test2" }
            ]
        };

        // Act
        var count = group.GetTotalFilterCount();

        // Assert
        count.ShouldBe(2);
    }

    [Fact]
    public void GetTotalFilterCount_GroupWithSubGroups_ReturnsCorrectCount()
    {
        // Arrange
        var group = new QueryFilterGroup
        {
            Filters =
            [
                new QueryFilterParameter { Field = "name1", Value = "test1" }
            ],
            SubGroups =
            [
                new QueryFilterGroup
                {
                    Filters =
                    [
                        new QueryFilterParameter { Field = "name2", Value = "test2" },
                        new QueryFilterParameter { Field = "name3", Value = "test3" }
                    ]
                }
            ]
        };

        // Act
        var count = group.GetTotalFilterCount();

        // Assert
        count.ShouldBe(3); // 1 + 2
    }

    [Fact]
    public void GetTotalFilterCount_NestedSubGroups_ReturnsCorrectCount()
    {
        // Arrange
        var group = new QueryFilterGroup
        {
            Filters =
            [
                new QueryFilterParameter { Field = "name1", Value = "test1" }
            ],
            SubGroups =
            [
                new QueryFilterGroup
                {
                    Filters =
                    [
                        new QueryFilterParameter { Field = "name2", Value = "test2" }
                    ],
                    SubGroups =
                    [
                        new QueryFilterGroup
                        {
                            Filters =
                            [
                                new QueryFilterParameter { Field = "name3", Value = "test3" },
                                new QueryFilterParameter { Field = "name4", Value = "test4" }
                            ]
                        }
                    ]
                }
            ]
        };

        // Act
        var count = group.GetTotalFilterCount();

        // Assert
        count.ShouldBe(4); // 1 + 1 + 2
    }

    [Fact]
    public void GetAllFilters_EmptyGroup_ReturnsEmptyList()
    {
        // Arrange
        var group = new QueryFilterGroup();

        // Act
        var allFilters = group.GetAllFilters();

        // Assert
        allFilters.ShouldBeEmpty();
    }

    [Fact]
    public void GetAllFilters_GroupWithFilters_ReturnsAllFilters()
    {
        // Arrange
        var filter1 = new QueryFilterParameter { Field = "name1", Value = "test1" };
        var filter2 = new QueryFilterParameter { Field = "name2", Value = "test2" };

        var group = new QueryFilterGroup
        {
            Filters = [filter1, filter2]
        };

        // Act
        var allFilters = group.GetAllFilters();

        // Assert
        allFilters.Count.ShouldBe(2);
        allFilters.ShouldContain(filter1);
        allFilters.ShouldContain(filter2);
    }

    [Fact]
    public void GetAllFilters_GroupWithSubGroups_ReturnsAllFiltersFlattened()
    {
        // Arrange
        var filter1 = new QueryFilterParameter { Field = "name1", Value = "test1" };
        var filter2 = new QueryFilterParameter { Field = "name2", Value = "test2" };
        var filter3 = new QueryFilterParameter { Field = "name3", Value = "test3" };

        var group = new QueryFilterGroup
        {
            Filters = [filter1],
            SubGroups =
            [
                new QueryFilterGroup
                {
                    Filters = [filter2, filter3]
                }
            ]
        };

        // Act
        var allFilters = group.GetAllFilters();

        // Assert
        allFilters.Count.ShouldBe(3);
        allFilters.ShouldContain(filter1);
        allFilters.ShouldContain(filter2);
        allFilters.ShouldContain(filter3);
    }

    [Fact]
    public void DefaultValues_SetCorrectly()
    {
        // Arrange & Act
        var group = new QueryFilterGroup();

        // Assert
        group.Filters.ShouldNotBeNull();
        group.Filters.ShouldBeEmpty();
        group.SubGroups.ShouldNotBeNull();
        group.SubGroups.ShouldBeEmpty();
        group.LogicalOperator.ShouldBe(FilterLogicalOperator.All);
        group.GroupId.ShouldBe(0);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var filter = new QueryFilterParameter { Field = "test", Value = "value" };
        var subGroup = new QueryFilterGroup { Filters = [filter] };

        var group = new QueryFilterGroup
        {
            Filters = [filter],
            SubGroups = [subGroup],
            LogicalOperator = FilterLogicalOperator.Any,
            GroupId = 42
        };

        // Assert
        group.Filters.Count.ShouldBe(1);
        group.Filters[0].ShouldBe(filter);
        group.SubGroups.Count.ShouldBe(1);
        group.SubGroups[0].ShouldBe(subGroup);
        group.LogicalOperator.ShouldBe(FilterLogicalOperator.Any);
        group.GroupId.ShouldBe(42);
    }
}
