using SharedKernel.Models.Paging;
using SharedKernel.Models.Sort;

using Shouldly;

namespace SharedKernel.UnitTests.Models.Paging;

public class PagingExtensionsTests
{
    public class User
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTimeOffset CreatedOffset { get; set; }
        public DateTime? LastLoginUtc { get; set; }
        public DateTimeOffset? LastActivityOffset { get; set; }
        public UserStatus Status { get; set; }
    }

    public enum UserStatus
    {
        Active,
        Inactive
    }

    private readonly List<User> _users;

    public PagingExtensionsTests()
    {
        _users = new List<User>
        {
            new User
            {
                Id = 1,
                Name = "John Doe",
                CreatedUtc = DateTime.Parse("2023-01-01T00:00:00Z"),
                CreatedOffset = DateTimeOffset.Parse("2023-01-01T00:00:00+00:00"),
                LastLoginUtc = DateTime.Parse("2023-06-01T00:00:00Z"),
                LastActivityOffset = DateTimeOffset.Parse("2023-06-01T00:00:00+00:00"),
                Status = UserStatus.Active
            },
            new User
            {
                Id = 2,
                Name = "Jane Smith",
                CreatedUtc = DateTime.Parse("2023-02-01T00:00:00Z"),
                CreatedOffset = DateTimeOffset.Parse("2023-02-01T00:00:00+00:00"),
                LastLoginUtc = null,
                LastActivityOffset = null,
                Status = UserStatus.Inactive
            },
            new User
            {
                Id = 3,
                Name = "Bob Johnson",
                CreatedUtc = DateTime.Parse("2022-12-01T00:00:00Z"),
                CreatedOffset = DateTimeOffset.Parse("2022-12-01T00:00:00+00:00"),
                LastLoginUtc = DateTime.Parse("2023-07-01T00:00:00Z"),
                LastActivityOffset = DateTimeOffset.Parse("2023-07-01T00:00:00+00:00"),
                Status = UserStatus.Active
            }
        };
    }

    private IQueryable<User> GetQueryableUsers() => _users.AsQueryable();

    #region ApplyPagingOrDefault Tests

    [Fact]
    public void ApplyPagingOrDefault_NullPagingParams_ReturnsDefaultPageSize()
    {
        var query = GetQueryableUsers();

        var result = query.ApplyPagingOrDefault(null).ToList();

        result.Count.ShouldBe(3); // All items, less than default page size (10)
        result[0].Id.ShouldBe(1);
        result[1].Id.ShouldBe(2);
        result[2].Id.ShouldBe(3);
    }

    [Fact]
    public void ApplyPagingOrDefault_DefaultParams_ReturnsDefaultPageSize()
    {
        var query = GetQueryableUsers();
        var @params = new PagingParams();

        var result = query.ApplyPagingOrDefault(@params).ToList();

        result.Count.ShouldBe(3); // All items, less than default page size (10)
        result[0].Id.ShouldBe(1);
        result[1].Id.ShouldBe(2);
        result[2].Id.ShouldBe(3);
    }

    [Fact]
    public void ApplyPagingOrDefault_WithPageSize_ReturnsFirstPageWithSpecifiedSize()
    {
        var query = GetQueryableUsers();
        var @params = new PagingParams(PageSize: 2);

        var result = query.ApplyPagingOrDefault(@params).ToList();

        result.Count.ShouldBe(2);
        result[0].Id.ShouldBe(1); // John Doe
        result[1].Id.ShouldBe(2); // Jane Smith
    }

    [Fact]
    public void ApplyPagingOrDefault_WithPageSizeAndPageIndex_ReturnsCorrectPage()
    {
        var query = GetQueryableUsers();
        var @params = new PagingParams(PageSize: 1, PageIndex: 1);

        var result = query.ApplyPagingOrDefault(@params).ToList();

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(2); // Jane Smith (second item, page index 1 with page size 1)
    }

    [Fact]
    public void ApplyPagingOrDefault_WithPageIndex_ReturnsSecondPageWithDefaultSize()
    {
        var query = GetQueryableUsers();
        var @params = new PagingParams(PageIndex: 1);

        var result = query.ApplyPagingOrDefault(@params).ToList();

        result.Count.ShouldBe(0); // No items on page index 1 (page 2) with default page size (10)
    }

    [Fact]
    public void ApplyPagingOrDefault_NegativePageIndex_NormalizesToFirstPage()
    {
        var query = GetQueryableUsers();
        var @params = new PagingParams(PageSize: 2, PageIndex: -1);

        var result = query.ApplyPagingOrDefault(@params).ToList();

        result.Count.ShouldBe(2);
        result[0].Id.ShouldBe(1); // John Doe
        result[1].Id.ShouldBe(2); // Jane Smith
    }

    [Fact]
    public void ApplyPagingOrDefault_CustomFallbackDefaultPageSize()
    {
        var query = GetQueryableUsers();

        var result = query.ApplyPagingOrDefault(null, fallbackDefaultPageSize: 2).ToList();

        result.Count.ShouldBe(2);
        result[0].Id.ShouldBe(1); // John Doe
        result[1].Id.ShouldBe(2); // Jane Smith
    }

    [Fact]
    public void ApplyPagingOrDefault_NegativeFallbackPageSize_UsesDefault()
    {
        var query = GetQueryableUsers();

        var result = query.ApplyPagingOrDefault(null, fallbackDefaultPageSize: -5).ToList();

        result.Count.ShouldBe(3); // All items, uses default page size (10)
        result[0].Id.ShouldBe(1);
        result[1].Id.ShouldBe(2);
        result[2].Id.ShouldBe(3);
    }

    [Fact]
    public void ApplyPagingOrDefault_ExcessivePageSize_CapsAtMax()
    {
        var query = GetQueryableUsers();
        var @params = new PagingParams(PageSize: 150);

        var result = query.ApplyPagingOrDefault(@params).ToList();

        result.Count.ShouldBe(3); // All items, capped at max page size (100)
        result[0].Id.ShouldBe(1);
        result[1].Id.ShouldBe(2);
        result[2].Id.ShouldBe(3);
    }

    [Fact]
    public void ApplyPagingOrDefault_BeyondTotalItems_ReturnsEmpty()
    {
        var query = GetQueryableUsers();
        var @params = new PagingParams(PageSize: 5, PageIndex: 2);

        var result = query.ApplyPagingOrDefault(@params).ToList();

        result.Count.ShouldBe(0); // No items on page index 2 with 5 items per page
    }

    [Fact]
    public void ApplyPagingOrDefault_WithSort_AppliesCorrectly()
    {
        var query = GetQueryableUsers();
        var pagingParams = new PagingParams(PageSize: 2);
        var sortParams = new SortParams("Name", "asc");

        var result = query
            .ApplySort(sortParams)
            .ApplyPagingOrDefault(pagingParams)
            .ToList();

        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("Bob Johnson"); // Alphabetical order
        result[1].Name.ShouldBe("Jane Smith");
    }

    [Fact]
    public void ApplyPagingOrDefault_ZeroPageSize_UsesDefault()
    {
        var query = GetQueryableUsers();
        var @params = new PagingParams(PageSize: 0);

        var result = query.ApplyPagingOrDefault(@params).ToList();

        result.Count.ShouldBe(3); // Uses default page size (10)
    }

    [Fact]
    public void ApplyPagingOrDefault_NullQuery_ThrowsArgumentNullException()
    {
        IQueryable<User>? query = null;
        var @params = new PagingParams(PageSize: 1);

        Should.Throw<ArgumentNullException>(() => query!.ApplyPagingOrDefault(@params));
    }

    #endregion

    #region ApplyPagingOrAll Tests

    [Fact]
    public void ApplyPagingOrAll_NullPagingParams_ReturnsAllRecords()
    {
        var query = GetQueryableUsers();

        var result = query.ApplyPagingOrAll(null).ToList();

        result.Count.ShouldBe(3); // All items, no pagination
        result[0].Id.ShouldBe(1);
        result[1].Id.ShouldBe(2);
        result[2].Id.ShouldBe(3);
    }

    [Fact]
    public void ApplyPagingOrAll_DefaultParams_ReturnsAllRecords()
    {
        var query = GetQueryableUsers();
        var @params = new PagingParams();

        var result = query.ApplyPagingOrAll(@params).ToList();

        result.Count.ShouldBe(3); // All items, no valid paging values
        result[0].Id.ShouldBe(1);
        result[1].Id.ShouldBe(2);
        result[2].Id.ShouldBe(3);
    }

    [Fact]
    public void ApplyPagingOrAll_WithPageSize_ReturnsFirstPageWithSpecifiedSize()
    {
        var query = GetQueryableUsers();
        var @params = new PagingParams(PageSize: 2);

        var result = query.ApplyPagingOrAll(@params).ToList();

        result.Count.ShouldBe(2);
        result[0].Id.ShouldBe(1);
        result[1].Id.ShouldBe(2);
    }

    [Fact]
    public void ApplyPagingOrAll_WithPageSizeAndPageIndex_ReturnsCorrectPage()
    {
        var query = GetQueryableUsers();
        var @params = new PagingParams(PageSize: 1, PageIndex: 2);

        var result = query.ApplyPagingOrAll(@params).ToList();

        result.Count.ShouldBe(1); // Third item
        result[0].Id.ShouldBe(3); // Bob Johnson
    }

    [Fact]
    public void ApplyPagingOrAll_WithPageIndex_ReturnsSecondPageWithDefaultSize()
    {
        var query = GetQueryableUsers();
        var @params = new PagingParams(PageIndex: 1);

        var result = query.ApplyPagingOrAll(@params).ToList();

        result.Count.ShouldBe(0); // No items on page index 1 (page 2) with default page size (10)
    }

    [Fact]
    public void ApplyPagingOrAll_NegativePageIndex_NormalizesToFirstPage()
    {
        var query = GetQueryableUsers();
        var @params = new PagingParams(PageSize: 2, PageIndex: -1);

        var result = query.ApplyPagingOrAll(@params).ToList();

        result.Count.ShouldBe(2); // Normalized to page index 0, returns first 2 items
        result[0].Id.ShouldBe(1);
        result[1].Id.ShouldBe(2);
    }

    [Fact]
    public void ApplyPagingOrAll_WithSort_AppliesCorrectly()
    {
        var query = GetQueryableUsers();
        var pagingParams = new PagingParams(PageSize: 2);
        var sortParams = new SortParams("Name", "asc");

        var result = query
            .ApplySort(sortParams)
            .ApplyPagingOrAll(pagingParams)
            .ToList();

        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("Bob Johnson"); // Alphabetical order
        result[1].Name.ShouldBe("Jane Smith");
    }

    [Fact]
    public void ApplyPagingOrAll_MaxAllItemsLimit_CapsResults()
    {
        // Create a large collection to test the limit
        var largeUserList = Enumerable.Range(1, 1500)
            .Select(i => new User
            {
                Id = i,
                Name = $"User {i}",
                CreatedUtc = DateTime.Now,
                CreatedOffset = DateTimeOffset.Now,
                Status = UserStatus.Active
            })
            .AsQueryable();

        var result = largeUserList.ApplyPagingOrAll(null, maxAllItemsLimit: 100).ToList();

        result.Count.ShouldBe(100); // Capped at maxAllItemsLimit
        result[0].Id.ShouldBe(1);
        result[99].Id.ShouldBe(100);
    }

    [Fact]
    public void ApplyPagingOrAll_ExtremeMaxLimit_UsesConfiguredMax()
    {
        var query = GetQueryableUsers();

        var result = query.ApplyPagingOrAll(null, maxAllItemsLimit: 10000).ToList();

        result.Count.ShouldBe(3); // Still limited by actual data count, but capped at MaxAllItemsLimit (1000)
    }

    [Fact]
    public void ApplyPagingOrAll_NegativeMaxLimit_UsesMinimum()
    {
        var query = GetQueryableUsers();

        var result = query.ApplyPagingOrAll(null, maxAllItemsLimit: -5).ToList();

        result.Count.ShouldBe(1); // Uses minimum of 1
    }

    [Fact]
    public void ApplyPagingOrAll_NullQuery_ThrowsArgumentNullException()
    {
        IQueryable<User>? query = null;
        var @params = new PagingParams(PageSize: 1);

        Should.Throw<ArgumentNullException>(() => query!.ApplyPagingOrAll(@params));
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void ApplyPagingOrDefault_LargeDataSet_PerformanceTest()
    {
        // Create a large dataset
        var largeUserList = Enumerable.Range(1, 10000)
            .Select(i => new User
            {
                Id = i,
                Name = $"User {i}",
                CreatedUtc = DateTime.Now,
                CreatedOffset = DateTimeOffset.Now,
                Status = i % 2 == 0 ? UserStatus.Active : UserStatus.Inactive
            })
            .AsQueryable();

        var @params = new PagingParams(PageSize: 20, PageIndex: 50); // Page 51 with 20 items per page
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var result = largeUserList.ApplyPagingOrDefault(@params).ToList();

        stopwatch.Stop();

        result.Count.ShouldBe(20);
        result[0].Id.ShouldBe(1001); // (50 * 20) + 1 = 1001
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(100); // Should be fast for in-memory operations
    }

    #endregion
}