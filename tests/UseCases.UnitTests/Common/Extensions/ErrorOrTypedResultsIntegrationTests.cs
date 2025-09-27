using ErrorOr;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

using Shouldly;

using UseCases.Common.Extensions;

namespace UseCases.UnitTests.Common.Extensions;

/// <summary>
/// Integration tests that verify the complete flow from service layer through ErrorOr extensions
/// These tests demonstrate real-world usage scenarios and edge cases
/// </summary>
public class ErrorOrTypedResultsIntegrationTests
{
    #region Complete Flow Tests

    [Fact]
    public void CompleteFlow_SuccessfulProductRetrieval_ReturnsCorrectResult()
    {
        // Arrange
        var product = new TestProductModel(1, "iPhone 15", 999.99m);
        var successResult = ErrorOrFactory.From(product);

        // Act - Simulate what happens in a real endpoint
        var httpResult = successResult.ToTypedResult();

        // Assert
        httpResult.ShouldBeOfType<Ok<TestProductModel>>();
        var okResult = (Ok<TestProductModel>)httpResult;
        okResult.Value.ShouldBe(product);
        okResult.Value!.Id.ShouldBe(1);
        okResult.Value.Name.ShouldBe("iPhone 15");
        okResult.Value.Price.ShouldBe(999.99m);
    }

    [Fact]
    public void CompleteFlow_ProductCreationWithValidation_ReturnsValidationProblem()
    {
        // Arrange - Simulate validation errors from service layer
        var validationErrors = new List<Error>
        {
            Error.Validation("Name", "Product name is required"),
            Error.Validation("Name", "Product name must be unique"),
            Error.Validation("Price", "Product price must be greater than 0"),
            Error.Validation("Category", "Product category is required")
        };
        var errorResult = ErrorOrFactory.From<TestProductModel>(validationErrors);

        // Act - Simulate what happens in a POST endpoint
        var httpResult = errorResult.ToTypedResultCreated("/api/products/1");

        // Assert
        httpResult.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)httpResult;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        problemResult.ProblemDetails.Title.ShouldBe("Validation Failed");

        var validationDetails = problemResult.ProblemDetails as HttpValidationProblemDetails;
        validationDetails.ShouldNotBeNull();
        
        // Verify that errors are properly grouped by field
        validationDetails.Errors.ShouldContainKey("Name");
        validationDetails.Errors.ShouldContainKey("Price");
        validationDetails.Errors.ShouldContainKey("Category");
        
