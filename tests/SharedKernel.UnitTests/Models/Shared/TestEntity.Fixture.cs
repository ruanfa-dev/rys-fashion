namespace SharedKernel.UnitTests.Models.Shared;

public enum TestStatus
{
    Pending,
    Active,
    Inactive
}

public class TestAddress
{
    public required string City { get; set; }
    public string? PostalCode { get; set; }
}

public class TestEntity
{
    public int Id { get; set; }
    public required string StringProperty { get; set; }
    public string? NullableStringProperty { get; set; }
    public int IntProperty { get; set; }
    public int? NullableIntProperty { get; set; }
    public decimal DecimalProperty { get; set; }
    public DateTime DateTimeProperty { get; set; }
    public DateTimeOffset DateTimeOffsetProperty { get; set; }
    public bool BoolProperty { get; set; }
    public TestStatus Status { get; set; }
    public TestAddress? Address { get; set; }

    public static List<TestEntity> GetTestData() =>
        new()
        {
        new TestEntity
        {
            Id = 1,
            StringProperty = "Apple",
            NullableStringProperty = "HasValue",
            IntProperty = 10,
            NullableIntProperty = 100,
            DecimalProperty = 10.5m,
            DateTimeProperty = DateTime.Parse("2023-01-10T00:00:00Z").ToUniversalTime(),
            DateTimeOffsetProperty = DateTimeOffset.Parse("2023-01-01T00:00:00Z"),
            BoolProperty = true,
            Status = TestStatus.Active,
            Address = new TestAddress { City = "New York", PostalCode = "10001" }
        },
        new TestEntity
        {
            Id = 2,
            StringProperty = "Banana",
            NullableStringProperty = null,
            IntProperty = 20,
            NullableIntProperty = null,
            DecimalProperty = 20.5m,
            DateTimeProperty = new DateTime(2023, 2, 15, 0, 0, 0, DateTimeKind.Utc),
            DateTimeOffsetProperty = new DateTimeOffset(2023, 2, 15, 0, 0, 0, TimeSpan.Zero),
            BoolProperty = false,
            Status = TestStatus.Pending,
            Address = new TestAddress { City = "London", PostalCode = null }
        },
        new TestEntity
        {
            Id = 3,
            StringProperty = "Blueberry",
            NullableStringProperty = "AnotherValue",
            IntProperty = 30,
            NullableIntProperty = 300,
            DecimalProperty = 30.5m,
            DateTimeProperty = new DateTime(2023, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            DateTimeOffsetProperty = new DateTimeOffset(2023, 3, 20, 0, 0, 0, TimeSpan.Zero),
            BoolProperty = true,
            Status = TestStatus.Inactive,
            Address = null // Null nested object
        }
        };
}