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
        int id = 1;
        string name = "Test Product";
        decimal price = 99.99m;

        // Act
        TestProductModel product = new TestProductModel(id, name, price);

        // Assert
        product.Id.ShouldBe(id);
        product.Name.ShouldBe(name);
        product.Price.ShouldBe(price);
    }

    [Fact]
    public void Product_Constructor_WithNameAndPrice_SetsIdToZero()
    {
        // Arrange
        string name = "Test Product";
        decimal price = 99.99m;

        // Act
        TestProductModel product = new TestProductModel(name, price);

        // Assert
        product.Id.ShouldBe(0);
        product.Name.ShouldBe(name);
        product.Price.ShouldBe(price);
    }

    [Fact]
    public void Product_UpdateName_ReturnsNewInstanceWithUpdatedName()
    {
        // Arrange
        TestProductModel originalProduct = new TestProductModel(1, "Original Name", 99.99m);
        string newName = "Updated Name";

        // Act
        TestProductModel updatedProduct = originalProduct.UpdateName(newName);

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
        TestProductModel originalProduct = new TestProductModel(1, "Original Name", 99.99m);

        // Act
        TestProductModel updatedProduct = originalProduct.UpdateName("");

        // Assert
        updatedProduct.Name.ShouldBe("");
    }

    [Fact]
    public void Product_UpdateName_WithNull_SetsNullName()
    {
        // Arrange
        TestProductModel originalProduct = new TestProductModel(1, "Original Name", 99.99m);

        // Act
        TestProductModel updatedProduct = originalProduct.UpdateName(null!);

        // Assert
        updatedProduct.Name.ShouldBeNull();
    }

    [Fact]
    public void Product_Equality_TwoProductsWithSameValues_AreEqual()
    {
        // Arrange
        TestProductModel product1 = new TestProductModel(1, "Test Product", 99.99m);
        TestProductModel product2 = new TestProductModel(1, "Test Product", 99.99m);

        // Act & Assert
        product1.ShouldBe(product2);
        product1.Equals(product2).ShouldBeTrue();
        (product1 == product2).ShouldBeTrue();
    }

    [Fact]
    public void Product_Equality_TwoProductsWithDifferentValues_AreNotEqual()
    {
        // Arrange
        TestProductModel product1 = new TestProductModel(1, "Test Product", 99.99m);
        TestProductModel product2 = new TestProductModel(2, "Test Product", 99.99m);

        // Act & Assert
        product1.ShouldNotBe(product2);
        product1.Equals(product2).ShouldBeFalse();
        (product1 != product2).ShouldBeTrue();
    }

    [Fact]
    public void Product_GetHashCode_TwoEqualProducts_HaveSameHashCode()
    {
        // Arrange
        TestProductModel product1 = new TestProductModel(1, "Test Product", 99.99m);
        TestProductModel product2 = new TestProductModel(1, "Test Product", 99.99m);

        // Act & Assert
        product1.GetHashCode().ShouldBe(product2.GetHashCode());
    }

    [Fact]
    public void Product_ToString_ReturnsExpectedFormat()
    {
        // Arrange
        TestProductModel product = new TestProductModel(1, "Test Product", 99.99m);

        // Act
        string? toString = product.ToString();

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
        string name = "Test Product";
        decimal price = 99.99m;

        // Act
        CreateProductRequest request = new CreateProductRequest(name, price);

        // Assert
        request.Name.ShouldBe(name);
        request.Price.ShouldBe(price);
    }

    [Fact]
    public void CreateProductRequest_Equality_TwoRequestsWithSameValues_AreEqual()
    {
        // Arrange
        CreateProductRequest request1 = new CreateProductRequest("Test Product", 99.99m);
        CreateProductRequest request2 = new CreateProductRequest("Test Product", 99.99m);

        // Act & Assert
        request1.ShouldBe(request2);
        request1.Equals(request2).ShouldBeTrue();
    }

    [Fact]
    public void CreateProductRequest_Equality_TwoRequestsWithDifferentValues_AreNotEqual()
    {
        // Arrange
        CreateProductRequest request1 = new CreateProductRequest("Test Product", 99.99m);
        CreateProductRequest request2 = new CreateProductRequest("Different Product", 99.99m);

        // Act & Assert
        request1.ShouldNotBe(request2);
        request1.Equals(request2).ShouldBeFalse();
    }

    [Fact]
    public void CreateProductRequest_WithNullName_HandlesCorrectly()
    {
        // Arrange & Act
        CreateProductRequest request = new CreateProductRequest(null!, 99.99m);

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
        CreateProductRequest request = new CreateProductRequest(whitespace, 99.99m);

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
        CreateProductRequest request = new CreateProductRequest("Test Product", price);

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
        string name = "Updated Product";
        decimal price = 199.99m;

        // Act
        UpdateProductRequest request = new UpdateProductRequest(name, price);

        // Assert
        request.Name.ShouldBe(name);
        request.Price.ShouldBe(price);
    }

    [Fact]
    public void UpdateProductRequest_Equality_TwoRequestsWithSameValues_AreEqual()
    {
        // Arrange
        UpdateProductRequest request1 = new UpdateProductRequest("Updated Product", 199.99m);
        UpdateProductRequest request2 = new UpdateProductRequest("Updated Product", 199.99m);

        // Act & Assert
        request1.ShouldBe(request2);
        request1.Equals(request2).ShouldBeTrue();
    }

    [Fact]
    public void UpdateProductRequest_Equality_TwoRequestsWithDifferentValues_AreNotEqual()
    {
        // Arrange
        UpdateProductRequest request1 = new UpdateProductRequest("Updated Product", 199.99m);
        UpdateProductRequest request2 = new UpdateProductRequest("Updated Product", 299.99m);

        // Act & Assert
        request1.ShouldNotBe(request2);
        request1.Equals(request2).ShouldBeFalse();
    }

    [Fact]
    public void UpdateProductRequest_WithNullName_HandlesCorrectly()
    {
        // Arrange & Act
        UpdateProductRequest request = new UpdateProductRequest(null!, 199.99m);

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
        string longName = new string('A', 10000);

        // Act
        TestProductModel product = new TestProductModel(1, longName, 99.99m);

        // Assert
        product.Name.ShouldBe(longName);
        product.Name.Length.ShouldBe(10000);
    }

    [Fact]
    public void Product_WithSpecialCharactersInName_HandlesCorrectly()
    {
        // Arrange
        string specialName = "Product with special chars: <>&\"'åäö中文🚀";

        // Act
        TestProductModel product = new TestProductModel(1, specialName, 99.99m);

        // Assert
        product.Name.ShouldBe(specialName);
    }

    [Fact]
    public void Product_WithNegativeId_HandlesCorrectly()
    {
        // Arrange
        int negativeId = -1;

        // Act
        TestProductModel product = new TestProductModel(negativeId, "Test Product", 99.99m);

        // Assert
        product.Id.ShouldBe(negativeId);
    }

    [Fact]
    public void Product_WithMaxIntId_HandlesCorrectly()
    {
        // Arrange
        int maxId = int.MaxValue;

        // Act
        TestProductModel product = new TestProductModel(maxId, "Max Product", 99.99m);

        // Assert
        product.Id.ShouldBe(maxId);
        product.Price.ShouldBe(99.99m);
    }

    [Fact]
    public void Product_WithMinIntId_HandlesCorrectly()
    {
        // Arrange
        int minId = int.MinValue;

        // Act
        TestProductModel product = new TestProductModel(minId, "Min Product", 99.99m);

        // Assert
        product.Id.ShouldBe(minId);
        product.Price.ShouldBe(99.99m);
    }

    [Fact]
    public void Product_UpdateName_ChainedUpdates_WorksCorrectly()
    {
        // Arrange
        TestProductModel originalProduct = new TestProductModel(1, "Original", 99.99m);

        // Act
        TestProductModel updated1 = originalProduct.UpdateName("First Update");
        TestProductModel updated2 = updated1.UpdateName("Second Update");
        TestProductModel updated3 = updated2.UpdateName("Final Update");

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
        TestProductModel product = new TestProductModel(42, "Deconstructed Product", 299.99m);

        // Act
        (int id, string name, decimal price) = product;

        // Assert
        id.ShouldBe(42);
        name.ShouldBe("Deconstructed Product");
        price.ShouldBe(299.99m);
    }

    [Fact]
    public void CreateProductRequest_Deconstruction_WorksCorrectly()
    {
        // Arrange
        CreateProductRequest request = new CreateProductRequest("Create Product", 399.99m);

        // Act
        (string name, decimal price) = request;

        // Assert
        name.ShouldBe("Create Product");
        price.ShouldBe(399.99m);
    }

    [Fact]
    public void UpdateProductRequest_Deconstruction_WorksCorrectly()
    {
        // Arrange
        UpdateProductRequest request = new UpdateProductRequest("Update Product", 499.99m);

        // Act
        (string name, decimal price) = request;

        // Assert
        name.ShouldBe("Update Product");
        price.ShouldBe(499.99m);
    }

    #endregion
}