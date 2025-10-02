using Core.Catalog.Taxonomies;

using ErrorOr;

using Shouldly;

namespace Core.UnitTests.Catalogs.Taxonomies;

public class TaxonTests
{
    #region Create Tests

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        string name = "Electronics";
        Guid taxonomyId = Guid.NewGuid();

        // Act
        ErrorOr<Taxon> result = Taxon.Create(name, taxonomyId);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe(name);
        result.Value.TaxonomyId.ShouldBe(taxonomyId);
        result.Value.IsRoot.ShouldBeTrue();
        result.Value.ParentId.ShouldBeNull();
        result.Value.Automatic.ShouldBeFalse();
        result.Value.RulesMatchPolicy.ShouldBe("all");
        result.Value.SortOrder.ShouldBe("manual");
        result.Value.Permalink.ShouldBe("electronics");
        result.Value.PrettyName.ShouldBe(name);
    }

    [Fact]
    public void Create_WithParentId_ShouldSucceed()
    {
        // Arrange
        string name = "Laptops";
        Guid taxonomyId = Guid.NewGuid();
        Guid parentId = Guid.NewGuid();

        // Act
        ErrorOr<Taxon> result = Taxon.Create(name, taxonomyId, parentId);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ParentId.ShouldBe(parentId);
        result.Value.IsRoot.ShouldBeFalse();
    }

    [Fact]
    public void Create_WithAutomaticTrue_ShouldSucceed()
    {
        // Arrange
        string name = "Featured Products";
        Guid taxonomyId = Guid.NewGuid();

        // Act
        ErrorOr<Taxon> result = Taxon.Create(name, taxonomyId, automatic: true);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Automatic.ShouldBeTrue();
        result.Value.IsManual.ShouldBeFalse();
    }

    [Fact]
    public void Create_WithCustomRulesMatchPolicy_ShouldSucceed()
    {
        // Arrange
        string name = "Sale Items";
        Guid taxonomyId = Guid.NewGuid();

        // Act
        ErrorOr<Taxon> result = Taxon.Create(name, taxonomyId, automatic: true, rulesMatchPolicy: "any");

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.RulesMatchPolicy.ShouldBe("any");
    }

    [Fact]
    public void Create_WithMetadata_ShouldSucceed()
    {
        // Arrange
        string name = "Gaming";
        Guid taxonomyId = Guid.NewGuid();
        var publicMetadata = new Dictionary<string, string?> { ["featured"] = "true" };
        var privateMetadata = new Dictionary<string, string?> { ["internal_code"] = "GM001" };

        // Act
        ErrorOr<Taxon> result = Taxon.Create(
            name,
            taxonomyId,
            publicMetadata: publicMetadata,
            privateMetadata: privateMetadata);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.PublicMetadata?.ShouldContainKey("featured");
        result.Value.PublicMetadata?["featured"].ShouldBe("true");
        result.Value.PrivateMetadata?.ShouldContainKey("internal_code");
    }

    [Fact]
    public void Create_WithImageUrls_ShouldSucceed()
    {
        // Arrange
        string name = "Smartphones";
        Guid taxonomyId = Guid.NewGuid();
        string imageUrl = "https://example.com/image.jpg";
        string squareImageUrl = "https://example.com/square.jpg";

        // Act
        ErrorOr<Taxon> result = Taxon.Create(
            name,
            taxonomyId,
            imageUrl: imageUrl,
            squareImageUrl: squareImageUrl);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ImageUrl.ShouldBe(imageUrl);
        result.Value.SquareImageUrl.ShouldBe(squareImageUrl);
        result.Value.PageBuilderImageUrl.ShouldBe(squareImageUrl);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_Name_ShouldSucceed()
    {
        // Arrange
        var taxon = Taxon.Create("Old Name", Guid.NewGuid()).Value;
        string newName = "New Name";

        // Act
        ErrorOr<Taxon> result = taxon.Update(name: newName);

        // Assert
        result.IsError.ShouldBeFalse();
        taxon.Name.ShouldBe(newName);
    }

    [Fact]
    public void Update_Description_ShouldSucceed()
    {
        // Arrange
        var taxon = Taxon.Create("Electronics", Guid.NewGuid()).Value;
        string description = "All electronic devices";

        // Act
        ErrorOr<Taxon> result = taxon.Update(description: description);

        // Assert
        result.IsError.ShouldBeFalse();
        taxon.Description.ShouldBe(description);
    }

    [Fact]
    public void Update_Automatic_ShouldSucceed()
    {
        // Arrange
        var taxon = Taxon.Create("Products", Guid.NewGuid()).Value;
        taxon.Automatic.ShouldBeFalse();

        // Act
        ErrorOr<Taxon> result = taxon.Update(automatic: true);

        // Assert
        result.IsError.ShouldBeFalse();
        taxon.Automatic.ShouldBeTrue();
    }

    [Fact]
    public void Update_RulesMatchPolicy_ShouldSucceed()
    {
        // Arrange
        var taxon = Taxon.Create("Products", Guid.NewGuid(), automatic: true).Value;

        // Act
        ErrorOr<Taxon> result = taxon.Update(rulesMatchPolicy: "any");

        // Assert
        result.IsError.ShouldBeFalse();
        taxon.RulesMatchPolicy.ShouldBe("any");
    }

    [Fact]
    public void Update_MetaFields_ShouldSucceed()
    {
        // Arrange
        var taxon = Taxon.Create("Products", Guid.NewGuid()).Value;

        // Act
        ErrorOr<Taxon> result = taxon.Update(
            metaTitle: "Products | Store",
            metaDescription: "Browse our products",
            metaKeywords: "products, store, shop");

        // Assert
        result.IsError.ShouldBeFalse();
        taxon.MetaTitle.ShouldBe("Products | Store");
        taxon.MetaDescription.ShouldBe("Browse our products");
        taxon.MetaKeywords.ShouldBe("products, store, shop");
        taxon.SeoTitle.ShouldBe("Products | Store");
    }

    [Fact]
    public void Update_ImageUrls_ShouldSucceed()
    {
        // Arrange
        var taxon = Taxon.Create("Products", Guid.NewGuid()).Value;

        // Act
        ErrorOr<Taxon> result = taxon.Update(
            imageUrl: "https://example.com/new.jpg",
            squareImageUrl: "https://example.com/new-square.jpg");

        // Assert
        result.IsError.ShouldBeFalse();
        taxon.ImageUrl.ShouldBe("https://example.com/new.jpg");
        taxon.SquareImageUrl.ShouldBe("https://example.com/new-square.jpg");
    }

    [Fact]
    public void Update_WithSelfParentId_ShouldReturnError()
    {
        // Arrange
        var taxon = Taxon.Create("Products", Guid.NewGuid()).Value;

        // Act
        ErrorOr<Taxon> result = taxon.Update(parentId: taxon.Id);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(Taxon.Errors.SelfParent);
    }

    #endregion

    #region SetParent Tests

    [Fact]
    public void SetParent_WithValidParent_ShouldSucceed()
    {
        // Arrange
        Guid taxonomyId = Guid.NewGuid();
        var parent = Taxon.Create("Electronics", taxonomyId).Value;
        var child = Taxon.Create("Laptops", taxonomyId).Value;

        // Act
        ErrorOr<Taxon> result = child.SetParent(parent);

        // Assert
        result.IsError.ShouldBeFalse();
        child.ParentId.ShouldBe(parent.Id);
        child.Parent.ShouldBe(parent);
        parent.Children.ShouldContain(child);
    }

    [Fact]
    public void SetParent_WithSelfAsParent_ShouldReturnError()
    {
        // Arrange
        var taxon = Taxon.Create("Products", Guid.NewGuid()).Value;

        // Act
        ErrorOr<Taxon> result = taxon.SetParent(taxon);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(Taxon.Errors.SelfParent);
    }

    [Fact]
    public void SetParent_WithDifferentTaxonomy_ShouldReturnError()
    {
        // Arrange
        var parent = Taxon.Create("Parent", Guid.NewGuid()).Value;
        var child = Taxon.Create("Child", Guid.NewGuid()).Value;

        // Act
        ErrorOr<Taxon> result = child.SetParent(parent);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(Taxon.Errors.ParentTaxonomyMismatch);
    }

    [Fact]
    public void SetParent_WithCircularReference_ShouldReturnError()
    {
        // Arrange
        Guid taxonomyId = Guid.NewGuid();
        var grandparent = Taxon.Create("Grandparent", taxonomyId).Value;
        var parent = Taxon.Create("Parent", taxonomyId).Value;
        var child = Taxon.Create("Child", taxonomyId).Value;

        parent.SetParent(grandparent);
        child.SetParent(parent);

        // Act - Try to set grandparent as child of child (circular)
        ErrorOr<Taxon> result = grandparent.SetParent(child);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(Taxon.Errors.CircularReference);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public void Delete_WithNoChildren_ShouldSucceed()
    {
        // Arrange
        var taxon = Taxon.Create("Products", Guid.NewGuid()).Value;

        // Act
        ErrorOr<Deleted> result = taxon.Delete();

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact]
    public void Delete_WithChildren_ShouldReturnError()
    {
        // Arrange
        Guid taxonomyId = Guid.NewGuid();
        var parent = Taxon.Create("Parent", taxonomyId).Value;
        var child = Taxon.Create("Child", taxonomyId).Value;
        child.SetParent(parent);

        // Act
        ErrorOr<Deleted> result = parent.Delete();

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(Taxon.Errors.HasChildren);
    }

    [Fact]
    public void Delete_WithClassifications_ShouldReturnError()
    {
        // Arrange
        var taxon = Taxon.Create("Products", Guid.NewGuid()).Value;
        var productId = Guid.NewGuid();
        taxon.ClassifyProduct(productId);

        // Act
        ErrorOr<Deleted> result = taxon.Delete();

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(Taxon.Errors.HasClassifications);
    }

    #endregion

    #region Hierarchy Tests

    [Fact]
    public void IsAncestorOf_WithDirectChild_ShouldReturnTrue()
    {
        // Arrange
        Guid taxonomyId = Guid.NewGuid();
        var parent = Taxon.Create("Parent", taxonomyId).Value;
        var child = Taxon.Create("Child", taxonomyId).Value;
        child.SetParent(parent);

        // Act
        bool result = parent.IsAncestorOf(child);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsAncestorOf_WithGrandchild_ShouldReturnTrue()
    {
        // Arrange
        Guid taxonomyId = Guid.NewGuid();
        var grandparent = Taxon.Create("Grandparent", taxonomyId).Value;
        var parent = Taxon.Create("Parent", taxonomyId).Value;
        var child = Taxon.Create("Child", taxonomyId).Value;

        parent.SetParent(grandparent);
        child.SetParent(parent);

        // Act
        bool result = grandparent.IsAncestorOf(child);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsDescendantOf_WithDirectParent_ShouldReturnTrue()
    {
        // Arrange
        Guid taxonomyId = Guid.NewGuid();
        var parent = Taxon.Create("Parent", taxonomyId).Value;
        var child = Taxon.Create("Child", taxonomyId).Value;
        child.SetParent(parent);

        // Act
        bool result = child.IsDescendantOf(parent);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void GetLevel_ForRoot_ShouldReturnZero()
    {
        // Arrange
        var root = Taxon.Create("Root", Guid.NewGuid()).Value;

        // Act
        int level = root.GetLevel();

        // Assert
        level.ShouldBe(0);
    }

    [Fact]
    public void GetLevel_ForDirectChild_ShouldReturnOne()
    {
        // Arrange
        Guid taxonomyId = Guid.NewGuid();
        var parent = Taxon.Create("Parent", taxonomyId).Value;
        var child = Taxon.Create("Child", taxonomyId).Value;
        child.SetParent(parent);

        // Act
        int level = child.GetLevel();

        // Assert
        level.ShouldBe(1);
    }

    [Fact]
    public void GetAncestors_ShouldReturnInCorrectOrder()
    {
        // Arrange
        Guid taxonomyId = Guid.NewGuid();
        var grandparent = Taxon.Create("Grandparent", taxonomyId).Value;
        var parent = Taxon.Create("Parent", taxonomyId).Value;
        var child = Taxon.Create("Child", taxonomyId).Value;

        parent.SetParent(grandparent);
        child.SetParent(parent);

        // Act
        var ancestors = child.GetAncestors().ToList();

        // Assert
        ancestors.Count.ShouldBe(2);
        ancestors[0].ShouldBe(grandparent);
        ancestors[1].ShouldBe(parent);
    }

    [Fact]
    public void GetAllDescendants_ShouldReturnAllChildren()
    {
        // Arrange
        Guid taxonomyId = Guid.NewGuid();
        var root = Taxon.Create("Root", taxonomyId).Value;
        var child1 = Taxon.Create("Child1", taxonomyId).Value;
        var child2 = Taxon.Create("Child2", taxonomyId).Value;
        var grandchild = Taxon.Create("Grandchild", taxonomyId).Value;

        child1.SetParent(root);
        child2.SetParent(root);
        grandchild.SetParent(child1);

        // Act
        var descendants = root.GetAllDescendants().ToList();

        // Assert
        descendants.Count.ShouldBe(3);
        descendants.ShouldContain(child1);
        descendants.ShouldContain(child2);
        descendants.ShouldContain(grandchild);
    }

    #endregion

    #region Product Classification Tests

    [Fact]
    public void ClassifyProduct_WithValidProductId_ShouldSucceed()
    {
        // Arrange
        var taxon = Taxon.Create("Electronics", Guid.NewGuid()).Value;
        Guid productId = Guid.NewGuid();

        // Act
        ErrorOr<Taxon> result = taxon.ClassifyProduct(productId);

        // Assert
        result.IsError.ShouldBeFalse();
        taxon.Classifications.Count.ShouldBe(1);
        taxon.Classifications.First().ProductId.ShouldBe(productId);
    }

    [Fact]
    public void ClassifyProduct_WithEmptyGuid_ShouldReturnError()
    {
        // Arrange
        var taxon = Taxon.Create("Electronics", Guid.NewGuid()).Value;

        // Act
        ErrorOr<Taxon> result = taxon.ClassifyProduct(Guid.Empty);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(Taxon.Errors.NullProduct);
    }

    [Fact]
    public void ClassifyProduct_WithDuplicateProduct_ShouldReturnError()
    {
        // Arrange
        var taxon = Taxon.Create("Electronics", Guid.NewGuid()).Value;
        Guid productId = Guid.NewGuid();
        taxon.ClassifyProduct(productId);

        // Act
        ErrorOr<Taxon> result = taxon.ClassifyProduct(productId);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(Taxon.Errors.ProductAlreadyClassified);
    }

    [Fact]
    public void UnclassifyProduct_WithExistingClassification_ShouldSucceed()
    {
        // Arrange
        var taxon = Taxon.Create("Electronics", Guid.NewGuid()).Value;
        Guid productId = Guid.NewGuid();
        taxon.ClassifyProduct(productId);

        // Act
        ErrorOr<Taxon> result = taxon.UnclassifyProduct(productId);

        // Assert
        result.IsError.ShouldBeFalse();
        taxon.Classifications.ShouldBeEmpty();
    }

    [Fact]
    public void UnclassifyProduct_WithNonExistentClassification_ShouldReturnError()
    {
        // Arrange
        var taxon = Taxon.Create("Electronics", Guid.NewGuid()).Value;
        Guid productId = Guid.NewGuid();

        // Act
        ErrorOr<Taxon> result = taxon.UnclassifyProduct(productId);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(Taxon.Errors.ProductNotClassified);
    }

    #endregion

    #region Slug Generation Tests

    [Fact]
    public void GenerateSlug_ForRootTaxon_ShouldBeParameterized()
    {
        // Arrange
        var taxon = Taxon.Create("Product Categories", Guid.NewGuid()).Value;

        // Act
        string slug = taxon.GenerateSlug();

        // Assert
        slug.ShouldBe("product-categories");
    }

    [Fact]
    public void GenerateSlug_WithParent_ShouldIncludeParentPath()
    {
        // Arrange
        Guid taxonomyId = Guid.NewGuid();
        var parent = Taxon.Create("Electronics", taxonomyId).Value;
        var child = Taxon.Create("Laptops", taxonomyId).Value;
        child.SetParent(parent);

        // Act
        string slug = child.GenerateSlug();

        // Assert
        slug.ShouldBe("electronics/laptops");
    }

    [Fact]
    public void SetPermalink_ShouldGenerateSlug()
    {
        // Arrange
        var taxon = Taxon.Create("Gaming Consoles", Guid.NewGuid()).Value;

        // Act
        taxon.SetPermalink();

        // Assert
        taxon.Permalink.ShouldBe("gaming-consoles");
    }

    [Fact]
    public void GeneratePrettyName_ForRootTaxon_ShouldBeSimpleName()
    {
        // Arrange
        var taxon = Taxon.Create("Electronics", Guid.NewGuid()).Value;

        // Act
        string prettyName = taxon.GeneratePrettyName();

        // Assert
        prettyName.ShouldBe("Electronics");
    }

    [Fact]
    public void GeneratePrettyName_WithParent_ShouldIncludeHierarchy()
    {
        // Arrange
        Guid taxonomyId = Guid.NewGuid();
        var parent = Taxon.Create("Electronics", taxonomyId).Value;
        var child = Taxon.Create("Laptops", taxonomyId).Value;
        child.SetParent(parent);

        // Act
        string prettyName = child.GeneratePrettyName();

        // Assert
        prettyName.ShouldBe("Electronics -> Laptops");
    }

    #endregion

    #region Nested Set Tests

    [Fact]
    public void UpdateNestedSetValues_WithValidValues_ShouldSucceed()
    {
        // Arrange
        var taxon = Taxon.Create("Products", Guid.NewGuid()).Value;

        // Act
        ErrorOr<Success> result = taxon.UpdateNestedSetValues(1, 10, 0);

        // Assert
        result.IsError.ShouldBeFalse();
        taxon.Lft.ShouldBe(1);
        taxon.Rgt.ShouldBe(10);
        taxon.Depth.ShouldBe(0);
    }

    [Fact]
    public void UpdateNestedSetValues_WithInvalidRange_ShouldReturnError()
    {
        // Arrange
        var taxon = Taxon.Create("Products", Guid.NewGuid()).Value;

        // Act
        ErrorOr<Success> result = taxon.UpdateNestedSetValues(10, 5, 0);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(Taxon.Errors.InvalidNestedSetValues);
    }

    [Fact]
    public void UpdateNestedSetValues_WithInvalidDepth_ShouldReturnError()
    {
        // Arrange
        var taxon = Taxon.Create("Products", Guid.NewGuid()).Value;

        // Act
        ErrorOr<Success> result = taxon.UpdateNestedSetValues(1, 10, 25);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(Taxon.Errors.InvalidDepth);
    }

    #endregion

    #region Child Index Tests

    [Fact]
    public void UpdateChildIndex_WithValidIndex_ShouldSucceed()
    {
        // Arrange
        var taxon = Taxon.Create("Products", Guid.NewGuid()).Value;

        // Act
        ErrorOr<Success> result = taxon.UpdateChildIndex(5);

        // Assert
        result.IsError.ShouldBeFalse();
        taxon.ChildIndex.ShouldBe(5);
    }

    [Fact]
    public void UpdateChildIndex_WithNegativeIndex_ShouldReturnError()
    {
        // Arrange
        var taxon = Taxon.Create("Products", Guid.NewGuid()).Value;

        // Act
        ErrorOr<Success> result = taxon.UpdateChildIndex(-1);

        // Assert
        result.IsError.ShouldBeTrue();
    }

    #endregion

    #region Computed Properties Tests

    [Fact]
    public void IsRoot_WithNoParent_ShouldBeTrue()
    {
        // Arrange
        var taxon = Taxon.Create("Root", Guid.NewGuid()).Value;

        // Assert
        taxon.IsRoot.ShouldBeTrue();
    }

    [Fact]
    public void IsRoot_WithParent_ShouldBeFalse()
    {
        // Arrange
        Guid taxonomyId = Guid.NewGuid();
        var parent = Taxon.Create("Parent", taxonomyId).Value;
        var child = Taxon.Create("Child", taxonomyId).Value;
        child.SetParent(parent);

        // Assert
        child.IsRoot.ShouldBeFalse();
    }

    [Fact]
    public void IsManual_WhenAutomaticIsFalse_ShouldBeTrue()
    {
        // Arrange
        var taxon = Taxon.Create("Manual", Guid.NewGuid()).Value;

        // Assert
        taxon.IsManual.ShouldBeTrue();
    }

    [Fact]
    public void IsManual_WhenAutomaticIsTrue_ShouldBeFalse()
    {
        // Arrange
        var taxon = Taxon.Create("Automatic", Guid.NewGuid(), automatic: true).Value;

        // Assert
        taxon.IsManual.ShouldBeFalse();
    }

    [Fact]
    public void PageBuilderImageUrl_WithSquareImage_ShouldReturnSquareImage()
    {
        // Arrange
        var taxon = Taxon.Create(
            "Products",
            Guid.NewGuid(),
            imageUrl: "https://example.com/image.jpg",
            squareImageUrl: "https://example.com/square.jpg").Value;

        // Assert
        taxon.PageBuilderImageUrl.ShouldBe("https://example.com/square.jpg");
    }

    [Fact]
    public void PageBuilderImageUrl_WithoutSquareImage_ShouldReturnImageUrl()
    {
        // Arrange
        var taxon = Taxon.Create(
            "Products",
            Guid.NewGuid(),
            imageUrl: "https://example.com/image.jpg").Value;

        // Assert
        taxon.PageBuilderImageUrl.ShouldBe("https://example.com/image.jpg");
    }

    [Fact]
    public void SeoTitle_WithMetaTitle_ShouldReturnMetaTitle()
    {
        // Arrange
        var taxon = Taxon.Create("Products", Guid.NewGuid(), metaTitle: "Buy Products").Value;

        // Assert
        taxon.SeoTitle.ShouldBe("Buy Products");
    }

    [Fact]
    public void SeoTitle_WithoutMetaTitle_ShouldReturnName()
    {
        // Arrange
        var taxon = Taxon.Create("Products", Guid.NewGuid()).Value;

        // Assert
        taxon.SeoTitle.ShouldBe("Products");
    }

    #endregion

    #region Cache Tests

    [Fact]
    public void CachedSelfAndDescendantsIds_ShouldIncludeSelfAndChildren()
    {
        // Arrange
        Guid taxonomyId = Guid.NewGuid();
        var parent = Taxon.Create("Parent", taxonomyId).Value;
        var child1 = Taxon.Create("Child1", taxonomyId).Value;
        var child2 = Taxon.Create("Child2", taxonomyId).Value;

        child1.SetParent(parent);
        child2.SetParent(parent);

        // Act
        var ids = parent.CachedSelfAndDescendantsIds;

        // Assert
        ids.Count.ShouldBe(3);
        ids.ShouldContain(parent.Id);
        ids.ShouldContain(child1.Id);
        ids.ShouldContain(child2.Id);
    }

    [Fact]
    public void InvalidateDescendantsCache_ShouldClearCache()
    {
        // Arrange
        Guid taxonomyId = Guid.NewGuid();
        var parent = Taxon.Create("Parent", taxonomyId).Value;
        var child = Taxon.Create("Child", taxonomyId).Value;
        child.SetParent(parent);

        // Access cache to populate it
        var cachedBefore = parent.CachedSelfAndDescendantsIds;
        cachedBefore.Count.ShouldBe(2);

        // Act
        parent.InvalidateDescendantsCache();

        // Add another child
        var child2 = Taxon.Create("Child2", taxonomyId).Value;
        child2.SetParent(parent);

        // Assert - should rebuild cache with new child
        var cachedAfter = parent.CachedSelfAndDescendantsIds;
        cachedAfter.Count.ShouldBe(3);
    }

    #endregion
}