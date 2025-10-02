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
        ErrorOr<TestModel> successResult = ErrorOrFactory.From(_testModel);

        // Act
        IResult result = successResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<Ok<TestModel>>();
        Ok<TestModel> okResult = (Ok<TestModel>)result;
        okResult.Value.ShouldBe(_testModel);
    }

    [Fact]
    public void ToTypedResult_WithNullValue_ReturnsOkResultWithNull()
    {
        // Arrange
        TestModel? nullModel = null;
        ErrorOr<TestModel?> successResult = ErrorOrFactory.From(nullModel);

        // Act
        IResult result = successResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<Ok<TestModel?>>();
        Ok<TestModel?> okResult = (Ok<TestModel?>)result;
        okResult.Value.ShouldBeNull();
    }

    [Fact]
    public void ToTypedResult_WithValidationError_ReturnsValidationProblem()
    {
        // Arrange
        Error validationError = Error.Validation("TestField", "Validation failed");
        ErrorOr<TestModel> errorResult = ErrorOrFactory.From<TestModel>(validationError);

        // Act
        IResult result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        problemResult.ProblemDetails.Title.ShouldBe("Validation Failed");
    }

    [Fact]
    public void ToTypedResult_WithNotFoundError_ReturnsNotFoundProblem()
    {
        // Arrange
        Error notFoundError = Error.NotFound("Resource.NotFound", "Resource was not found");
        ErrorOr<TestModel> errorResult = ErrorOrFactory.From<TestModel>(notFoundError);

        // Act
        IResult result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        problemResult.ProblemDetails.Title.ShouldBe("Resource.NotFound");
        problemResult.ProblemDetails.Detail.ShouldBe("Resource was not found");
    }

    [Fact]
    public void ToTypedResult_WithUnauthorizedError_ReturnsUnauthorizedProblem()
    {
        // Arrange
        Error unauthorizedError = Error.Unauthorized("Auth.Unauthorized", "Access denied");
        ErrorOr<TestModel> errorResult = ErrorOrFactory.From<TestModel>(unauthorizedError);

        // Act
        IResult result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
        problemResult.ProblemDetails.Title.ShouldBe("Auth.Unauthorized");
    }

    [Fact]
    public void ToTypedResult_WithForbiddenError_ReturnsForbiddenProblem()
    {
        // Arrange
        Error forbiddenError = Error.Forbidden("Auth.Forbidden", "Insufficient permissions");
        ErrorOr<TestModel> errorResult = ErrorOrFactory.From<TestModel>(forbiddenError);

        // Act
        IResult result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        problemResult.ProblemDetails.Title.ShouldBe("Auth.Forbidden");
    }

    [Fact]
    public void ToTypedResult_WithConflictError_ReturnsConflictProblem()
    {
        // Arrange
        Error conflictError = Error.Conflict("Resource.Conflict", "Resource already exists");
        ErrorOr<TestModel> errorResult = ErrorOrFactory.From<TestModel>(conflictError);

        // Act
        IResult result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
        problemResult.ProblemDetails.Title.ShouldBe("Resource.Conflict");
    }

    [Fact]
    public void ToTypedResult_WithFailureError_ReturnsInternalServerErrorProblem()
    {
        // Arrange
        Error failureError = Error.Failure("System.Failure", "Internal system error");
        ErrorOr<TestModel> errorResult = ErrorOrFactory.From<TestModel>(failureError);

        // Act
        IResult result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        problemResult.ProblemDetails.Title.ShouldBe("System.Failure");
    }

    [Fact]
    public void ToTypedResult_WithUnexpectedError_ReturnsUnprocessableEntityProblem()
    {
        // Arrange
        Error unexpectedError = Error.Unexpected("System.Unexpected", "Unexpected error occurred");
        ErrorOr<TestModel> errorResult = ErrorOrFactory.From<TestModel>(unexpectedError);

        // Act
        IResult result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status422UnprocessableEntity);
        problemResult.ProblemDetails.Title.ShouldBe("System.Unexpected");
    }

    [Fact]
    public void ToTypedResult_WithMultipleValidationErrors_ReturnsValidationProblemWithAllErrors()
    {
        // Arrange
        List<Error> errors =
        [
            Error.Validation("Field1", "Field1 is required"),
            Error.Validation("Field2", "Field2 is invalid"),
            Error.Validation("Field1", "Field1 must be unique")
        ];
        ErrorOr<TestModel> errorResult = ErrorOrFactory.From<TestModel>(errors);

        // Act
        IResult result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        problemResult.ProblemDetails.Title.ShouldBe("Validation Failed");
        
        // Check that validation errors are properly grouped
        HttpValidationProblemDetails? validationProblemDetails = problemResult.ProblemDetails as HttpValidationProblemDetails;
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
        ErrorOr<TestModel> successResult = ErrorOrFactory.From(_testModel);

        // Act
        IResult result = successResult.ToTypedResultCreated(TestLocationUrl);

        // Assert
        result.ShouldBeOfType<Created<TestModel>>();
        Created<TestModel> createdResult = (Created<TestModel>)result;
        createdResult.Value.ShouldBe(_testModel);
        createdResult.Location.ShouldBe(TestLocationUrl);
    }

    [Fact]
    public void ToTypedResultCreated_WithEmptyLocationUrl_ReturnsCreatedResultWithEmptyLocation()
    {
        // Arrange
        ErrorOr<TestModel> successResult = ErrorOrFactory.From(_testModel);

        // Act
        IResult result = successResult.ToTypedResultCreated(string.Empty);

        // Assert
        result.ShouldBeOfType<Created<TestModel>>();
        Created<TestModel> createdResult = (Created<TestModel>)result;
        createdResult.Location.ShouldBe(string.Empty);
    }

    [Fact]
    public void ToTypedResultCreated_WithError_ReturnsProblemDetails()
    {
        // Arrange
        Error error = Error.Validation("Field", "Invalid field");
        ErrorOr<TestModel> errorResult = ErrorOrFactory.From<TestModel>(error);

        // Act
        IResult result = errorResult.ToTypedResultCreated(TestLocationUrl);

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
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
        ErrorOr<TestModel> successResult = ErrorOrFactory.From(_testModel);

        // Act
        IResult result = successResult.ToTypedResultCreated(locationUrl!);

        // Assert
        result.ShouldBeOfType<Created<TestModel>>();
        Created<TestModel> createdResult = (Created<TestModel>)result;
        createdResult.Location.ShouldBe(locationUrl);
    }

    #endregion

    #region ToTypedResultNoContent Tests

    [Fact]
    public void ToTypedResultNoContent_WithSuccessUpdatedResult_ReturnsNoContentResult()
    {
        // Arrange
        ErrorOr<Updated> successResult = ErrorOrFactory.From(Result.Updated);

        // Act
        IResult result = successResult.ToTypedResultNoContent();

        // Assert
        result.ShouldBeOfType<NoContent>();
    }

    [Fact]
    public void ToTypedResultNoContent_WithError_ReturnsProblemDetails()
    {
        // Arrange
        Error error = Error.NotFound("Resource.NotFound", "Resource not found");
        ErrorOr<Updated> errorResult = ErrorOrFactory.From<Updated>(error);

        // Act
        IResult result = errorResult.ToTypedResultNoContent();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    #endregion

    #region ToTypedResultDeleted Tests

    [Fact]
    public void ToTypedResultDeleted_WithSuccessDeletedResult_ReturnsNoContentResult()
    {
        // Arrange
        ErrorOr<Deleted> successResult = ErrorOrFactory.From(Result.Deleted);

        // Act
        IResult result = successResult.ToTypedResultDeleted();

        // Assert
        result.ShouldBeOfType<NoContent>();
    }

    [Fact]
    public void ToTypedResultDeleted_WithError_ReturnsProblemDetails()
    {
        // Arrange
        Error error = Error.NotFound("Resource.NotFound", "Resource not found for deletion");
        ErrorOr<Deleted> errorResult = ErrorOrFactory.From<Deleted>(error);

        // Act
        IResult result = errorResult.ToTypedResultDeleted();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        problemResult.ProblemDetails.Detail.ShouldBe("Resource not found for deletion");
    }

    #endregion

    #region ToTypedResultAccepted Tests

    [Fact]
    public void ToTypedResultAccepted_WithSuccessValueAndLocation_ReturnsAcceptedResult()
    {
        // Arrange
        ErrorOr<TestModel> successResult = ErrorOrFactory.From(_testModel);

        // Act
        IResult result = successResult.ToTypedResultAccepted(TestLocationUrl);

        // Assert
        result.ShouldBeOfType<Accepted<TestModel>>();
        Accepted<TestModel> acceptedResult = (Accepted<TestModel>)result;
        acceptedResult.Value.ShouldBe(_testModel);
        acceptedResult.Location.ShouldBe(TestLocationUrl);
    }

    [Fact]
    public void ToTypedResultAccepted_WithSuccessValueAndNullLocation_ReturnsAcceptedResult()
    {
        // Arrange
        ErrorOr<TestModel> successResult = ErrorOrFactory.From(_testModel);

        // Act
        IResult result = successResult.ToTypedResultAccepted(null);

        // Assert
        result.ShouldBeOfType<Accepted<TestModel>>();
        Accepted<TestModel> acceptedResult = (Accepted<TestModel>)result;
        acceptedResult.Value.ShouldBe(_testModel);
        acceptedResult.Location.ShouldBeNull();
    }

    [Fact]
    public void ToTypedResultAccepted_WithSuccessValueAndNoLocation_ReturnsAcceptedResult()
    {
        // Arrange
        ErrorOr<TestModel> successResult = ErrorOrFactory.From(_testModel);

        // Act
        IResult result = successResult.ToTypedResultAccepted();

        // Assert
        result.ShouldBeOfType<Accepted<TestModel>>();
        Accepted<TestModel> acceptedResult = (Accepted<TestModel>)result;
        acceptedResult.Value.ShouldBe(_testModel);
    }

    [Fact]
    public void ToTypedResultAccepted_WithError_ReturnsProblemDetails()
    {
        // Arrange
        Error error = Error.Failure("Processing.Failed", "Failed to process request");
        ErrorOr<TestModel> errorResult = ErrorOrFactory.From<TestModel>(error);

        // Act
        IResult result = errorResult.ToTypedResultAccepted(TestLocationUrl);

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    #endregion

    #region ToProblemDetails Tests

    [Fact]
    public void ToProblemDetails_WithEmptyErrorList_ReturnsGenericProblem()
    {
        // Arrange
        List<Error> emptyErrors = [];

        // Act
        IResult result = ErrorOrTypedResultsExtensions.ToProblemDetails(emptyErrors);

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        problemResult.ProblemDetails.Detail.ShouldBe("An unknown error occurred.");
    }

    [Fact]
    public void ToProblemDetails_WithSingleNonValidationError_ReturnsProblemDetails()
    {
        // Arrange
        List<Error> errors = [Error.NotFound("User.NotFound", "User with specified ID was not found")];

        // Act
        IResult result = ErrorOrTypedResultsExtensions.ToProblemDetails(errors);

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        problemResult.ProblemDetails.Title.ShouldBe("User.NotFound");
        problemResult.ProblemDetails.Detail.ShouldBe("User with specified ID was not found");
        problemResult.ProblemDetails.Type.ShouldBe("https://httpstatuses.com/404");
    }

    [Fact]
    public void ToProblemDetails_WithSingleValidationError_ReturnsValidationProblem()
    {
        // Arrange
        List<Error> errors = [Error.Validation("Email", "Email is required")];

        // Act
        IResult result = ErrorOrTypedResultsExtensions.ToProblemDetails(errors);

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        problemResult.ProblemDetails.Title.ShouldBe("Validation Failed");

        HttpValidationProblemDetails? validationProblemDetails = problemResult.ProblemDetails as HttpValidationProblemDetails;
        validationProblemDetails.ShouldNotBeNull();
        validationProblemDetails.Errors.ShouldContainKey("Email");
        validationProblemDetails.Errors["Email"].ShouldContain("Email is required");
    }

    [Fact]
    public void ToProblemDetails_WithMultipleValidationErrors_GroupsByPropertyName()
    {
        // Arrange
        List<Error> errors =
        [
            Error.Validation("Email", "Email is required"),
            Error.Validation("Email", "Email format is invalid"),
            Error.Validation("Password", "Password is required"),
            Error.Validation("Age", "Age must be positive")
        ];

        // Act
        IResult result = ErrorOrTypedResultsExtensions.ToProblemDetails(errors);

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);

        HttpValidationProblemDetails? validationProblemDetails = problemResult.ProblemDetails as HttpValidationProblemDetails;
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
        Error customError = Error.Custom(999, "Custom.Error", "Custom error message");
        ErrorOr<TestModel> errorResult = ErrorOrFactory.From<TestModel>(customError);

        // Act
        IResult result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        problemResult.ProblemDetails.Title.ShouldBe("Custom.Error");
    }

    [Fact]
    public void ToTypedResult_WithVeryLongErrorMessage_HandlesCorrectly()
    {
        // Arrange
        string longMessage = new string('A', 5000); // Very long error message
        Error error = Error.Failure("Long.Error", longMessage);
        ErrorOr<TestModel> errorResult = ErrorOrFactory.From<TestModel>(error);

        // Act
        IResult result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
        problemResult.ProblemDetails.Detail.ShouldBe(longMessage);
        problemResult.ProblemDetails.Detail!.Length.ShouldBe(5000);
    }

    [Fact]
    public void ToTypedResult_WithSpecialCharactersInErrorMessage_HandlesCorrectly()
    {
        // Arrange
        string specialMessage = "Error with special chars: <>&\"'åäö中文🚀";
        Error error = Error.NotFound("Special.Error", specialMessage);
        ErrorOr<TestModel> errorResult = ErrorOrFactory.From<TestModel>(error);

        // Act
        IResult result = errorResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        ProblemHttpResult problemResult = (ProblemHttpResult)result;
        problemResult.ProblemDetails.Detail.ShouldBe(specialMessage);
    }

    [Fact]
    public void ToTypedResultCreated_WithVeryLongLocationUrl_HandlesCorrectly()
    {
        // Arrange
        ErrorOr<TestModel> successResult = ErrorOrFactory.From(_testModel);
        string longUrl = "https://example.com/" + new string('a', 2000);

        // Act
        IResult result = successResult.ToTypedResultCreated(longUrl);

        // Assert
        result.ShouldBeOfType<Created<TestModel>>();
        Created<TestModel> createdResult = (Created<TestModel>)result;
        createdResult.Location.ShouldBe(longUrl);
    }

    [Fact]
    public void ToTypedResult_WithComplexGenericType_HandlesCorrectly()
    {
        // Arrange
        Dictionary<string, List<TestModel>> complexModel = new Dictionary<string, List<TestModel>>
        {
            ["category1"] = [_testModel],
            ["category2"] = [new(2, "Test2", 200.0m)]
        };
        ErrorOr<Dictionary<string, List<TestModel>>> successResult = ErrorOrFactory.From(complexModel);

        // Act
        IResult result = successResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<Ok<Dictionary<string, List<TestModel>>>>();
        Ok<Dictionary<string, List<TestModel>>> okResult = (Ok<Dictionary<string, List<TestModel>>>)result;
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
        ErrorOr<int?> successResult = ErrorOrFactory.From(nullableInt);

        // Act
        IResult result = successResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<Ok<int?>>();
        Ok<int?> okResult = (Ok<int?>)result;
        okResult.Value.ShouldBeNull();
    }

    [Fact]
    public void ToTypedResult_WithNullableValueTypeWithValue_HandlesCorrectly()
    {
        // Arrange
        int? nullableInt = 42;
        ErrorOr<int?> successResult = ErrorOrFactory.From(nullableInt);

        // Act
        IResult result = successResult.ToTypedResult();

        // Assert
        result.ShouldBeOfType<Ok<int?>>();
        Ok<int?> okResult = (Ok<int?>)result;
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