        validationDetails.Errors["Name"].Length.ShouldBe(2);
        validationDetails.Errors["Price"].Length.ShouldBe(1);
        validationDetails.Errors["Category"].Length.ShouldBe(1);
    }

    [Fact]
    public void CompleteFlow_ProductNotFound_ReturnsNotFoundProblem()
    {
        // Arrange
        var notFoundError = Error.NotFound("Product.NotFound", "Product with ID 999 was not found");
        var errorResult = ErrorOrFactory.From<TestProductModel>(notFoundError);

        // Act
        var httpResult = errorResult.ToTypedResult();

        // Assert
        httpResult.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)httpResult;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        problemResult.ProblemDetails.Title.ShouldBe("Product.NotFound");
        problemResult.ProblemDetails.Detail.ShouldBe("Product with ID 999 was not found");
        problemResult.ProblemDetails.Type.ShouldBe("https://httpstatuses.com/404");
    }

    [Fact]
    public void CompleteFlow_ProductUpdateSuccess_ReturnsNoContent()
    {
        // Arrange
        var updateResult = ErrorOrFactory.From(Result.Updated);

        // Act
        var httpResult = updateResult.ToTypedResultNoContent();

        // Assert
        httpResult.ShouldBeOfType<NoContent>();
    }

    [Fact]
    public void CompleteFlow_ProductDeletionConflict_ReturnsConflictProblem()
    {
        // Arrange
        var conflictError = Error.Conflict("Product.InUse", "Cannot delete product that has active orders");
        var errorResult = ErrorOrFactory.From<Deleted>(conflictError);

        // Act
        var httpResult = errorResult.ToTypedResultDeleted();

        // Assert
        httpResult.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)httpResult;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
        problemResult.ProblemDetails.Title.ShouldBe("Product.InUse");
        problemResult.ProblemDetails.Detail.ShouldBe("Cannot delete product that has active orders");
    }

    #endregion

    #region Business Scenarios

    [Fact]
    public void BusinessScenario_CreateProductWithDuplicateName_ReturnsConflict()
    {
        // Arrange - Simulate business rule violation
        var duplicateNameError = Error.Conflict("Product.NameExists", "A product with name 'iPhone 15' already exists");
        var errorResult = ErrorOrFactory.From<TestProductModel>(duplicateNameError);

        // Act
        var httpResult = errorResult.ToTypedResultCreated("/api/products/new");

        // Assert
        httpResult.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)httpResult;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
        problemResult.ProblemDetails.Title.ShouldBe("Product.NameExists");
    }

    [Fact]
    public void BusinessScenario_UnauthorizedProductAccess_ReturnsUnauthorized()
    {
        // Arrange
        var unauthorizedError = Error.Unauthorized("Product.AccessDenied", "You don't have permission to access this product");
        var errorResult = ErrorOrFactory.From<TestProductModel>(unauthorizedError);

        // Act
        var httpResult = errorResult.ToTypedResult();

        // Assert
        httpResult.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)httpResult;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
        problemResult.ProblemDetails.Title.ShouldBe("Product.AccessDenied");
    }

    [Fact]
    public void BusinessScenario_ProductUpdateForbidden_ReturnsForbidden()
    {
        // Arrange
        var forbiddenError = Error.Forbidden("Product.UpdateForbidden", "Product is locked and cannot be modified");
        var errorResult = ErrorOrFactory.From<Updated>(forbiddenError);

        // Act
        var httpResult = errorResult.ToTypedResultNoContent();

        // Assert
        httpResult.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)httpResult;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        problemResult.ProblemDetails.Title.ShouldBe("Product.UpdateForbidden");
    }

    [Fact]
    public void BusinessScenario_SystemFailure_ReturnsInternalServerError()
    {
        // Arrange
        var systemError = Error.Failure("Database.ConnectionFailed", "Unable to connect to the database");
        var errorResult = ErrorOrFactory.From<TestProductModel>(systemError);

        // Act
        var httpResult = errorResult.ToTypedResult();

        // Assert
        httpResult.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)httpResult;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        problemResult.ProblemDetails.Title.ShouldBe("Database.ConnectionFailed");
    }

    #endregion

    #region Complex Multi-Error Scenarios

    [Fact]
    public void MultiErrorScenario_ComplexValidationWithMultipleFields_HandlesCorrectly()
    {
        // Arrange - Simulate complex validation with multiple errors per field
        var errors = new List<Error>
        {
            // Name validations
            Error.Validation("Name", "Name is required"),
            Error.Validation("Name", "Name must be at least 3 characters"),
            Error.Validation("Name", "Name contains invalid characters"),
            
            // Price validations
            Error.Validation("Price", "Price is required"),
            Error.Validation("Price", "Price must be greater than 0"),
            Error.Validation("Price", "Price cannot exceed $10,000"),
            
            // Category validations
            Error.Validation("CategoryId", "Category is required"),
            Error.Validation("CategoryId", "Category does not exist"),
            
            // SKU validations
            Error.Validation("SKU", "SKU is required"),
            Error.Validation("SKU", "SKU must be unique"),
            Error.Validation("SKU", "SKU format is invalid")
        };
        var errorResult = ErrorOrFactory.From<TestProductModel>(errors);

        // Act
        var httpResult = errorResult.ToTypedResultCreated("/api/products/new");

        // Assert
        httpResult.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)httpResult;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);

        var validationDetails = problemResult.ProblemDetails as HttpValidationProblemDetails;
        validationDetails.ShouldNotBeNull();
        
        // Verify error grouping
        validationDetails.Errors.ShouldContainKey("Name");
        validationDetails.Errors.ShouldContainKey("Price");
        validationDetails.Errors.ShouldContainKey("CategoryId");
        validationDetails.Errors.ShouldContainKey("SKU");
        
        validationDetails.Errors["Name"].Length.ShouldBe(3);
        validationDetails.Errors["Price"].Length.ShouldBe(3);
        validationDetails.Errors["CategoryId"].Length.ShouldBe(2);
        validationDetails.Errors["SKU"].Length.ShouldBe(3);
        
        // Verify specific error messages
        validationDetails.Errors["Name"].ShouldContain("Name is required");
        validationDetails.Errors["Name"].ShouldContain("Name must be at least 3 characters");
        validationDetails.Errors["Name"].ShouldContain("Name contains invalid characters");
    }

    [Fact]
    public void MultiErrorScenario_MixedErrorTypesWithValidationFirst_TreatsAsValidation()
    {
        // Arrange - First error is validation, so should be treated as validation problem
        var errors = new List<Error>
        {
            Error.Validation("Field1", "Validation error"),
            Error.NotFound("Resource.NotFound", "Resource not found"),
            Error.Conflict("Resource.Conflict", "Resource conflict"),
            Error.Failure("System.Error", "System error")
        };
        var errorResult = ErrorOrFactory.From<TestProductModel>(errors);

        // Act
        var httpResult = errorResult.ToTypedResult();

        // Assert
        httpResult.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)httpResult;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        problemResult.ProblemDetails.Title.ShouldBe("Validation Failed");
    }

    [Fact]
    public void MultiErrorScenario_MixedErrorTypesWithNonValidationFirst_UsesFirstErrorType()
    {
        // Arrange - First error is not validation, so should use that error type
        var errors = new List<Error>
        {
            Error.NotFound("Resource.NotFound", "Resource not found"),
            Error.Validation("Field1", "Validation error"),
            Error.Conflict("Resource.Conflict", "Resource conflict")
        };
        var errorResult = ErrorOrFactory.From<TestProductModel>(errors);

        // Act
        var httpResult = errorResult.ToTypedResult();

        // Assert
        httpResult.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)httpResult;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        problemResult.ProblemDetails.Title.ShouldBe("Resource.NotFound");
    }

    #endregion

    #region Async Workflow Simulations

    [Fact]
    public async Task AsyncWorkflow_ProductProcessingAccepted_ReturnsAcceptedWithLocation()
    {
        // Arrange - Simulate async processing scenario
        var processingResult = new ProductProcessingStatus(1, "Processing", "Product is being processed");
        var successResult = ErrorOrFactory.From(processingResult);

        // Act
        var httpResult = successResult.ToTypedResultAccepted("/api/products/1/status");

        // Assert
        httpResult.ShouldBeOfType<Accepted<ProductProcessingStatus>>();
        var acceptedResult = (Accepted<ProductProcessingStatus>)httpResult;
        acceptedResult.Value.ShouldBe(processingResult);
        acceptedResult.Location.ShouldBe("/api/products/1/status");
        
        await Task.CompletedTask; // Simulate async operation
    }

    [Fact]
    public async Task AsyncWorkflow_ProductProcessingFailed_ReturnsUnprocessableEntity()
    {
        // Arrange
        var processingError = Error.Unexpected("Processing.Failed", "Unexpected error during product processing");
        var errorResult = ErrorOrFactory.From<ProductProcessingStatus>(processingError);

        // Act
        var httpResult = errorResult.ToTypedResultAccepted("/api/products/1/status");

        // Assert
        httpResult.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)httpResult;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status422UnprocessableEntity);
        problemResult.ProblemDetails.Title.ShouldBe("Processing.Failed");
        
        await Task.CompletedTask; // Simulate async operation
    }

    #endregion

    #region Boundary Tests

    [Fact]
    public void BoundaryTest_EmptyErrorList_ReturnsGenericInternalServerError()
    {
        // Arrange
        var emptyErrors = new List<Error>();

        // Act
        var httpResult = ErrorOrTypedResultsExtensions.ToProblemDetails(emptyErrors);

        // Assert
        httpResult.ShouldBeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)httpResult;
        problemResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        problemResult.ProblemDetails.Detail.ShouldBe("An unknown error occurred.");
    }

    [Fact]
    public void BoundaryTest_NullableProductHandling_WorksCorrectly()
    {
        // Arrange
        TestProductModel? nullProduct = null;
        var successResult = ErrorOrFactory.From(nullProduct);

        // Act
        var httpResult = successResult.ToTypedResult();

        // Assert
        httpResult.ShouldBeOfType<Ok<TestProductModel?>>();
        var okResult = (Ok<TestProductModel?>)httpResult;
        okResult.Value.ShouldBeNull();
    }

    #endregion

    #region Test Helper Classes

    private record ProductProcessingStatus(int ProductId, string Status, string Message);

    private static class ErrorOrFactory
    {
        public static ErrorOr<T> From<T>(T value) => value;
        public static ErrorOr<T> From<T>(Error error) => error;
        public static ErrorOr<T> From<T>(List<Error> errors) => errors;
    }

    #endregion
}