using System.Collections.Generic;
using System.Threading.Tasks;
using ErrorOr;
using Shouldly;
using UseCases.Common.Extensions;
using Xunit;

namespace UseCases.UnitTests.Common.Extensions;

/// <summary>
/// Tests for the example service class TypedResultsExampleService
/// Validates business logic and error handling scenarios
/// </summary>
public class TypedResultsExampleServiceTests
{
    private readonly TypedResultsExampleService _service;

    public TypedResultsExampleServiceTests()
    {
        _service = new TypedResultsExampleService();
    }

    #region GetProductByIdAsync Tests

    [Fact]
    public async Task GetProductByIdAsync_WithValidId_CallsFindProductInDatabase()
    {
        // Arrange
        var validId = 1;

        // Act & Assert - This will throw NotImplementedException as expected
        // since it's a placeholder implementation
        await Should.ThrowAsync<NotImplementedException>(
            async () => await _service.GetProductByIdAsync(validId)
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetProductByIdAsync_WithInvalidId_ReturnsValidationError(int invalidId)
    {
        // Act
        var result = await _service.GetProductByIdAsync(invalidId);
        
        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
        result.FirstError.Code.ShouldBe("Product.Id");
        result.FirstError.Description.ShouldBe("Product ID must be greater than 0");
    }

    [Fact]
    public async Task GetProductByIdAsync_WithPositiveId_DoesNotReturnValidationError()
    {
        // Arrange
        var validId = 1;

        // Act & Assert - Should not fail on validation, but will throw on database access
        await Should.ThrowAsync<NotImplementedException>(
            async () => await _service.GetProductByIdAsync(validId)
        );
    }

    #endregion

    #region CreateProductAsync Tests

    [Fact]
    public async Task CreateProductAsync_WithValidRequest_CallsValidationAndDatabase()
    {
        // Arrange
        var validRequest = new CreateProductRequest("Valid Product", 99.99m);

        // Act & Assert - Will throw NotImplementedException on database access
        await Should.ThrowAsync<NotImplementedException>(
            async () => await _service.CreateProductAsync(validRequest)
        );
    }

    [Fact]
    public async Task CreateProductAsync_WithEmptyName_ReturnsValidationError()
    {
        // Arrange
        var invalidRequest = new CreateProductRequest("", 99.99m);

        // Act
        var result = await _service.CreateProductAsync(invalidRequest);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(e => 
            e.Type == ErrorType.Validation && 
            e.Code == "Name" && 
            e.Description == "Product name is required");
    }

    [Fact]
    public async Task CreateProductAsync_WithWhitespaceOnlyName_ReturnsValidationError()
    {
        // Arrange
        var invalidRequest = new CreateProductRequest("   ", 99.99m);

        // Act
        var result = await _service.CreateProductAsync(invalidRequest);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(e => 
            e.Type == ErrorType.Validation && 
            e.Code == "Name" && 
            e.Description == "Product name is required");
    }

    [Fact]
    public async Task CreateProductAsync_WithNullName_ReturnsValidationError()
    {
        // Arrange
        var invalidRequest = new CreateProductRequest(null!, 99.99m);

        // Act
        var result = await _service.CreateProductAsync(invalidRequest);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(e => 
            e.Type == ErrorType.Validation && 
            e.Code == "Name" && 
            e.Description == "Product name is required");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public async Task CreateProductAsync_WithInvalidPrice_ReturnsValidationError(decimal invalidPrice)
    {
        // Arrange
        var invalidRequest = new CreateProductRequest("Valid Name", invalidPrice);

        // Act
        var result = await _service.CreateProductAsync(invalidRequest);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(e => 
            e.Type == ErrorType.Validation && 
            e.Code == "Price" && 
            e.Description == "Product price must be greater than 0");
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(1)]
    [InlineData(99.99)]
    [InlineData(1000000)]
    public async Task CreateProductAsync_WithValidPrice_DoesNotReturnPriceValidationError(decimal validPrice)
    {
        // Arrange
        var validRequest = new CreateProductRequest("Valid Name", validPrice);

        // Act & Assert - Should not fail on price validation, but will throw on database access
        await Should.ThrowAsync<NotImplementedException>(
            async () => await _service.CreateProductAsync(validRequest)
        );
    }

    [Fact]
    public async Task CreateProductAsync_WithMultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange
        var invalidRequest = new CreateProductRequest("", -10m);

        // Act
        var result = await _service.CreateProductAsync(invalidRequest);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Errors.Count.ShouldBe(2);
        
        result.Errors.ShouldContain(e => 
            e.Type == ErrorType.Validation && 
            e.Code == "Name" && 
            e.Description == "Product name is required");
            
        result.Errors.ShouldContain(e => 
            e.Type == ErrorType.Validation && 
            e.Code == "Price" && 
            e.Description == "Product price must be greater than 0");
    }

    [Fact]
    public async Task CreateProductAsync_WithBothNullNameAndZeroPrice_ReturnsAllValidationErrors()
    {
        // Arrange
        var invalidRequest = new CreateProductRequest(null!, 0m);

        // Act
        var result = await _service.CreateProductAsync(invalidRequest);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Errors.Count.ShouldBe(2);
        
        result.Errors.ShouldContain(e => e.Code == "Name");
        result.Errors.ShouldContain(e => e.Code == "Price");
    }

    #endregion

    #region UpdateProductAsync Tests

    [Fact]
    public async Task UpdateProductAsync_WithValidIdAndRequest_CallsGetProductFirst()
    {
        // Arrange
        var validId = 1;
        var validRequest = new UpdateProductRequest("Updated Name", 199.99m);

        // Act & Assert - Will fail when trying to get the product (NotImplementedException)
        await Should.ThrowAsync<NotImplementedException>(
            async () => await _service.UpdateProductAsync(validId, validRequest)
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task UpdateProductAsync_WithInvalidId_ReturnsValidationErrorFromGetProduct(int invalidId)
    {
        // Arrange
        var validRequest = new UpdateProductRequest("Updated Name", 199.99m);

        // Act
        var result = await _service.UpdateProductAsync(invalidId, validRequest);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
        result.FirstError.Code.ShouldBe("Product.Id");
        result.FirstError.Description.ShouldBe("Product ID must be greater than 0");
    }

    #endregion

    #region DeleteProductAsync Tests

    [Fact]
    public async Task DeleteProductAsync_WithValidId_CallsGetProductFirst()
    {
        // Arrange
        var validId = 1;

        // Act & Assert - Will fail when trying to get the product (NotImplementedException)
        await Should.ThrowAsync<NotImplementedException>(
            async () => await _service.DeleteProductAsync(validId)
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task DeleteProductAsync_WithInvalidId_ReturnsValidationErrorFromGetProduct(int invalidId)
    {
        // Act
        var result = await _service.DeleteProductAsync(invalidId);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
        result.FirstError.Code.ShouldBe("Product.Id");
        result.FirstError.Description.ShouldBe("Product ID must be greater than 0");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CreateProductAsync_WithVeryLargePrice_DoesNotReturnValidationError()
    {
        // Arrange
        var request = new CreateProductRequest("Expensive Item", 999999999.99m);

        // Act & Assert - Should not fail validation, but will throw on database access
        await Should.ThrowAsync<NotImplementedException>(
            async () => await _service.CreateProductAsync(request)
        );
    }

    [Fact]
    public async Task CreateProductAsync_WithMinimalValidPrice_DoesNotReturnValidationError()
    {
        // Arrange
        var request = new CreateProductRequest("Cheap Item", 0.01m);

        // Act & Assert - Should not fail validation, but will throw on database access
        await Should.ThrowAsync<NotImplementedException>(
            async () => await _service.CreateProductAsync(request)
        );
    }

    [Fact]
    public async Task CreateProductAsync_WithVeryLongName_DoesNotReturnValidationError()
    {
        // Arrange
        var longName = new string('A', 1000);
        var request = new CreateProductRequest(longName, 99.99m);

        // Act & Assert - Should not fail validation, but will throw on database access
        await Should.ThrowAsync<NotImplementedException>(
            async () => await _service.CreateProductAsync(request)
        );
    }

    [Fact]
    public async Task CreateProductAsync_WithSpecialCharactersInName_DoesNotReturnValidationError()
    {
        // Arrange
        var specialName = "Product with special chars: <>&\"'едц????";
        var request = new CreateProductRequest(specialName, 99.99m);

        // Act & Assert - Should not fail validation, but will throw on database access
        await Should.ThrowAsync<NotImplementedException>(
            async () => await _service.CreateProductAsync(request)
        );
    }

    #endregion
}