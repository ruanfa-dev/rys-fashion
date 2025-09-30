using Core.Catalog.Products;
using Core.Catalog.Taxonomies;

using Shouldly;

namespace Core.UnitTests.Taxonomies;

public sealed class TaxonomyTests
{
    [Fact]
    public void Create_ShouldCreateRootTaxon()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var name = "Men";

        // Act
        var res = Taxonomy.Create(name, storeId);

        // Assert
        res.IsError.ShouldBeFalse();
        var taxonomy = res.Value;
        taxonomy.Root.ShouldNotBeNull();
        taxonomy.Root!.Name.ShouldBe(name);
        taxonomy.Taxons.Count.ShouldBe(1);
    }

    [Fact]
    public void Update_ShouldSyncRootName_WhenRootExists()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var res = Taxonomy.Create("Women", storeId);
        res.IsError.ShouldBeFalse();
        var taxonomy = res.Value;
        var root = taxonomy.Root;
        root.ShouldNotBeNull();

        // Act
        var updateRes = taxonomy.Update("Women & Kids");

        // Assert
        updateRes.IsError.ShouldBeFalse();
        taxonomy.Name.ShouldBe("Women & Kids");
        taxonomy.Root!.Name.ShouldBe("Women & Kids");
    }

    [Fact]
    public void Delete_ShouldPrevent_WhenTaxonsHaveChildrenOrClassifications()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var res = Taxonomy.Create("Accessories", storeId);
        res.IsError.ShouldBeFalse();
        var taxonomy = res.Value;
        var root = taxonomy.Root!;

        // Add a child taxon to simulate dependent taxons
        var childRes = Taxon.Create("Belts", taxonomy.Id, parentId: root.Id);
        childRes.IsError.ShouldBeFalse();
        var child = childRes.Value;
        taxonomy.Taxons.Add(child);

        // Act
        var deleteRes = taxonomy.Delete();

        // Assert
        deleteRes.IsError.ShouldBeTrue();
        deleteRes.FirstError.Code.ShouldContain("HasDependentTaxons");

        // Now remove child and add a classification to root to simulate classifications
        taxonomy.Taxons.Remove(child);
        root.Classifications.Add(new Classification { Id = Guid.NewGuid(), TaxonId = root.Id });

        var deleteRes2 = taxonomy.Delete();
        deleteRes2.IsError.ShouldBeTrue();
        deleteRes2.FirstError.Code.ShouldContain("HasClassifications");
    }
}
