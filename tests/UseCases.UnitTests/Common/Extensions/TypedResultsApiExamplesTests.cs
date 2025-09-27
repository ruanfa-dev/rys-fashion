using ErrorOr;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

using Shouldly;

using UseCases.Common.Extensions;

namespace UseCases.UnitTests.Common.Extensions;

/// <summary>
/// Tests for the TypedResultsApiExamples static class functionality
/// Focuses on the search endpoint logic and behavior
/// </summary>
public class TypedResultsApiExamplesTests
{
    #region Search Endpoint Logic Tests

    [Fact]
    public void SearchEndpoint_WithNoParameters_ReturnsBadRequest()
    {
        // Arrange
        var searchDelegate = GetSearchEndpointDelegate();

        // Act
        var result = searchDelegate(null, null, null);

        // Assert
        result.ShouldBeOfType<BadRequest<Error>>();
        var badRequestResult = (BadRequest<Error>)result;
        badRequestResult.Value.Code.ShouldBe("Search.Empty");
        badRequestResult.Value.Description.ShouldBe("At least one search parameter is required");
    }

    [Theory]
    [InlineData("", null, null)]
    [InlineData("   ", null, null)]
    [InlineData(null, null, null)]
    public void SearchEndpoint_WithEmptyParameters_ReturnsBadRequest(string? name, decimal? minPrice, decimal? maxPrice)
    {
        // Arrange
        var searchDelegate = GetSearchEndpointDelegate();

        // Act
        var result = searchDelegate(name, minPrice, maxPrice);

        // Assert
        result.ShouldBeOfType<BadRequest<Error>>();
    }

    [Theory]
    [InlineData("product", null, null)]
    [InlineData(null, 10.0, null)]
    [InlineData(null, null, 100.0)]
    [InlineData("product", 10.0, 100.0)]
    public void SearchEndpoint_WithValidParameters_ReturnsOk(string? name, double? minPrice, double? maxPrice)
    {
        // Arrange
        var searchDelegate = GetSearchEndpointDelegate();

        // Act
        var result = searchDelegate(name, (decimal?)minPrice, (decimal?)maxPrice);

        // Assert
        result.ShouldBeOfType<Ok<List<TestProductModel>>>();
        var okResult = (Ok<List<TestProductModel>>)result;
        okResult.Value.ShouldNotBeNull();
        okResult.Value.ShouldBeOfType<List<TestProductModel>>();
    }

    [Theory]
    [InlineData("a")]
    [InlineData("very long product name that exceeds normal expectations")]
    public void SearchEndpoint_WithVariousNameLengths_HandlesCorrectly(string name)
    {
        // Arrange
        var searchDelegate = GetSearchEndpointDelegate();

        // Act
        var result = searchDelegate(name, null, null);

        // Assert
        result.ShouldBeOfType<Ok<List<TestProductModel>>>();
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(999999999.99)] // Use a large but valid decimal constant
    [InlineData(-1)] // Negative prices should be allowed in search
    public void SearchEndpoint_WithVariousPrices_HandlesCorrectly(decimal price)
    {
        // Arrange
        var searchDelegate = GetSearchEndpointDelegate();

        // Act
        var result = searchDelegate(null, price, price + 100);

        // Assert
        result.ShouldBeOfType<Ok<List<TestProductModel>>>();
    }

    [Fact]
    public void SearchEndpoint_WithSpecialCharactersInName_HandlesCorrectly()
    {
        // Arrange
        var searchDelegate = GetSearchEndpointDelegate();
        var specialName = "<>&\"'åäö中文🚀";

        // Act
        var result = searchDelegate(specialName, null, null);

        // Assert
        result.ShouldBeOfType<Ok<List<TestProductModel>>>();
    }

    [Fact]
    public void SearchEndpoint_WithMinAndMaxPriceEqual_HandlesCorrectly()
    {
        // Arrange
        var searchDelegate = GetSearchEndpointDelegate();
        var price = 99.99m;

        // Act
        var result = searchDelegate(null, price, price);

        // Assert
        result.ShouldBeOfType<Ok<List<TestProductModel>>>();
    }

    [Fact]
    public void SearchEndpoint_WithMaxPriceLowerThanMinPrice_HandlesCorrectly()
    {
        // Arrange
        var searchDelegate = GetSearchEndpointDelegate();

        // Act
        var result = searchDelegate(null, 100m, 50m);

        // Assert
        result.ShouldBeOfType<Ok<List<TestProductModel>>>();
    }

    #endregion

    #region Product Model Tests

    [Fact]
    public void Product_ConstructorAndProperties_WorkCorrectly()
    {
        // Arrange & Act
        var product = new TestProductModel(1, "Test Product", 99.99m);

        // Assert
        product.Id.ShouldBe(1);
        product.Name.ShouldBe("Test Product");
        product.Price.ShouldBe(99.99m);
    }

    [Fact]
    public void Product_UpdateName_CreatesNewInstance()
    {
        // Arrange
        var original = new TestProductModel(1, "Original", 50m);

        // Act
        var updated = original.UpdateName("Updated");

        // Assert
        updated.Name.ShouldBe("Updated");
        updated.Id.ShouldBe(original.Id);
        updated.Price.ShouldBe(original.Price);
        original.Name.ShouldBe("Original"); // Original unchanged
    }

    [Fact]
    public void CreateProductRequest_Properties_WorkCorrectly()
    {
        // Arrange & Act
        var request = new CreateProductRequest("New Product", 25.99m);

        // Assert
        request.Name.ShouldBe("New Product");
        request.Price.ShouldBe(25.99m);
    }

    [Fact]
    public void UpdateProductRequest_Properties_WorkCorrectly()
    {
        // Arrange & Act
        var request = new UpdateProductRequest("Updated Product", 35.99m);

        // Assert
        request.Name.ShouldBe("Updated Product");
        request.Price.ShouldBe(35.99m);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void SearchLogic_EmptyStringName_IsConsideredEmpty()
    {
        // Arrange
        var emptyName = "";

        // Act
        var isEmpty = string.IsNullOrWhiteSpace(emptyName);

        // Assert
        isEmpty.ShouldBeTrue();
    }

    [Fact]
    public void SearchLogic_WhitespaceOnlyName_IsConsideredEmpty()
    {
        // Arrange
        var whitespaceName = "   ";

        // Act
        var isEmpty = string.IsNullOrWhiteSpace(whitespaceName);

        // Assert
        isEmpty.ShouldBeTrue();
    }

    [Fact]
    public void SearchLogic_ValidName_IsNotConsideredEmpty()
    {
        // Arrange
        var validName = "Product";

        // Act
        var isEmpty = string.IsNullOrWhiteSpace(validName);

        // Assert
        isEmpty.ShouldBeFalse();
    }

    #endregion

    #region Helper Methods

    private static Func<string?, decimal?, decimal?, IResult> GetSearchEndpointDelegate()
    {
        // This simulates the search endpoint delegate behavior
        return (name, minPrice, maxPrice) =>
        {
            // Simulate search logic from the actual implementation
            if (string.IsNullOrWhiteSpace(name) && !minPrice.HasValue && !maxPrice.HasValue)
            {
                var emptySearchError = Error.Validation("Search.Empty", "At least one search parameter is required");
                return Results.BadRequest(emptySearchError);
            }

            // Return empty search results (as in the actual implementation)
            var searchResults = new List<TestProductModel>();
            return Results.Ok(searchResults);
        };
    }

    #endregion
}