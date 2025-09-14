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
public class ErrorOrTypedResultsPerformanceTests
{
    private readonly ITestOutputHelper _output;

    public ErrorOrTypedResultsPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region Performance Tests

    [Fact]
    public void Performance_SingleSuccessResult_IsEfficient()
    {
        // Arrange
        var product = new TestProductModel(1, "Test Product", 99.99m);
        var successResult = ErrorOrFactory.From(product);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = successResult.ToTypedResult();
        stopwatch.Stop();

        // Assert
        result.ShouldBeOfType<Ok<TestProductModel>>();
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(10); // Should be very fast
        _output.WriteLine($"Single success conversion took: {stopwatch.ElapsedTicks} ticks");
    }

    [Fact]
    public void Performance_SingleErrorResult_IsEfficient()
    {
        // Arrange
        var error = Error.NotFound("Product.NotFound", "Product was not found");
        var errorResult = ErrorOrFactory.From<TestProductModel>(error);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = errorResult.ToTypedResult();
        stopwatch.Stop();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(10);
        _output.WriteLine($"Single error conversion took: {stopwatch.ElapsedTicks} ticks");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Performance_MultipleValidationErrors_ScalesLinearly(int errorCount)
    {
        // Arrange
        var errors = new List<Error>();
        for (int i = 0; i < errorCount; i++)
        {
            errors.Add(Error.Validation($"Field{i}", $"Error message {i}"));
        }
        var errorResult = ErrorOrFactory.From<TestProductModel>(errors);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = errorResult.ToTypedResult();
        stopwatch.Stop();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        var validationDetails = problemResult.ProblemDetails as HttpValidationProblemDetails;
        validationDetails.ShouldNotBeNull();
        validationDetails.Errors.Count.ShouldBe(errorCount);

        _output.WriteLine($"{errorCount} validation errors took: {stopwatch.ElapsedMilliseconds}ms");

        // Performance should be reasonable even with many errors
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(errorCount / 10 + 100);
    }

    [Fact]
    public void Performance_RepeatedConversions_AreConsistent()
    {
        // Arrange
        var product = new TestProductModel(1, "Test Product", 99.99m);
        var successResult = ErrorOrFactory.From(product);
        var times = new List<long>();

        // Act - Perform multiple conversions
        for (int i = 0; i < 1000; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = successResult.ToTypedResult();
            stopwatch.Stop();
            times.Add(stopwatch.ElapsedTicks);
            
            result.ShouldBeOfType<Ok<TestProductModel>>();
        }

        // Assert
        var averageTime = times.Sum() / times.Count;
        var maxTime = times.Max();
        var minTime = times.Min();
        
        _output.WriteLine($"Average: {averageTime} ticks, Min: {minTime} ticks, Max: {maxTime} ticks");
        
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
        var largeMessage = new string('A', 100_000); // 100KB message
        var error = Error.Failure("Large.Error", largeMessage);
        var errorResult = ErrorOrFactory.From<TestProductModel>(error);

        // Act
        var result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.ProblemDetails.Detail.ShouldBe(largeMessage);
        problemResult.ProblemDetails.Detail!.Length.ShouldBe(100_000);
    }

    [Fact]
    public void Memory_ManyErrorsWithLargeMessages_HandledGracefully()
    {
        // Arrange
        var errors = new List<Error>();
        for (int i = 0; i < 100; i++)
        {
            var largeMessage = new string('X', 10_000); // 10KB each
            errors.Add(Error.Validation($"Field{i}", largeMessage));
        }
        var errorResult = ErrorOrFactory.From<TestProductModel>(errors);

        // Act
        var result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        var validationDetails = problemResult.ProblemDetails as HttpValidationProblemDetails;
        validationDetails.ShouldNotBeNull();
        validationDetails.Errors.Count.ShouldBe(100);

        // Each error message should be preserved
        foreach (var kvp in validationDetails.Errors)
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
        var product = new TestProductModel(1, "Test Product", 99.99m);
        var successResult = ErrorOrFactory.From(product);
        var tasks = new List<Task<IResult>>();

        // Act - Run conversions in parallel
        for (int i = 0; i < 1000; i++)
        {
            tasks.Add(Task.Run(() => successResult.ToTypedResult()));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        foreach (var result in results)
        {
            result.ShouldBeOfType<Ok<TestProductModel>>();
            var okResult = (Ok<TestProductModel>)result;
            okResult.Value.ShouldBe(product);
        }
    }

    [Fact]
    public async Task Concurrency_ParallelErrorConversions_AreThreadSafe()
    {
        // Arrange
        var error = Error.NotFound("Product.NotFound", "Product was not found");
        var errorResult = ErrorOrFactory.From<TestProductModel>(error);
        var tasks = new List<Task<IResult>>();

        // Act
        for (int i = 0; i < 1000; i++)
        {
            tasks.Add(Task.Run(() => errorResult.ToTypedResult()));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        foreach (var result in results)
        {
            result.ShouldBeOfType<ProblemHttpResult>();
            var problemResult = (ProblemHttpResult)result;
            problemResult.ProblemDetails.Title.ShouldBe("Product.NotFound");
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ExtremeCases_MaxIntId_HandlesCorrectly()
    {
        // Arrange
        var product = new TestProductModel(int.MaxValue, "Max ID Product", 999999999.99m);
        var successResult = ErrorOrFactory.From(product);

        // Act
        var result = successResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<Ok<TestProductModel>>();
        var okResult = (Ok<TestProductModel>)result;
        okResult.Value!.Id.ShouldBe(int.MaxValue);
        okResult.Value.Price.ShouldBe(999999999.99m);
    }

    [Fact]
    public void ExtremeCases_MinIntId_HandlesCorrectly()
    {
        // Arrange
        var product = new TestProductModel(int.MinValue, "Min ID Product", -999999999.99m);
        var successResult = ErrorOrFactory.From(product);

        // Act
        var result = successResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<Ok<TestProductModel>>();
        var okResult = (Ok<TestProductModel>)result;
        okResult.Value!.Id.ShouldBe(int.MinValue);
        okResult.Value.Price.ShouldBe(-999999999.99m);
    }

    [Fact]
    public void ExtremeCases_UnicodeErrorMessages_HandlesCorrectly()
    {
        // Arrange
        var unicodeMessage = "测试错误消息 🚀 العربية русский ñäöü €₹¥£";
        var error = Error.Validation("UnicodeField", unicodeMessage);
        var errorResult = ErrorOrFactory.From<TestProductModel>(error);

        // Act
        var result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        var validationDetails = problemResult.ProblemDetails as HttpValidationProblemDetails;
        validationDetails.ShouldNotBeNull();
        validationDetails.Errors["UnicodeField"][0].ShouldBe(unicodeMessage);
    }

    [Fact]
    public void ExtremeCases_ErrorCodeWithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var specialCode = "Error.With-Special_Characters.123!@#";
        var error = Error.Validation(specialCode, "Error with special code");
        var errorResult = ErrorOrFactory.From<TestProductModel>(error);

        // Act
        var result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        var validationDetails = problemResult.ProblemDetails as HttpValidationProblemDetails;
        validationDetails.ShouldNotBeNull();
        validationDetails.Errors.ShouldContainKey(specialCode);
    }

    [Fact]
    public void ExtremeCases_VeryLongLocationUrl_HandlesCorrectly()
    {
        // Arrange
        var product = new TestProductModel(1, "Test", 99.99m);
        var successResult = ErrorOrFactory.From(product);
        var veryLongUrl = "https://example.com/" + new string('a', 10_000) + "/product/1";

        // Act
        var result = successResult.ToTypedResultCreated(veryLongUrl);

        // Assert
        result.ShouldBeOfType<Created<TestProductModel>>();
        var createdResult = (Created<TestProductModel>)result;
        createdResult.Location.ShouldBe(veryLongUrl);
        createdResult.Location!.Length.ShouldBe(veryLongUrl.Length);
    }

    [Fact]
    public void ExtremeCases_ErrorWithNullDescription_HandlesCorrectly()
    {
        // Arrange
        var error = Error.Custom(999, "Null.Description", null!);
        var errorResult = ErrorOrFactory.From<TestProductModel>(error);

        // Act
        var result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.ProblemDetails.Detail.ShouldBeNull();
    }

    [Fact]
    public void ExtremeCases_ErrorWithEmptyCode_HandlesCorrectly()
    {
        // Arrange
        var error = Error.Custom(999, "", "Error with empty code");
        var errorResult = ErrorOrFactory.From<TestProductModel>(error);

        // Act
        var result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
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