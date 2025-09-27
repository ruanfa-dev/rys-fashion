using ErrorOr;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

using Shouldly;

using UseCases.Common.Extensions;

namespace UseCases.UnitTests.Common.Extensions;

/// <summary>
/// Comprehensive test suite for ErrorOrTypedResultsExtensions
/// Tests all extension methods with various scenarios including edge cases
/// </summary>
public class ErrorOrTypedResultsExtensionsTests
{
    private readonly TestModel _testModel = new(1, "Test", 100.50m);
    private const string TestLocationUrl = "/api/test/1";

    #region ToTypedResult Tests

    [Fact]
    public void ToTypedResult_WithSuccessValue_ReturnsOkResult()
    {
        // Arrange
        var successResult = ErrorOrFactory.From(_testModel);

        // Act
        var result = successResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<Ok<TestModel>>();
        var okResult = (Ok<TestModel>)result;
        okResult.Value.ShouldBe(_testModel);
    }

    [Fact]
    public void ToTypedResult_WithNullValue_ReturnsOkResultWithNull()
    {
        // Arrange
        TestModel? nullModel = null;
        var successResult = ErrorOrFactory.From(nullModel);

        // Act
        var result = successResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<Ok<TestModel?>>();
        var okResult = (Ok<TestModel?>)result;
        okResult.Value.ShouldBeNull();
    }

    [Fact]
    public void ToTypedResult_WithValidationError_ReturnsValidationProblem()
    {
        // Arrange
        var validationError = Error.Validation("TestField", "Validation failed");
        var errorResult = ErrorOrFactory.From<TestModel>(validationError);

        // Act
        var result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        problemResult.ProblemDetails.Title.ShouldBe("Validation Failed");
    }

    [Fact]
    public void ToTypedResult_WithNotFoundError_ReturnsNotFoundProblem()
    {
        // Arrange
        var notFoundError = Error.NotFound("Resource.NotFound", "Resource was not found");
        var errorResult = ErrorOrFactory.From<TestModel>(notFoundError);

        // Act
        var result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        problemResult.ProblemDetails.Title.ShouldBe("Resource.NotFound");
        problemResult.ProblemDetails.Detail.ShouldBe("Resource was not found");
    }

    [Fact]
    public void ToTypedResult_WithUnauthorizedError_ReturnsUnauthorizedProblem()
    {
        // Arrange
        var unauthorizedError = Error.Unauthorized("Auth.Unauthorized", "Access denied");
        var errorResult = ErrorOrFactory.From<TestModel>(unauthorizedError);

        // Act
        var result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
        problemResult.ProblemDetails.Title.ShouldBe("Auth.Unauthorized");
    }

    [Fact]
    public void ToTypedResult_WithForbiddenError_ReturnsForbiddenProblem()
    {
        // Arrange
        var forbiddenError = Error.Forbidden("Auth.Forbidden", "Insufficient permissions");
        var errorResult = ErrorOrFactory.From<TestModel>(forbiddenError);

        // Act
        var result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        problemResult.ProblemDetails.Title.ShouldBe("Auth.Forbidden");
    }

    [Fact]
    public void ToTypedResult_WithConflictError_ReturnsConflictProblem()
    {
        // Arrange
        var conflictError = Error.Conflict("Resource.Conflict", "Resource already exists");
        var errorResult = ErrorOrFactory.From<TestModel>(conflictError);

        // Act
        var result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
        problemResult.ProblemDetails.Title.ShouldBe("Resource.Conflict");
    }

    [Fact]
    public void ToTypedResult_WithFailureError_ReturnsInternalServerErrorProblem()
    {
        // Arrange
        var failureError = Error.Failure("System.Failure", "Internal system error");
        var errorResult = ErrorOrFactory.From<TestModel>(failureError);

        // Act
        var result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        problemResult.ProblemDetails.Title.ShouldBe("System.Failure");
    }

