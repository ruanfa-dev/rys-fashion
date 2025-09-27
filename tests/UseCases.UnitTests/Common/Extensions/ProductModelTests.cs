using Shouldly;

using UseCases.Common.Extensions;

namespace UseCases.UnitTests.Common.Extensions;

/// <summary>
/// Tests for the Product record and related DTOs used in TypedResults examples
/// </summary>
public class ProductModelTests
{
    #region Product Record Tests

    [Fact]
    public void Product_Constructor_WithAllParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var id = 1;
        var name = "Test Product";
        var price = 99.99m;

        // Act
        var product = new TestProductModel(id, name, price);

        // Assert
        product.Id.ShouldBe(id);
        product.Name.ShouldBe(name);
        product.Price.ShouldBe(price);
    }

    [Fact]
    public void Product_Constructor_WithNameAndPrice_SetsIdToZero()
    {
        // Arrange
        var name = "Test Product";
        var price = 99.99m;

        // Act
        var product = new TestProductModel(name, price);

        // Assert
        product.Id.ShouldBe(0);
        product.Name.ShouldBe(name);
        product.Price.ShouldBe(price);
    }

    [Fact]
    public void Product_UpdateName_ReturnsNewInstanceWithUpdatedName()
    {
        // Arrange
        var originalProduct = new TestProductModel(1, "Original Name", 99.99m);
        var newName = "Updated Name";

        // Act
        var updatedProduct = originalProduct.UpdateName(newName);

        // Assert
        updatedProduct.Id.ShouldBe(originalProduct.Id);
        updatedProduct.Name.ShouldBe(newName);
        updatedProduct.Price.ShouldBe(originalProduct.Price);
        
        // Original should remain unchanged (immutability)
        originalProduct.Name.ShouldBe("Original Name");
    }

    [Fact]
    public void Product_UpdateName_WithEmptyString_SetsEmptyName()
    {
        // Arrange
        var originalProduct = new TestProductModel(1, "Original Name", 99.99m);

        // Act
        var updatedProduct = originalProduct.UpdateName("");

        // Assert
        updatedProduct.Name.ShouldBe("");
    }

    [Fact]
    public void Product_UpdateName_WithNull_SetsNullName()
    {
        // Arrange
        var originalProduct = new TestProductModel(1, "Original Name", 99.99m);

        // Act
        var updatedProduct = originalProduct.UpdateName(null!);

        // Assert
        updatedProduct.Name.ShouldBeNull();
    }

    [Fact]
    public void Product_Equality_TwoProductsWithSameValues_AreEqual()
    {
        // Arrange
        var product1 = new TestProductModel(1, "Test Product", 99.99m);
        var product2 = new TestProductModel(1, "Test Product", 99.99m);

        // Act & Assert
        product1.ShouldBe(product2);
        product1.Equals(product2).ShouldBeTrue();
        (product1 == product2).ShouldBeTrue();
    }

    [Fact]
    public void Product_Equality_TwoProductsWithDifferentValues_AreNotEqual()
    {
        // Arrange
        var product1 = new TestProductModel(1, "Test Product", 99.99m);
        var product2 = new TestProductModel(2, "Test Product", 99.99m);

        // Act & Assert
        product1.ShouldNotBe(product2);
        product1.Equals(product2).ShouldBeFalse();
        (product1 != product2).ShouldBeTrue();
    }

    [Fact]
    public void Product_GetHashCode_TwoEqualProducts_HaveSameHashCode()
    {
        // Arrange
        var product1 = new TestProductModel(1, "Test Product", 99.99m);
        var product2 = new TestProductModel(1, "Test Product", 99.99m);

        // Act & Assert
        product1.GetHashCode().ShouldBe(product2.GetHashCode());
    }

    [Fact]
    public void Product_ToString_ReturnsExpectedFormat()
    {
        // Arrange
        var product = new TestProductModel(1, "Test Product", 99.99m);

        // Act
        var toString = product.ToString();

        // Assert
        toString.ShouldContain("1");
        toString.ShouldContain("Test Product");
        toString.ShouldContain("99.99");
    }

    #endregion

    #region CreateProductRequest Tests

    [Fact]
    public void CreateProductRequest_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var name = "Test Product";
        var price = 99.99m;

        // Act
        var request = new CreateProductRequest(name, price);

        // Assert
        request.Name.ShouldBe(name);
        request.Price.ShouldBe(price);
    }

    [Fact]
    public void CreateProductRequest_Equality_TwoRequestsWithSameValues_AreEqual()
    {
        // Arrange
        var request1 = new CreateProductRequest("Test Product", 99.99m);
        var request2 = new CreateProductRequest("Test Product", 99.99m);

        // Act & Assert
        request1.ShouldBe(request2);
        request1.Equals(request2).ShouldBeTrue();
    }

    [Fact]
    public void CreateProductRequest_Equality_TwoRequestsWithDifferentValues_AreNotEqual()
    {
        // Arrange
        var request1 = new CreateProductRequest("Test Product", 99.99m);
        var request2 = new CreateProductRequest("Different Product", 99.99m);

        // Act & Assert
        request1.ShouldNotBe(request2);
        request1.Equals(request2).ShouldBeFalse();
    }

    [Fact]
    public void CreateProductRequest_WithNullName_HandlesCorrectly()
    {
        // Arrange & Act
        var request = new CreateProductRequest(null!, 99.99m);

        // Assert
        request.Name.ShouldBeNull();
        request.Price.ShouldBe(99.99m);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void CreateProductRequest_WithWhitespaceName_HandlesCorrectly(string whitespace)
    {
        // Arrange & Act
        var request = new CreateProductRequest(whitespace, 99.99m);

        // Assert
        request.Name.ShouldBe(whitespace);
        request.Price.ShouldBe(99.99m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-99.99)]
    [InlineData(999999999.99)] // Use a large but valid decimal constant
    public void CreateProductRequest_WithVariousPrices_HandlesCorrectly(decimal price)
    {
        // Arrange & Act
        var request = new CreateProductRequest("Test Product", price);

        // Assert
        request.Name.ShouldBe("Test Product");
        request.Price.ShouldBe(price);
    }

    #endregion

    #region UpdateProductRequest Tests

    [Fact]
    public void UpdateProductRequest_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var name = "Updated Product";
        var price = 199.99m;

        // Act
        var request = new UpdateProductRequest(name, price);

        // Assert
        request.Name.ShouldBe(name);
        request.Price.ShouldBe(price);
    }

    [Fact]
    public void UpdateProductRequest_Equality_TwoRequestsWithSameValues_AreEqual()
    {
        // Arrange
        var request1 = new UpdateProductRequest("Updated Product", 199.99m);
        var request2 = new UpdateProductRequest("Updated Product", 199.99m);

        // Act & Assert
        request1.ShouldBe(request2);
        request1.Equals(request2).ShouldBeTrue();
    }

    [Fact]
    public void UpdateProductRequest_Equality_TwoRequestsWithDifferentValues_AreNotEqual()
    {
        // Arrange
        var request1 = new UpdateProductRequest("Updated Product", 199.99m);
        var request2 = new UpdateProductRequest("Updated Product", 299.99m);

        // Act & Assert
        request1.ShouldNotBe(request2);
        request1.Equals(request2).ShouldBeFalse();
    }

    [Fact]
    public void UpdateProductRequest_WithNullName_HandlesCorrectly()
    {
        // Arrange & Act
        var request = new UpdateProductRequest(null!, 199.99m);

        // Assert
        request.Name.ShouldBeNull();
        request.Price.ShouldBe(199.99m);
    }

    #endregion

    #region Edge Cases and Complex Scenarios

    [Fact]
    public void Product_WithExtremelyLongName_HandlesCorrectly()
    {
        // Arrange
        var longName = new string('A', 10000);

        // Act
        var product = new TestProductModel(1, longName, 99.99m);

        // Assert
        product.Name.ShouldBe(longName);
        product.Name.Length.ShouldBe(10000);
    }

    [Fact]
    public void Product_WithSpecialCharactersInName_HandlesCorrectly()
    {
        // Arrange
        var specialName = "Product with special chars: <>&\"'åäö中文🚀";

        // Act
        var product = new TestProductModel(1, specialName, 99.99m);

        // Assert
        product.Name.ShouldBe(specialName);
    }

    [Fact]
    public void Product_WithNegativeId_HandlesCorrectly()
    {
        // Arrange
        var negativeId = -1;

        // Act
        var product = new TestProductModel(negativeId, "Test Product", 99.99m);

        // Assert
        product.Id.ShouldBe(negativeId);
    }

    [Fact]
    public void Product_WithMaxIntId_HandlesCorrectly()
    {
        // Arrange
        var maxId = int.MaxValue;

        // Act
        var product = new TestProductModel(maxId, "Max Product", 99.99m);

        // Assert
        product.Id.ShouldBe(maxId);
        product.Price.ShouldBe(99.99m);
    }

    [Fact]
    public void Product_WithMinIntId_HandlesCorrectly()
    {
        // Arrange
        var minId = int.MinValue;

        // Act
        var product = new TestProductModel(minId, "Min Product", 99.99m);

        // Assert
        product.Id.ShouldBe(minId);
        product.Price.ShouldBe(99.99m);
    }

    [Fact]
    public void Product_UpdateName_ChainedUpdates_WorksCorrectly()
    {
        // Arrange
        var originalProduct = new TestProductModel(1, "Original", 99.99m);

        // Act
        var updated1 = originalProduct.UpdateName("First Update");
        var updated2 = updated1.UpdateName("Second Update");
        var updated3 = updated2.UpdateName("Final Update");

        // Assert
        originalProduct.Name.ShouldBe("Original");
        updated1.Name.ShouldBe("First Update");
        updated2.Name.ShouldBe("Second Update");
        updated3.Name.ShouldBe("Final Update");
        
        // All should have same Id and Price
        updated1.Id.ShouldBe(originalProduct.Id);
        updated2.Id.ShouldBe(originalProduct.Id);
        updated3.Id.ShouldBe(originalProduct.Id);
        
        updated1.Price.ShouldBe(originalProduct.Price);
        updated2.Price.ShouldBe(originalProduct.Price);
        updated3.Price.ShouldBe(originalProduct.Price);
    }

    [Fact]
    public void Product_Deconstruction_WorksCorrectly()
    {
        // Arrange
        var product = new TestProductModel(42, "Deconstructed Product", 299.99m);

        // Act
        var (id, name, price) = product;

        // Assert
        id.ShouldBe(42);
        name.ShouldBe("Deconstructed Product");
        price.ShouldBe(299.99m);
    }

    [Fact]
    public void CreateProductRequest_Deconstruction_WorksCorrectly()
    {
        // Arrange
        var request = new CreateProductRequest("Create Product", 399.99m);

        // Act
        var (name, price) = request;

        // Assert
        name.ShouldBe("Create Product");
        price.ShouldBe(399.99m);
    }

    [Fact]
    public void UpdateProductRequest_Deconstruction_WorksCorrectly()
    {
        // Arrange
        var request = new UpdateProductRequest("Update Product", 499.99m);

        // Act
        var (name, price) = request;

        // Assert
        name.ShouldBe("Update Product");
        price.ShouldBe(499.99m);
    }

    #endregion
}