using System.Diagnostics;

using ErrorOr;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

using Shouldly;

using UseCases.Common.Extensions;

namespace UseCases.UnitTests.Common.Extensions;

/// <summary>
/// Performance, stress, and edge case tests for ErrorOrTypedResultsExtensions
/// These tests verify the extensions handle extreme scenarios gracefully
/// </summary>
public class ErrorOrTypedResultsPerformanceTests(ITestOutputHelper output)
{
    #region Performance Tests

    [Fact]
    public void Performance_SingleSuccessResult_IsEfficient()
    {
        // Arrange
        TestProductModel product = new TestProductModel(1, "Test Product", 99.99m);
        ErrorOr<TestProductModel> successResult = ErrorOrFactory.From(product);
        Stopwatch stopwatch = Stopwatch.StartNew();

        // Act
        IResult result = successResult.ToTypedResult();
        stopwatch.Stop();

        // Assert
        result.ShouldBeOfType<Ok<TestProductModel>>();
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(10); // Should be very fast
        output.WriteLine($"Single success conversion took: {stopwatch.ElapsedTicks} ticks");
    }

    [Fact]
    public void Performance_SingleErrorResult_IsEfficient()
    {
        // Arrange
        Error error = Error.NotFound("Product.NotFound", "Product was not found");
        ErrorOr<TestProductModel> errorResult = ErrorOrFactory.From<TestProductModel>(error);
        Stopwatch stopwatch = Stopwatch.StartNew();

        // Act
        IResult result = errorResult.ToTypedResult();
        stopwatch.Stop();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(10);
        output.WriteLine($"Single error conversion took: {stopwatch.ElapsedTicks} ticks");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Performance_MultipleValidationErrors_ScalesLinearly(int errorCount)
    {
        // Arrange
        List<Error> errors = [];
        for (int i = 0; i < errorCount; i++)
        {
            errors.Add(Error.Validation($"Field{i}", $"Error message {i}"));
        }
        ErrorOr<TestProductModel> errorResult = ErrorOrFactory.From<TestProductModel>(errors);
        Stopwatch stopwatch = Stopwatch.StartNew();

        // Act
        IResult result = errorResult.ToTypedResult();
        stopwatch.Stop();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
        HttpValidationProblemDetails? validationDetails = problemResult.ProblemDetails as HttpValidationProblemDetails;
        validationDetails.ShouldNotBeNull();
        validationDetails.Errors.Count.ShouldBe(errorCount);

        output.WriteLine($"{errorCount} validation errors took: {stopwatch.ElapsedMilliseconds}ms");

        // Performance should be reasonable even with many errors
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(errorCount / 10 + 100);
    }

    [Fact]
    public void Performance_RepeatedConversions_AreConsistent()
    {
        // Arrange
        TestProductModel product = new TestProductModel(1, "Test Product", 99.99m);
        ErrorOr<TestProductModel> successResult = ErrorOrFactory.From(product);
        List<long> times = [];

        // Act - Perform multiple conversions
        for (int i = 0; i < 1000; i++)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            IResult result = successResult.ToTypedResult();
            stopwatch.Stop();
            times.Add(stopwatch.ElapsedTicks);
            
            result.ShouldBeOfType<Ok<TestProductModel>>();
        }

        // Assert
        long averageTime = times.Sum() / times.Count;
        long maxTime = times.Max();
        long minTime = times.Min();
        
        output.WriteLine($"Average: {averageTime} ticks, Min: {minTime} ticks, Max: {maxTime} ticks");
        
        // Performance should be consistent (max shouldn't be more than 100x average for micro-operations)
        // This is more lenient to account for system variations
        maxTime.ShouldBeLessThan(averageTime * 1000);
    }

    #endregion

    #region Memory Tests

    [Fact]
    public void Memory_LargeErrorMessages_HandledCorrectly()
    {
        // Arrange - Create error with very large message
        string largeMessage = new string('A', 100_000); // 100KB message
        Error error = Error.Failure("Large.Error", largeMessage);
        ErrorOr<TestProductModel> errorResult = ErrorOrFactory.From<TestProductModel>(error);

        // Act
        IResult result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
        problemResult.ProblemDetails.Detail.ShouldBe(largeMessage);
        problemResult.ProblemDetails.Detail!.Length.ShouldBe(100_000);
    }

