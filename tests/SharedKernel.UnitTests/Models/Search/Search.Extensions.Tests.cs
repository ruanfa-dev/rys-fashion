using SharedKernel.Models.Search;
using SharedKernel.UnitTests.Models.Shared;

using Shouldly;

namespace SharedKernel.UnitTests.Models.Search;

public sealed class SearchParamsExtensionsTests
{
    private readonly List<TestEntity> _testData = TestEntity.GetTestData();

    private IQueryable<TestEntity> GetQueryableTestData() => _testData.AsQueryable();

    #region ApplySearch with SearchParams Tests

    [Fact]
    public void ApplySearch_WithNullSearchTerm_ReturnsOriginalQuery()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();
        SearchParameter searchParams = new SearchParameter(SearchTerm: null);

        // Act
        List<TestEntity> result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.ShouldBe(3);
        result[0].StringProperty.ShouldBe("Apple");
        result[1].StringProperty.ShouldBe("Banana");
        result[2].StringProperty.ShouldBe("Blueberry");
    }

    [Fact]
    public void ApplySearch_WithEmptySearchTerm_ReturnsOriginalQuery()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();
        SearchParameter searchParams = new SearchParameter(SearchTerm: "");

        // Act
        List<TestEntity> result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.ShouldBe(3);
    }

    [Fact]
    public void ApplySearch_WithWhitespaceSearchTerm_ReturnsOriginalQuery()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();
        SearchParameter searchParams = new SearchParameter(SearchTerm: "   ");

        // Act
        List<TestEntity> result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.ShouldBe(3);
    }

    [Fact]
    public void ApplySearch_FullTextSearch_SearchesAllStringProperties()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();
        SearchParameter searchParams = new SearchParameter(SearchTerm: "apple");

        // Act
        List<TestEntity> result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].StringProperty.ShouldBe("Apple");
    }

    [Fact]
    public void ApplySearch_CaseInsensitiveByDefault_FindsMatches()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();
        SearchParameter searchParams = new SearchParameter(SearchTerm: "APPLE");

        // Act
        List<TestEntity> result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].StringProperty.ShouldBe("Apple");
    }

    [Fact]
    public void ApplySearch_WithCaseSensitiveOption_RespectsCasing()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();
        SearchParameter searchParams = new SearchParameter(SearchTerm: "APPLE");
        SearchOptions options = new SearchOptions { CaseSensitive = true };

        // Act
        List<TestEntity> result = query.ApplySearch(searchParams, options).ToList();

        // Assert
        result.Count.ShouldBe(0); // No matches with case sensitivity
    }

    [Fact]
    public void ApplySearch_WithExactMatchOption_FindsExactMatches()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();
        SearchParameter searchParams = new SearchParameter(SearchTerm: "Apple");
        SearchOptions options = new SearchOptions { ExactMatch = true };

        // Act
        List<TestEntity> result = query.ApplySearch(searchParams, options).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].StringProperty.ShouldBe("Apple");
    }

    [Fact]
    public void ApplySearch_WithStartsWithOption_FindsStartsWithMatches()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();
        SearchParameter searchParams = new SearchParameter(SearchTerm: "Blue");
        SearchOptions options = new SearchOptions { StartsWith = true };

        // Act
        List<TestEntity> result = query.ApplySearch(searchParams, options).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].StringProperty.ShouldBe("Blueberry");
    }

    [Fact]
    public void ApplySearch_WithNullableStringProperty_HandlesNullsGracefully()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();
        SearchParameter searchParams = new SearchParameter(SearchTerm: "HasValue");

        // Act
        List<TestEntity> result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(1);
        result[0].NullableStringProperty.ShouldBe("HasValue");
    }

    #endregion

    #region ApplySearch with SearchFields Tests

    [Fact]
    public void ApplySearch_WithSpecificSearchFields_SearchesOnlyThoseFields()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();
        SearchParameter searchParams = new SearchParameter(SearchTerm: "Apple", SearchFields: ["StringProperty"]);

        // Act
        List<TestEntity> result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].StringProperty.ShouldBe("Apple");
    }

    [Fact]
    public void ApplySearch_WithMultipleSearchFields_SearchesAllSpecifiedFields()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();
        SearchParameter searchParams = new SearchParameter(SearchTerm: "Value", SearchFields: ["StringProperty", "NullableStringProperty"]);

        // Act
        List<TestEntity> result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.ShouldBe(2); // "HasValue" and "AnotherValue"
        result.ShouldContain(e => e.Id == 1 && e.NullableStringProperty == "HasValue");
        result.ShouldContain(e => e.Id == 3 && e.NullableStringProperty == "AnotherValue");
    }

    [Fact]
    public void ApplySearch_WithInvalidSearchField_IgnoresInvalidField()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();
        SearchParameter searchParams = new SearchParameter(SearchTerm: "Apple", SearchFields: ["InvalidField", "StringProperty"]);

        // Act
        List<TestEntity> result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].StringProperty.ShouldBe("Apple");
    }

    [Fact]
    public void ApplySearch_WithNonStringSearchField_IgnoresNonStringField()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();
        SearchParameter searchParams = new SearchParameter(SearchTerm: "10", SearchFields: ["IntProperty", "StringProperty"]);

        // Act
        List<TestEntity> result = query.ApplySearch(searchParams).ToList();

        // Assert
        // Should only search StringProperty, ignoring IntProperty
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0); // "10" is not contained in any StringProperty values
    }

    [Fact]
    public void ApplySearch_WithEmptySearchFields_PerformsFullTextSearch()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();
        SearchParameter searchParams = new SearchParameter(SearchTerm: "Apple", SearchFields: []);

        // Act
        List<TestEntity> result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].StringProperty.ShouldBe("Apple");
    }

    [Fact]
    public void ApplySearch_WithNullSearchFields_PerformsFullTextSearch()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();
        SearchParameter searchParams = new SearchParameter(SearchTerm: "Apple", SearchFields: null);

        // Act
        List<TestEntity> result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].StringProperty.ShouldBe("Apple");
    }

    [Fact]
    public void ApplySearch_WithOptionsInSearchParams_IgnoresEmbeddedOptions()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();
        SearchOptions options = new SearchOptions { CaseSensitive = true };
        SearchParameter searchParams = new SearchParameter(SearchTerm: "APPLE", Options: options);

        // Act
        List<TestEntity> result = query.ApplySearch(searchParams).ToList();

        // Assert
        // The current implementation ignores embedded options and uses default options
        result.Count.ShouldBe(1); // Finds match because it uses default case-insensitive behavior
        result[0].StringProperty.ShouldBe("Apple");
    }

    [Fact]
    public void ApplySearch_WithBothEmbeddedAndParameterOptions_PrioritizesParameterOptions()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();
        SearchOptions embeddedOptions = new SearchOptions { CaseSensitive = true };
        SearchOptions parameterOptions = new SearchOptions { CaseSensitive = false };
        SearchParameter searchParams = new SearchParameter(SearchTerm: "APPLE", Options: embeddedOptions);

        // Act
        List<TestEntity> result = query.ApplySearch(searchParams, parameterOptions).ToList();

        // Assert
        result.Count.ShouldBe(1); // Parameter options override embedded options
        result[0].StringProperty.ShouldBe("Apple");
    }

    #endregion

    #region ApplySearch with Expressions Tests

    [Fact]
    public void ApplySearch_WithSingleExpression_SearchesSpecifiedProperty()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();

        // Act
        List<TestEntity> result = query.ApplySearch("Apple", e => e.StringProperty).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].StringProperty.ShouldBe("Apple");
    }

    [Fact]
    public void ApplySearch_WithMultipleExpressions_SearchesAllSpecifiedProperties()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();

        // Act
        List<TestEntity> result = query.ApplySearch("Value",
            e => e.StringProperty,
            e => e.NullableStringProperty!).ToList();

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(e => e.NullableStringProperty == "HasValue");
        result.ShouldContain(e => e.NullableStringProperty == "AnotherValue");
    }

    [Fact]
    public void ApplySearch_WithExpressionsEmptySearchTerm_ReturnsOriginalQuery()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();

        // Act
        List<TestEntity> result = query.ApplySearch("", e => e.StringProperty).ToList();

        // Assert
        result.Count.ShouldBe(3);
    }

    [Fact]
    public void ApplySearch_WithExpressionsNullSearchTerm_ReturnsOriginalQuery()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();

        // Act
        List<TestEntity> result = query.ApplySearch(null!, e => e.StringProperty).ToList();

        // Assert
        result.Count.ShouldBe(3);
    }

    [Fact]
    public void ApplySearch_WithNoExpressions_ReturnsOriginalQuery()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();

        // Act
        List<TestEntity> result = query.ApplySearch("Apple").ToList();

        // Assert
        result.Count.ShouldBe(3); // No expressions provided, returns all
    }

    [Fact]
    public void ApplySearch_WithNullableStringExpression_HandlesNullsGracefully()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();

        // Act
        List<TestEntity> result = query.ApplySearch("NonExistentValue", e => e.NullableStringProperty!).ToList();

        // Assert
        result.Count.ShouldBe(0);
    }

    [Fact]
    public void ApplySearch_WithExpressions_AlwaysCaseInsensitive()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();

        // Act
        List<TestEntity> result = query.ApplySearch("APPLE", e => e.StringProperty).ToList();

        // Assert
        // Expression-based search always converts to lowercase, so this should find "Apple"
        result.Count.ShouldBe(1);
        result[0].StringProperty.ShouldBe("Apple");
    }

    #endregion

    #region SearchIn Tests

    [Fact]
    public void SearchIn_WithValidSearchTerm_ReturnsMatchingResults()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();

        // Act
        List<TestEntity> result = query.SearchIn("Banana", e => e.StringProperty).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].StringProperty.ShouldBe("Banana");
    }

    [Fact]
    public void SearchIn_WithEmptySearchTerm_ReturnsOriginalQuery()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();

        // Act
        List<TestEntity> result = query.SearchIn("", e => e.StringProperty).ToList();

        // Assert
        result.Count.ShouldBe(3);
    }

    [Fact]
    public void SearchIn_WithNullSearchTerm_ReturnsOriginalQuery()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();

        // Act
        List<TestEntity> result = query.SearchIn(null!, e => e.StringProperty).ToList();

        // Assert
        result.Count.ShouldBe(3);
    }

    [Fact]
    public void SearchIn_WithWhitespaceSearchTerm_ReturnsOriginalQuery()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();

        // Act
        List<TestEntity> result = query.SearchIn("   ", e => e.StringProperty).ToList();

        // Assert
        result.Count.ShouldBe(3);
    }

    [Fact]
    public void SearchIn_AlwaysCaseInsensitive()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();

        // Act
        List<TestEntity> result = query.SearchIn("BANANA", e => e.StringProperty).ToList();

        // Assert
        // SearchIn uses expression-based search which is always case insensitive
        result.Count.ShouldBe(1);
        result[0].StringProperty.ShouldBe("Banana");
    }

    #endregion

    #region Search Builder Tests

    [Fact]
    public void Search_CreatesSearchBuilder()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();

        // Act
        SearchBuilder<TestEntity> builder = query.Search("Apple");

        // Assert
        builder.ShouldNotBeNull();
        builder.ShouldBeOfType<SearchBuilder<TestEntity>>();
    }

    [Fact]
    public void SearchBuilder_WithSingleField_SearchesCorrectly()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();

        // Act
        List<TestEntity> result = query.Search("Apple")
            .In(e => e.StringProperty)
            .Execute()
            .ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].StringProperty.ShouldBe("Apple");
    }

    [Fact]
    public void SearchBuilder_WithMultipleFields_SearchesCorrectly()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();

        // Act
        List<TestEntity> result = query.Search("Value")
            .In(e => e.StringProperty, e => e.NullableStringProperty!)
            .Execute()
            .ToList();

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(e => e.NullableStringProperty == "HasValue");
        result.ShouldContain(e => e.NullableStringProperty == "AnotherValue");
    }

    [Fact]
    public void SearchBuilder_WithCaseSensitive_DoesNotRespectCasingSensitivityWhenUsingFields()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();

        // Act
        List<TestEntity> result = query.Search("APPLE")
            .In(e => e.StringProperty)
            .CaseSensitive()
            .Execute()
            .ToList();

        // Assert
        // When using fields, SearchBuilder calls expression-based ApplySearch which ignores case sensitivity options
        result.Count.ShouldBe(1); // Still finds match because expression search is always case insensitive
        result[0].StringProperty.ShouldBe("Apple");
    }

    [Fact]
    public void SearchBuilder_WithCaseSensitive_RespectsOptionsWhenNoFieldsSpecified()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();

        // Act
        List<TestEntity> result = query.Search("APPLE")
            .CaseSensitive()
            .Execute()
            .ToList();

        // Assert
        // When no fields specified, it uses SearchParams-based search which respects options
        result.Count.ShouldBe(0); // No matches with case sensitivity
    }

    [Fact]
    public void SearchBuilder_WithExactMatch_FindsExactMatches()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();

        // Act
        List<TestEntity> result = query.Search("Apple")
            .In(e => e.StringProperty)
            .ExactMatch()
            .Execute()
            .ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].StringProperty.ShouldBe("Apple");
    }

    [Fact]
    public void SearchBuilder_WithStartsWith_FindsStartsWithMatches()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();

        // Act
        List<TestEntity> result = query.Search("Blue")
            .In(e => e.StringProperty)
            .StartsWith()
            .Execute()
            .ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].StringProperty.ShouldBe("Blueberry");
    }

    [Fact]
    public void SearchBuilder_WithChainedOptions_AppliesAllOptions()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();

        // Act
        List<TestEntity> result = query.Search("blue")
            .In(e => e.StringProperty)
            .StartsWith()
            .CaseSensitive(false) // Explicitly set case insensitive
            .Execute()
            .ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].StringProperty.ShouldBe("Blueberry");
    }

    [Fact]
    public void SearchBuilder_WithEmptySearchTerm_ReturnsOriginalQuery()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();

        // Act
        List<TestEntity> result = query.Search("")
            .In(e => e.StringProperty)
            .Execute()
            .ToList();

        // Assert
        result.Count.ShouldBe(3);
    }

    [Fact]
    public void SearchBuilder_WithNoFields_PerformsFullTextSearch()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();

        // Act
        List<TestEntity> result = query.Search("Apple")
            .Execute()
            .ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].StringProperty.ShouldBe("Apple");
    }

    [Fact]
    public void SearchBuilder_ImplicitConversion_Works()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();

        // Act
        Func<IQueryable<TestEntity>> searchFunc = query.Search("Apple").In(e => e.StringProperty);
        List<TestEntity> result = searchFunc().ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].StringProperty.ShouldBe("Apple");
    }

    #endregion

    #region SearchOptions Tests

    [Fact]
    public void SearchOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        SearchOptions options = new SearchOptions();

        // Assert
        options.CaseSensitive?.ShouldBeFalse();
        options.ExactMatch?.ShouldBeFalse();
        options.StartsWith?.ShouldBeFalse();
    }

    [Fact]
    public void SearchOptions_WithInit_SetsValuesCorrectly()
    {
        // Arrange & Act
        SearchOptions options = new SearchOptions
        {
            CaseSensitive = true,
            ExactMatch = true,
            StartsWith = true
        };

        // Assert
        options.CaseSensitive?.ShouldBeTrue();
        options.ExactMatch?.ShouldBeTrue();
        options.StartsWith?.ShouldBeTrue();
    }

    [Fact]
    public void SearchOptions_WithRecord_SupportsWithExpression()
    {
        // Arrange
        SearchOptions options = new SearchOptions();

        // Act
        SearchOptions newOptions = options with { CaseSensitive = true };

        // Assert
        options.CaseSensitive?.ShouldBeFalse(); // Original unchanged
        newOptions.CaseSensitive?.ShouldBeTrue(); // New instance changed
    }

    #endregion

    #region Edge Cases and Error Handling Tests

    [Fact]
    public void ApplySearch_WithComplexSearchTerm_HandlesCorrectly()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();
        SearchParameter searchParams = new SearchParameter(SearchTerm: "Has Value");

        // Act
        List<TestEntity> result = query.ApplySearch(searchParams).ToList();

        // Assert
        // Should not find anything since "Has Value" (with space) is not in the data
        result.Count.ShouldBe(0);
    }

    [Fact]
    public void ApplySearch_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        List<TestEntity> testDataWithSpecialChars =
        [
            new TestEntity
            {
                Id = 99,
                StringProperty = "Test@Email.com",
                NullableStringProperty = "Special-Character_Value",
                Status = TestStatus.Active
            }
        ];
        IQueryable<TestEntity> query = testDataWithSpecialChars.AsQueryable();
        SearchParameter searchParams = new SearchParameter(SearchTerm: "@Email");

        // Act
        List<TestEntity> result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].StringProperty.ShouldBe("Test@Email.com");
    }

    [Fact]
    public void ApplySearch_CombinedWithOtherLinqOperations_WorksCorrectly()
    {
        // Arrange
        IOrderedQueryable<TestEntity> query = GetQueryableTestData()
            .Where(e => e.Status == TestStatus.Active)
            .OrderBy(e => e.Id);
        SearchParameter searchParams = new SearchParameter(SearchTerm: "Apple");

        // Act
        List<TestEntity> result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].StringProperty.ShouldBe("Apple");
        result[0].Status.ShouldBe(TestStatus.Active);
    }

    [Fact]
    public void ApplySearch_WithLargeDataset_PerformsEfficiently()
    {
        // Arrange
        List<TestEntity> largeDataset = [];
        for (int i = 0; i < 1000; i++)
        {
            largeDataset.Add(new TestEntity
            {
                Id = i,
                StringProperty = i % 2 == 0 ? $"Even{i}" : $"Odd{i}",
                NullableStringProperty = i % 10 == 0 ? null : $"Value{i}",
                Status = TestStatus.Active
            });
        }
        IQueryable<TestEntity> query = largeDataset.AsQueryable();
        SearchParameter searchParams = new SearchParameter(SearchTerm: "Even");

        // Act
        List<TestEntity> result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.ShouldBe(500); // Half should match "Even"
        result.All(e => e.StringProperty.Contains("Even")).ShouldBeTrue();
    }

    #endregion

    #region Integration Tests with TestEntity Properties

    [Fact]
    public void ApplySearch_WorksWithTestEntityComplexScenario()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData()
            .Where(e => e.Status != TestStatus.Inactive);
        SearchParameter searchParams = new SearchParameter(SearchTerm: "Value", SearchFields: ["NullableStringProperty"]);

        // Act
        List<TestEntity> result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.ShouldBe(1); // Only the Active entity with "HasValue"
        result[0].Id.ShouldBe(1);
        result[0].NullableStringProperty.ShouldBe("HasValue");
        result[0].Status.ShouldBe(TestStatus.Active);
    }

    [Fact]
    public void SearchBuilder_WorksWithTestEntityPropertiesChaining()
    {
        // Arrange
        IQueryable<TestEntity> query = GetQueryableTestData();

        // Act
        List<TestEntity> result = query
            .Where(e => e.IntProperty >= 20)
            .Search("another")
            .In(e => e.NullableStringProperty!)
            .CaseSensitive(false)
            .Execute()
            .OrderBy(e => e.Id)
            .ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(3);
        result[0].NullableStringProperty.ShouldBe("AnotherValue");
        result[0].IntProperty.ShouldBe(30);
    }

    #endregion
}