    [Fact]
    public void ToTypedResult_WithUnexpectedError_ReturnsUnprocessableEntityProblem()
    {
        // Arrange
        var unexpectedError = Error.Unexpected("System.Unexpected", "Unexpected error occurred");
        var errorResult = ErrorOrFactory.From<TestModel>(unexpectedError);

        // Act
        var result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status422UnprocessableEntity);
        problemResult.ProblemDetails.Title.ShouldBe("System.Unexpected");
    }

    [Fact]
    public void ToTypedResult_WithMultipleValidationErrors_ReturnsValidationProblemWithAllErrors()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.Validation("Field1", "Field1 is required"),
            Error.Validation("Field2", "Field2 is invalid"),
            Error.Validation("Field1", "Field1 must be unique") // Same field, multiple errors
        };
        var errorResult = ErrorOrFactory.From<TestModel>(errors);

        // Act
        var result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        problemResult.ProblemDetails.Title.ShouldBe("Validation Failed");
        
        // Check that validation errors are properly grouped
        var validationProblemDetails = problemResult.ProblemDetails as HttpValidationProblemDetails;
        validationProblemDetails.ShouldNotBeNull();
        validationProblemDetails.Errors.ShouldContainKey("Field1");
        validationProblemDetails.Errors.ShouldContainKey("Field2");
        validationProblemDetails.Errors["Field1"].Length.ShouldBe(2); // Two errors for Field1
        validationProblemDetails.Errors["Field2"].Length.ShouldBe(1); // One error for Field2
    }

    #endregion

    #region ToTypedResultCreated Tests

    [Fact]
    public void ToTypedResultCreated_WithSuccessValue_ReturnsCreatedResult()
    {
        // Arrange
        var successResult = ErrorOrFactory.From(_testModel);

        // Act
        var result = successResult.ToTypedResultCreated(TestLocationUrl);

        // Assert
        result.ShouldBeOfType<Created<TestModel>>();
        var createdResult = (Created<TestModel>)result;
        createdResult.Value.ShouldBe(_testModel);
        createdResult.Location.ShouldBe(TestLocationUrl);
    }

    [Fact]
    public void ToTypedResultCreated_WithEmptyLocationUrl_ReturnsCreatedResultWithEmptyLocation()
    {
        // Arrange
        var successResult = ErrorOrFactory.From(_testModel);

        // Act
        var result = successResult.ToTypedResultCreated(string.Empty);

        // Assert
        result.ShouldBeOfType<Created<TestModel>>();
        var createdResult = (Created<TestModel>)result;
        createdResult.Location.ShouldBe(string.Empty);
    }

    [Fact]
    public void ToTypedResultCreated_WithError_ReturnsProblemDetails()
    {
        // Arrange
        var error = Error.Validation("Field", "Invalid field");
        var errorResult = ErrorOrFactory.From<TestModel>(error);

        // Act
        var result = errorResult.ToTypedResultCreated(TestLocationUrl);

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/api/resource/123")]
    [InlineData("https://example.com/resource/123")]
    public void ToTypedResultCreated_WithVariousLocationUrls_HandlesCorrectly(string? locationUrl)
    {
        // Arrange
        var successResult = ErrorOrFactory.From(_testModel);

        // Act
        var result = successResult.ToTypedResultCreated(locationUrl!);

        // Assert
        result.ShouldBeOfType<Created<TestModel>>();
        var createdResult = (Created<TestModel>)result;
        createdResult.Location.ShouldBe(locationUrl);
    }

    #endregion

    #region ToTypedResultNoContent Tests

    [Fact]
    public void ToTypedResultNoContent_WithSuccessUpdatedResult_ReturnsNoContentResult()
    {
        // Arrange
        var successResult = ErrorOrFactory.From(Result.Updated);

        // Act
        var result = successResult.ToTypedResultNoContent();

        // Assert
        result.ShouldBeOfType<NoContent>();
    }

    [Fact]
    public void ToTypedResultNoContent_WithError_ReturnsProblemDetails()
    {
        // Arrange
        var error = Error.NotFound("Resource.NotFound", "Resource not found");
        var errorResult = ErrorOrFactory.From<Updated>(error);

        // Act
        var result = errorResult.ToTypedResultNoContent();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    #endregion

    #region ToTypedResultDeleted Tests

    [Fact]
    public void ToTypedResultDeleted_WithSuccessDeletedResult_ReturnsNoContentResult()
    {
        // Arrange
        var successResult = ErrorOrFactory.From(Result.Deleted);

        // Act
        var result = successResult.ToTypedResultDeleted();

        // Assert
        result.ShouldBeOfType<NoContent>();
    }

    [Fact]
    public void ToTypedResultDeleted_WithError_ReturnsProblemDetails()
    {
        // Arrange
        var error = Error.NotFound("Resource.NotFound", "Resource not found for deletion");
        var errorResult = ErrorOrFactory.From<Deleted>(error);

        // Act
        var result = errorResult.ToTypedResultDeleted();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        problemResult.ProblemDetails.Detail.ShouldBe("Resource not found for deletion");
    }

    #endregion

    #region ToTypedResultAccepted Tests

    [Fact]
    public void ToTypedResultAccepted_WithSuccessValueAndLocation_ReturnsAcceptedResult()
    {
        // Arrange
        var successResult = ErrorOrFactory.From(_testModel);

        // Act
        var result = successResult.ToTypedResultAccepted(TestLocationUrl);

        // Assert
        result.ShouldBeOfType<Accepted<TestModel>>();
        var acceptedResult = (Accepted<TestModel>)result;
        acceptedResult.Value.ShouldBe(_testModel);
        acceptedResult.Location.ShouldBe(TestLocationUrl);
    }

    [Fact]
    public void ToTypedResultAccepted_WithSuccessValueAndNullLocation_ReturnsAcceptedResult()
    {
        // Arrange
        var successResult = ErrorOrFactory.From(_testModel);

        // Act
        var result = successResult.ToTypedResultAccepted(null);

        // Assert
        result.ShouldBeOfType<Accepted<TestModel>>();
        var acceptedResult = (Accepted<TestModel>)result;
        acceptedResult.Value.ShouldBe(_testModel);
        acceptedResult.Location.ShouldBeNull();
    }

    [Fact]
    public void ToTypedResultAccepted_WithSuccessValueAndNoLocation_ReturnsAcceptedResult()
    {
        // Arrange
        var successResult = ErrorOrFactory.From(_testModel);

        // Act
        var result = successResult.ToTypedResultAccepted();

        // Assert
        result.ShouldBeOfType<Accepted<TestModel>>();
        var acceptedResult = (Accepted<TestModel>)result;
        acceptedResult.Value.ShouldBe(_testModel);
    }

    [Fact]
    public void ToTypedResultAccepted_WithError_ReturnsProblemDetails()
    {
        // Arrange
        var error = Error.Failure("Processing.Failed", "Failed to process request");
        var errorResult = ErrorOrFactory.From<TestModel>(error);

        // Act
        var result = errorResult.ToTypedResultAccepted(TestLocationUrl);

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    #endregion

    #region ToProblemDetails Tests

    [Fact]
    public void ToProblemDetails_WithEmptyErrorList_ReturnsGenericProblem()
    {
        // Arrange
        var emptyErrors = new List<Error>();

        // Act
        var result = ErrorOrTypedResultsExtensions.ToProblemDetails(emptyErrors);

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        problemResult.ProblemDetails.Detail.ShouldBe("An unknown error occurred.");
    }

    [Fact]
    public void ToProblemDetails_WithSingleNonValidationError_ReturnsProblemDetails()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.NotFound("User.NotFound", "User with specified ID was not found")
        };

        // Act
        var result = ErrorOrTypedResultsExtensions.ToProblemDetails(errors);

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        problemResult.ProblemDetails.Title.ShouldBe("User.NotFound");
        problemResult.ProblemDetails.Detail.ShouldBe("User with specified ID was not found");
        problemResult.ProblemDetails.Type.ShouldBe("https://httpstatuses.com/404");
    }

    [Fact]
    public void ToProblemDetails_WithSingleValidationError_ReturnsValidationProblem()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.Validation("Email", "Email is required")
        };

        // Act
        var result = ErrorOrTypedResultsExtensions.ToProblemDetails(errors);

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        problemResult.ProblemDetails.Title.ShouldBe("Validation Failed");

        var validationProblemDetails = problemResult.ProblemDetails as HttpValidationProblemDetails;
        validationProblemDetails.ShouldNotBeNull();
        validationProblemDetails.Errors.ShouldContainKey("Email");
        validationProblemDetails.Errors["Email"].ShouldContain("Email is required");
    }

    [Fact]
    public void ToProblemDetails_WithMultipleValidationErrors_GroupsByPropertyName()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.Validation("Email", "Email is required"),
            Error.Validation("Email", "Email format is invalid"),
            Error.Validation("Password", "Password is required"),
            Error.Validation("Age", "Age must be positive")
        };

        // Act
        var result = ErrorOrTypedResultsExtensions.ToProblemDetails(errors);

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);

        var validationProblemDetails = problemResult.ProblemDetails as HttpValidationProblemDetails;
        validationProblemDetails.ShouldNotBeNull();

        // Verify Email has 2 errors
        validationProblemDetails.Errors.ShouldContainKey("Email");
        validationProblemDetails.Errors["Email"].Length.ShouldBe(2);
        validationProblemDetails.Errors["Email"].ShouldContain("Email is required");
        validationProblemDetails.Errors["Email"].ShouldContain("Email format is invalid");

        // Verify Password has 1 error
        validationProblemDetails.Errors.ShouldContainKey("Password");
        validationProblemDetails.Errors["Password"].Length.ShouldBe(1);

        // Verify Age has 1 error
        validationProblemDetails.Errors.ShouldContainKey("Age");
        validationProblemDetails.Errors["Age"].Length.ShouldBe(1);
    }

    #endregion

    #region Edge Cases and Complex Scenarios

    [Fact]
    public void ToTypedResult_WithCustomErrorType_ReturnsInternalServerError()
    {
        // Arrange - Create a custom error that doesn't map to any specific status code
        var customError = Error.Custom(999, "Custom.Error", "Custom error message");
        var errorResult = ErrorOrFactory.From<TestModel>(customError);

        // Act
        var result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        problemResult.ProblemDetails.Title.ShouldBe("Custom.Error");
    }

    [Fact]
    public void ToTypedResult_WithVeryLongErrorMessage_HandlesCorrectly()
    {
        // Arrange
        var longMessage = new string('A', 5000); // Very long error message
        var error = Error.Failure("Long.Error", longMessage);
        var errorResult = ErrorOrFactory.From<TestModel>(error);

        // Act
        var result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.ProblemDetails.Detail.ShouldBe(longMessage);
        problemResult.ProblemDetails.Detail!.Length.ShouldBe(5000);
    }

    [Fact]
    public void ToTypedResult_WithSpecialCharactersInErrorMessage_HandlesCorrectly()
    {
        // Arrange
        var specialMessage = "Error with special chars: <>&\"'åäö中文🚀";
        var error = Error.NotFound("Special.Error", specialMessage);
        var errorResult = ErrorOrFactory.From<TestModel>(error);

        // Act
        var result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.ProblemDetails.Detail.ShouldBe(specialMessage);
    }

    [Fact]
    public void ToTypedResultCreated_WithVeryLongLocationUrl_HandlesCorrectly()
    {
        // Arrange
        var successResult = ErrorOrFactory.From(_testModel);
        var longUrl = "https://example.com/" + new string('a', 2000);

        // Act
        var result = successResult.ToTypedResultCreated(longUrl);

        // Assert
        result.ShouldBeOfType<Created<TestModel>>();
        var createdResult = (Created<TestModel>)result;
        createdResult.Location.ShouldBe(longUrl);
    }

    [Fact]
    public void ToTypedResult_WithComplexGenericType_HandlesCorrectly()
    {
        // Arrange
        var complexModel = new Dictionary<string, List<TestModel>>
        {
            ["category1"] = new List<TestModel> { _testModel },
            ["category2"] = new List<TestModel> { new(2, "Test2", 200.0m) }
        };
        var successResult = ErrorOrFactory.From(complexModel);

        // Act
        var result = successResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<Ok<Dictionary<string, List<TestModel>>>>();
        var okResult = (Ok<Dictionary<string, List<TestModel>>>)result;
        okResult.ShouldNotBeNull();
        okResult.Value.ShouldNotBeNull();
        okResult.Value.ShouldBe(complexModel);
        okResult.Value.Count.ShouldBe(2);
    }

    [Fact]
    public void ToTypedResult_WithNullableValueType_HandlesCorrectly()
    {
        // Arrange
        int? nullableInt = null;
        var successResult = ErrorOrFactory.From(nullableInt);

        // Act
        var result = successResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<Ok<int?>>();
        var okResult = (Ok<int?>)result;
        okResult.Value.ShouldBeNull();
    }

    [Fact]
    public void ToTypedResult_WithNullableValueTypeWithValue_HandlesCorrectly()
    {
        // Arrange
        int? nullableInt = 42;
        var successResult = ErrorOrFactory.From(nullableInt);

        // Act
        var result = successResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<Ok<int?>>();
        var okResult = (Ok<int?>)result;
        okResult.Value.ShouldBe(42);
    }

    #endregion

    #region Test Models and Helpers

    private record TestModel(int Id, string Name, decimal Price);

    /// <summary>
    /// Helper class to create ErrorOr instances for testing
    /// </summary>
    private static class ErrorOrFactory
    {
        public static ErrorOr<T> From<T>(T value) => value;
        public static ErrorOr<T> From<T>(Error error) => error;
        public static ErrorOr<T> From<T>(List<Error> errors) => errors;
    }

    #endregion
}