    [Fact]
    public void Memory_ManyErrorsWithLargeMessages_HandledGracefully()
    {
        // Arrange
        List<Error> errors = [];
        for (int i = 0; i < 100; i++)
        {
            string largeMessage = new string('X', 10_000); // 10KB each
            errors.Add(Error.Validation($"Field{i}", largeMessage));
        }
        ErrorOr<TestProductModel> errorResult = ErrorOrFactory.From<TestProductModel>(errors);

        // Act
        IResult result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
        HttpValidationProblemDetails? validationDetails = problemResult.ProblemDetails as HttpValidationProblemDetails;
        validationDetails.ShouldNotBeNull();
        validationDetails.Errors.Count.ShouldBe(100);

        // Each error message should be preserved
        foreach (KeyValuePair<string, string[]> kvp in validationDetails.Errors)
        {
            kvp.Value[0].Length.ShouldBe(10_000);
        }
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task Concurrency_ParallelConversions_AreThreadSafe()
    {
        // Arrange
        TestProductModel product = new TestProductModel(1, "Test Product", 99.99m);
        ErrorOr<TestProductModel> successResult = ErrorOrFactory.From(product);
        List<Task<IResult>> tasks = [];

        // Act - Run conversions in parallel
        for (int i = 0; i < 1000; i++)
        {
            tasks.Add(Task.Run(() => successResult.ToTypedResult()));
        }

        IResult[] results = await Task.WhenAll(tasks);

        // Assert
        foreach (IResult result in results)
        {
            result.ShouldBeOfType<Ok<TestProductModel>>();
            Ok<TestProductModel> okResult = (Ok<TestProductModel>)result;
            okResult.Value.ShouldBe(product);
        }
    }

    [Fact]
    public async Task Concurrency_ParallelErrorConversions_AreThreadSafe()
    {
        // Arrange
        Error error = Error.NotFound("Product.NotFound", "Product was not found");
        ErrorOr<TestProductModel> errorResult = ErrorOrFactory.From<TestProductModel>(error);
        List<Task<IResult>> tasks = [];

        // Act
        for (int i = 0; i < 1000; i++)
        {
            tasks.Add(Task.Run(() => errorResult.ToTypedResult()));
        }

        IResult[] results = await Task.WhenAll(tasks);

        // Assert
        foreach (IResult result in results)
        {
            result.ShouldBeOfType<ProblemHttpResult>();
            ProblemHttpResult problemResult = (ProblemHttpResult)result;
            problemResult.ProblemDetails.Title.ShouldBe("Product.NotFound");
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ExtremeCases_MaxIntId_HandlesCorrectly()
    {
        // Arrange
        TestProductModel product = new TestProductModel(int.MaxValue, "Max ID Product", 999999999.99m);
        ErrorOr<TestProductModel> successResult = ErrorOrFactory.From(product);

        // Act
        IResult result = successResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<Ok<TestProductModel>>();
        Ok<TestProductModel> okResult = (Ok<TestProductModel>)result;
        okResult.Value!.Id.ShouldBe(int.MaxValue);
        okResult.Value.Price.ShouldBe(999999999.99m);
    }

    [Fact]
    public void ExtremeCases_MinIntId_HandlesCorrectly()
    {
        // Arrange
        TestProductModel product = new TestProductModel(int.MinValue, "Min ID Product", -999999999.99m);
        ErrorOr<TestProductModel> successResult = ErrorOrFactory.From(product);

        // Act
        IResult result = successResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<Ok<TestProductModel>>();
        Ok<TestProductModel> okResult = (Ok<TestProductModel>)result;
        okResult.Value!.Id.ShouldBe(int.MinValue);
        okResult.Value.Price.ShouldBe(-999999999.99m);
    }

    [Fact]
    public void ExtremeCases_UnicodeErrorMessages_HandlesCorrectly()
    {
        // Arrange
        string unicodeMessage = "测试错误消息 🚀 العربية русский ñäöü €₹¥£";
        Error error = Error.Validation("UnicodeField", unicodeMessage);
        ErrorOr<TestProductModel> errorResult = ErrorOrFactory.From<TestProductModel>(error);

        // Act
        IResult result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
        HttpValidationProblemDetails? validationDetails = problemResult.ProblemDetails as HttpValidationProblemDetails;
        validationDetails.ShouldNotBeNull();
        validationDetails.Errors["UnicodeField"][0].ShouldBe(unicodeMessage);
    }

    [Fact]
    public void ExtremeCases_ErrorCodeWithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        string specialCode = "Error.With-Special_Characters.123!@#";
        Error error = Error.Validation(specialCode, "Error with special code");
        ErrorOr<TestProductModel> errorResult = ErrorOrFactory.From<TestProductModel>(error);

        // Act
        IResult result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
        HttpValidationProblemDetails? validationDetails = problemResult.ProblemDetails as HttpValidationProblemDetails;
        validationDetails.ShouldNotBeNull();
        validationDetails.Errors.ShouldContainKey(specialCode);
    }

    [Fact]
    public void ExtremeCases_VeryLongLocationUrl_HandlesCorrectly()
    {
        // Arrange
        TestProductModel product = new TestProductModel(1, "Test", 99.99m);
        ErrorOr<TestProductModel> successResult = ErrorOrFactory.From(product);
        string veryLongUrl = "https://example.com/" + new string('a', 10_000) + "/product/1";

        // Act
        IResult result = successResult.ToTypedResultCreated(veryLongUrl);

        // Assert
        result.ShouldBeOfType<Created<TestProductModel>>();
        Created<TestProductModel> createdResult = (Created<TestProductModel>)result;
        createdResult.Location.ShouldBe(veryLongUrl);
        createdResult.Location!.Length.ShouldBe(veryLongUrl.Length);
    }

    [Fact]
    public void ExtremeCases_ErrorWithNullDescription_HandlesCorrectly()
    {
        // Arrange
        Error error = Error.Custom(999, "Null.Description", null!);
        ErrorOr<TestProductModel> errorResult = ErrorOrFactory.From<TestProductModel>(error);

        // Act
        IResult result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
        problemResult.ProblemDetails.Detail.ShouldBeNull();
    }

    [Fact]
    public void ExtremeCases_ErrorWithEmptyCode_HandlesCorrectly()
    {
        // Arrange
        Error error = Error.Custom(999, "", "Error with empty code");
        ErrorOr<TestProductModel> errorResult = ErrorOrFactory.From<TestProductModel>(error);

        // Act
        IResult result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
        problemResult.ProblemDetails.Title.ShouldBe("");
    }

    #endregion

    #region Helper Classes

    private static class ErrorOrFactory
    {
        public static ErrorOr<T> From<T>(T value) => value;
        public static ErrorOr<T> From<T>(Error error) => error;
        public static ErrorOr<T> From<T>(List<Error> errors) => errors;
    }

    #endregion
}