using Shouldly;
using SharedKernel.Models.Filter;

namespace SharedKernel.UnitTests.Models.Filter;

/// <summary>
/// Unit tests for QueryFilterExtensions query parameter parsing and filtering functionality.
/// </summary>
public sealed class QueryFilterExtensionsTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    // Test data class
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public string Department { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public Address? Address { get; set; }
    }

    public class Address
    {
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }

    private static List<User> GetTestUsers() =>
    [
        new User { Id = 1, Name = "John Doe", Email = "john@example.com", Age = 25, IsActive = true, Department = "IT", CreatedAt = DateTime.Today.AddDays(-10), Address = new Address { City = "New York", PostalCode = "10001", Country = "USA" } },
        new User { Id = 2, Name = "Jane Smith", Email = "jane@example.com", Age = 30, IsActive = true, Department = "HR", CreatedAt = DateTime.Today.AddDays(-5), Address = new Address { City = "Los Angeles", PostalCode = "90001", Country = "USA" } },
        new User { Id = 3, Name = "Bob Johnson", Email = "bob@example.com", Age = 35, IsActive = false, Department = "IT", CreatedAt = DateTime.Today.AddDays(-20), Address = new Address { City = "Chicago", PostalCode = "60001", Country = "USA" } },
        new User { Id = 4, Name = "Alice Brown", Email = "alice@example.com", Age = 28, IsActive = true, Department = "Finance", CreatedAt = DateTime.Today.AddDays(-15), Address = null },
        new User { Id = 5, Name = "Charlie Wilson", Email = "charlie@example.com", Age = 32, IsActive = false, Department = "IT", CreatedAt = DateTime.Today.AddDays(-8), Address = new Address { City = "Boston", PostalCode = "02101", Country = "USA" } }
    ];

    [Fact]
    public void ApplyFilters_WithEmptyQueryParams_ReturnsOriginalQuery()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryParams = new Dictionary<string, string>();

        // Act
        var result = query.ApplyFilters(queryParams);

        // Assert
        result.Count().ShouldBe(5);
        result.ShouldBe(users);
    }

    [Fact]
    public void ApplyFilters_WithNullQueryParams_ThrowsArgumentNullException()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => query.ApplyFilters((Dictionary<string, string>)null!));
    }

    [Theory]
    [InlineData("name[eq]", "John Doe", 1)]
    [InlineData("age[gt]", "30", 2)]
    [InlineData("isactive[eq]", "true", 3)]
    [InlineData("department[eq]", "IT", 3)]
    public void ApplyFilters_WithBasicQueryParameters_FiltersCorrectly(string key, string value, int expectedCount)
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryParams = new Dictionary<string, string> { { key, value } };

        _output.WriteLine($"Testing filter: {key}={value}, expected count: {expectedCount}");

        // Act
        var result = query.ApplyFilters(queryParams);

        // Assert
        result.Count().ShouldBe(expectedCount);
    }

    [Fact]
    public void ApplyFilters_WithContainsOperator_FiltersCorrectly()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryParams = new Dictionary<string, string>
        {
            { "name[contains]", "john" }
        };

        // Act
        var result = query.ApplyFilters(queryParams);

        // Assert
        result.Count().ShouldBe(2); // John Doe and Bob Johnson
        result.All(u => u.Name.Contains("john", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
    }

    [Fact]
    public void ApplyFilters_WithOrLogic_FiltersCorrectly()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryParams = new Dictionary<string, string>
        {
            { "or_name[contains]", "john" },
            { "age[gte]", "30" }
        };

        // Act
        var result = query.ApplyFilters(queryParams);

        // Assert
        result.Count().ShouldBe(4); // John, Jane, Bob, Charlie (John contains john OR age >= 30)
    }

    [Fact]
    public void ApplyFilters_WithAndLogic_FiltersCorrectly()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryParams = new Dictionary<string, string>
        {
            { "and_department[eq]", "IT" },
            { "isactive[eq]", "true" }
        };

        // Act
        var result = query.ApplyFilters(queryParams);

        // Assert
        result.Count().ShouldBe(1); // Only John (IT AND active)
        result.First().Name.ShouldBe("John Doe");
    }

    [Fact]
    public void ApplyFilters_WithInOperator_FiltersCorrectly()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryParams = new Dictionary<string, string>
        {
            { "department[in]", "IT,HR" }
        };

        // Act
        var result = query.ApplyFilters(queryParams);

        // Assert
        result.Count().ShouldBe(4); // John, Jane, Bob, Charlie
        result.All(u => u.Department == "IT" || u.Department == "HR").ShouldBeTrue();
    }

    [Fact]
    public void ApplyFilters_WithNotInOperator_FiltersCorrectly()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryParams = new Dictionary<string, string>
        {
            { "department[notin]", "IT,HR" }
        };

        // Act
        var result = query.ApplyFilters(queryParams);

        // Assert
        result.Count().ShouldBe(1); // Only Alice (Finance)
        result.First().Name.ShouldBe("Alice Brown");
    }

    [Fact]
    public void ApplyFilters_WithRangeOperator_FiltersCorrectly()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryParams = new Dictionary<string, string>
        {
            { "age[range]", "25,32" }
        };

        // Act
        var result = query.ApplyFilters(queryParams);

        // Assert
        result.Count().ShouldBe(4); // John (25), Jane (30), Alice (28), Charlie (32)
        result.All(u => u.Age >= 25 && u.Age <= 32).ShouldBeTrue();
    }

    [Fact]
    public void ApplyFilters_WithNestedProperties_FiltersCorrectly()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryParams = new Dictionary<string, string>
        {
            { "address.city[eq]", "New York" }
        };

        // Act
        var result = query.ApplyFilters(queryParams);

        // Assert
        result.Count().ShouldBe(1);
        result.First().Name.ShouldBe("John Doe");
    }

    [Fact]
    public void ApplyFilters_WithNullCheck_FiltersCorrectly()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryParams = new Dictionary<string, string>
        {
            { "address[isnull]", "" }
        };

        // Act
        var result = query.ApplyFilters(queryParams);

        // Assert
        result.Count().ShouldBe(1);
        result.First().Name.ShouldBe("Alice Brown");
    }

    [Fact]
    public void ApplyFilters_WithNotNullCheck_FiltersCorrectly()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryParams = new Dictionary<string, string>
        {
            { "address[isnotnull]", "" }
        };

        // Act
        var result = query.ApplyFilters(queryParams);

        // Assert
        result.Count().ShouldBe(4); // All except Alice
        result.All(u => u.Address != null).ShouldBeTrue();
    }

    [Fact]
    public void ApplyFilters_WithComplexMixedLogic_FiltersCorrectly()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryParams = new Dictionary<string, string>
        {
            { "or_department[eq]", "IT" },
            { "or_age[gte]", "30" },
            { "and_isactive[eq]", "true" }
        };

        // Act
        var result = query.ApplyFilters(queryParams);

        // Assert  
        // Should get users who are (IT OR age >= 30) AND active
        // John: IT + active = ✓
        // Jane: age 30 + active = ✓  
        // Bob: IT but not active = ✗
        // Alice: age 28, not IT, but active = ✗
        // Charlie: IT, age 32, but not active = ✗
        result.Count().ShouldBe(2);
        result.Select(u => u.Name).ShouldBe(["John Doe", "Jane Smith"]);
    }

    [Fact]
    public void ApplyFilters_WithGrouping_FiltersCorrectly()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryParams = new Dictionary<string, string>
        {
            { "or_name[contains]", "john" },
            { "or_age[gte]", "30" },
            { "group1", "1" },
            { "department[eq]", "IT" },
            { "isactive[eq]", "true" }
        };

        // Act
        var result = query.ApplyFilters(queryParams);

        // Assert
        // Group 0: (name contains john OR age >= 30)
        // Group 1: (department = IT AND isActive = true)
        // Combined: Group 0 AND Group 1
        result.Count().ShouldBe(1);
        result.First().Name.ShouldBe("John Doe");
    }

    [Fact]
    public void ApplyFilters_WithAlternativeFormat_FiltersCorrectly()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryParams = new Dictionary<string, string>
        {
            { "name_eq", "John Doe" },
            { "age_gt", "20" }
        };

        // Act
        var result = query.ApplyFilters(queryParams);

        // Assert
        result.Count().ShouldBe(1);
        result.First().Name.ShouldBe("John Doe");
    }

    [Fact]
    public void ApplyFilters_WithGlobalLogicOperator_FiltersCorrectly()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryParams = new Dictionary<string, string>
        {
            { "logic", "or" },
            { "name[eq]", "John Doe" },
            { "department[eq]", "HR" }
        };

        // Act
        var result = query.ApplyFilters(queryParams);

        // Assert
        result.Count().ShouldBe(2); // John (name match) OR Jane (HR department)
        result.Select(u => u.Name).OrderBy(n => n).ShouldBe(["Jane Smith", "John Doe"]);
    }

    [Fact]
    public void ApplyFilters_WithInvalidOperator_IgnoresFilter()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryParams = new Dictionary<string, string>
        {
            { "name[invalidop]", "John" },
            { "age[gt]", "20" }
        };

        // Act
        var result = query.ApplyFilters(queryParams);

        // Assert
        // Invalid operator should be ignored, only age filter should apply
        result.Count().ShouldBe(5); // All users have age > 20
    }

    [Fact]
    public void ApplyFilters_WithInvalidPropertyName_IgnoresFilter()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryParams = new Dictionary<string, string>
        {
            { "invalidproperty[eq]", "value" },
            { "age[gt]", "30" }
        };

        // Act
        var result = query.ApplyFilters(queryParams);

        // Assert
        // Invalid property should be ignored, only age filter should apply
        result.Count().ShouldBe(2); // Jane and Bob have age > 30
    }

    [Theory]
    [InlineData("startswith", "john", 1)] // John Doe
    [InlineData("endswith", "smith", 1)]  // Jane Smith
    [InlineData("notcontains", "john", 3)] // Everyone except John Doe and Bob Johnson
    public void ApplyFilters_WithStringOperators_FiltersCorrectly(string op, string value, int expectedCount)
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryParams = new Dictionary<string, string>
        {
            { $"name[{op}]", value }
        };

        _output.WriteLine($"Testing string operator: {op} with value: {value}");

        // Act
        var result = query.ApplyFilters(queryParams);

        // Assert
        result.Count().ShouldBe(expectedCount);
    }

    [Theory]
    [InlineData("gte", "30", 3)] // Jane, Bob, and Charlie
    [InlineData("lte", "30", 3)] // John, Jane, and Alice
    [InlineData("ne", "30", 4)]  // Everyone except Jane
    public void ApplyFilters_WithComparisonOperators_FiltersCorrectly(string op, string value, int expectedCount)
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryParams = new Dictionary<string, string>
        {
            { $"age[{op}]", value }
        };

        _output.WriteLine($"Testing comparison operator: {op} with value: {value}");

        // Act
        var result = query.ApplyFilters(queryParams);

        // Assert
        result.Count().ShouldBe(expectedCount);
    }

    #region String-Based Filtering Tests

    [Fact]
    public void ApplyFilters_WithEmptyQueryString_ReturnsOriginalQuery()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();

        // Act & Assert
        query.ApplyFilters("").Count().ShouldBe(5);
        query.ApplyFilters("   ").Count().ShouldBe(5);
        query.ApplyFilters((string)null!).Count().ShouldBe(5);
    }

    [Fact]
    public void ApplyFilters_WithNullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        IQueryable<User> query = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => query.ApplyFilters("name[eq]=John"));
    }

    [Theory]
    [InlineData("name[eq]=John Doe", 1)]
    [InlineData("age[gt]=30", 2)]
    [InlineData("isactive[eq]=true", 3)]
    [InlineData("department[eq]=IT", 3)]
    [InlineData("name[contains]=john", 2)]
    public void ApplyFilters_WithBasicQueryString_FiltersCorrectly(string queryString, int expectedCount)
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();

        _output.WriteLine($"Testing query string: {queryString}, expected count: {expectedCount}");

        // Act
        var result = query.ApplyFilters(queryString);

        // Assert
        result.Count().ShouldBe(expectedCount);
    }

    [Fact]
    public void ApplyFilters_WithQueryStringLeadingQuestionMark_FiltersCorrectly()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryString = "?name[contains]=john&department[eq]=IT";

        // Act
        var result = query.ApplyFilters(queryString);

        // Assert
        result.Count().ShouldBe(2); // John Doe and Bob Johnson
        result.All(u => u.Name.Contains("john", StringComparison.OrdinalIgnoreCase) && u.Department == "IT")
            .ShouldBeTrue();
    }

    [Fact]
    public void ApplyFilters_WithMultipleParameters_FiltersCorrectly()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryString = "department[eq]=IT&isactive[eq]=true";

        // Act
        var result = query.ApplyFilters(queryString);

        // Assert
        result.Count().ShouldBe(1);
        result.First().Name.ShouldBe("John Doe");
    }

    [Fact]
    public void ApplyFilters_WithOrLogicQueryString_FiltersCorrectly()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryString = "or_name[contains]=john&or_age[gte]=30";

        // Act
        var result = query.ApplyFilters(queryString);

        // Assert
        result.Count().ShouldBe(4); // John, Jane, Bob, Charlie
    }

    [Fact]
    public void ApplyFilters_WithInOperatorQueryString_FiltersCorrectly()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryString = "department[in]=IT,HR";

        // Act
        var result = query.ApplyFilters(queryString);

        // Assert
        result.Count().ShouldBe(4);
        result.All(u => u.Department == "IT" || u.Department == "HR").ShouldBeTrue();
    }

    [Fact]
    public void ApplyFilters_WithRangeOperatorQueryString_FiltersCorrectly()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryString = "age[range]=25,32";

        // Act
        var result = query.ApplyFilters(queryString);

        // Assert
        result.Count().ShouldBe(4);
        result.All(u => u.Age >= 25 && u.Age <= 32).ShouldBeTrue();
    }

    [Fact]
    public void ApplyFilters_WithUrlEncodedQueryString_FiltersCorrectly()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        // URL encoded: "name[contains]=john doe&department[eq]=IT"
        var queryString = "name%5Bcontains%5D=john%20doe&department%5Beq%5D=IT";

        // Act
        var result = query.ApplyFilters(queryString);

        // Assert
        result.Count().ShouldBe(1);
        result.First().Name.ShouldBe("John Doe");
    }

    [Fact]
    public void ApplyFilters_WithNestedPropertyQueryString_FiltersCorrectly()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryString = "address.city[eq]=New York";

        // Act
        var result = query.ApplyFilters(queryString);

        // Assert
        result.Count().ShouldBe(1);
        result.First().Name.ShouldBe("John Doe");
    }

    [Theory]
    [InlineData("name[startswith]=john", 1)] // Only "John Doe"
    [InlineData("name[endswith]=smith", 1)] // Only "Jane Smith"  
    [InlineData("email[contains]=example.com", 5)] // All emails contain example.com
    [InlineData("name[notcontains]=john", 3)] // All except "John Doe" and "Bob Johnson"
    public void ApplyFilters_WithStringOperatorsQueryString_FiltersCorrectly(string queryString, int expectedCount)
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();

        _output.WriteLine($"Testing string operator: {queryString}, expected count: {expectedCount}");

        // Act
        var result = query.ApplyFilters(queryString);

        // Assert
        result.Count().ShouldBe(expectedCount);
    }

    [Theory]
    [InlineData("age[lt]=30", 2)]
    [InlineData("age[lte]=30", 3)]
    [InlineData("age[gte]=30", 3)]
    [InlineData("age[ne]=25", 4)]
    public void ApplyFilters_WithComparisonOperatorsQueryString_FiltersCorrectly(string queryString, int expectedCount)
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();

        _output.WriteLine($"Testing comparison operator: {queryString}, expected count: {expectedCount}");

        // Act
        var result = query.ApplyFilters(queryString);

        // Assert
        result.Count().ShouldBe(expectedCount);
    }

    [Fact]
    public void ApplyFilters_WithGlobalLogicQueryString_FiltersCorrectly()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryString = "logic=or&department[eq]=Finance&age[gte]=30";

        // Act
        var result = query.ApplyFilters(queryString);

        // Assert
        result.Count().ShouldBe(4); // Alice (Finance) OR users with age >= 30
    }

    [Fact]
    public void ApplyFilters_WithComplexQueryString_FiltersCorrectly()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryString = "name[contains]=o&and_age[gte]=25&or_department[eq]=Finance&isactive[eq]=true";

        // Act
        var result = query.ApplyFilters(queryString);

        // Assert
        result.Count().ShouldBe(1); // Only John Doe matches: name contains 'o' AND age >= 25 AND isActive = true
    }

    [Fact]
    public void ApplyFilters_WithInvalidQueryString_HandlesGracefully()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryString = "invalid_format=value&another_invalid";

        // Act
        var result = query.ApplyFilters(queryString);

        // Assert
        result.Count().ShouldBe(5); // Should return all users (no valid filters applied)
    }

    [Fact]
    public void ApplyFilters_WithMalformedQueryString_HandlesGracefully()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryString = "name[eq=john&age]gte]=25&=&invalid";

        // Act & Assert
        Should.NotThrow(() =>
        {
            var result = query.ApplyFilters(queryString);
            result.Count().ShouldBeGreaterThanOrEqualTo(0);
        });
    }

    [Theory]
    [InlineData("name[eq]=John+Doe", "John Doe")] // Plus encoding
    [InlineData("name[eq]=John%20Doe", "John Doe")] // Percent encoding
    [InlineData("email[eq]=john%40example.com", "john@example.com")] // @ symbol encoding
    public void ApplyFilters_WithEncodedValues_DecodesCorrectly(string queryString, string expectedValue)
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();

        _output.WriteLine($"Testing encoded query: {queryString}, expected value: {expectedValue}");

        // Act
        var result = query.ApplyFilters(queryString);

        // Assert
        result.Count().ShouldBe(1);
        var user = result.First();
        (user.Name == expectedValue || user.Email == expectedValue).ShouldBeTrue();
    }

    [Fact]
    public void ApplyFilters_StringVsDictionary_ProducesSameResults()
    {
        // Arrange
        var users = GetTestUsers();
        var query1 = users.AsQueryable();
        var query2 = users.AsQueryable();

        var queryString = "name[contains]=john&department[eq]=IT&isactive[eq]=true";
        var queryDict = new Dictionary<string, string>
        {
            { "name[contains]", "john" },
            { "department[eq]", "IT" },
            { "isactive[eq]", "true" }
        };

        // Act
        var stringResult = query1.ApplyFilters(queryString);
        var dictResult = query2.ApplyFilters(queryDict);

        // Assert
        stringResult.Count().ShouldBe(dictResult.Count());
        var stringUsers = stringResult.ToList();
        var dictUsers = dictResult.ToList();

        stringUsers.Count.ShouldBe(dictUsers.Count);
        for (int i = 0; i < stringUsers.Count; i++)
        {
            stringUsers[i].Id.ShouldBe(dictUsers[i].Id);
        }
    }

    [Fact]
    public void ApplyFilters_WithUnderscoreNotation_FiltersCorrectly()
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();
        var queryString = "name_contains=john&department_eq=IT";

        // Act
        var result = query.ApplyFilters(queryString);

        // Assert
        result.Count().ShouldBe(2); // John Doe and Bob Johnson
    }

    [Theory]
    [InlineData("")]
    [InlineData("?")]
    [InlineData("   ")]
    [InlineData("&&&")]
    [InlineData("===")]
    public void ApplyFilters_WithEmptyOrInvalidQueryStrings_ReturnsAllItems(string queryString)
    {
        // Arrange
        var users = GetTestUsers();
        var query = users.AsQueryable();

        // Act
        var result = query.ApplyFilters(queryString);

        // Assert
        result.Count().ShouldBe(5);
    }

    [Fact]
    public void ApplyFilters_WithSpecialCharactersInValue_HandlesCorrectly()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = 1, Name = "Test & User", Email = "test@example.com", Age = 25, IsActive = true, Department = "IT" },
            new() { Id = 2, Name = "Regular User", Email = "regular@example.com", Age = 30, IsActive = true, Department = "HR" }
        };
        var query = users.AsQueryable();
        var queryString = "name[contains]=Test%20%26%20User"; // "Test & User" encoded

        // Act
        var result = query.ApplyFilters(queryString);

        // Assert
        result.Count().ShouldBe(1);
        result.First().Name.ShouldBe("Test & User");
    }

    #endregion
}
