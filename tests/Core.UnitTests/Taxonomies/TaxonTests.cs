using Core.Catalog.Products;
using Core.Catalog.Taxonomies;
using Test.Common;

using Shouldly;

namespace Core.UnitTests.Taxonomies;

public sealed class TaxonTests
{
    static TaxonTests() => TaxonTestHelper.WireResolver();

    [Fact]
    public void Create_ShouldGeneratePrettyNameAndPermalink()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();

        // Act
        var res = Taxon.Create("Shirts", taxonomyId);

        // Assert
        res.IsError.ShouldBeFalse();
        var taxon = res.Value;
        taxon.Name.ShouldBe("Shirts");
        taxon.PrettyName.ShouldBe("Shirts");
        taxon.Permalink.ShouldBe("shirts");
    }

    [Fact]
    public void GenerateSlug_IncludesParentPermalink_WhenParentPresent()
    {
        var taxonomyId = Guid.NewGuid();
    var parentRes = Taxon.Create("Clothing", taxonomyId);
        parentRes.IsError.ShouldBeFalse();
        var parent = parentRes.Value;

    TaxonTestHelper.Register(parent);

    var childRes = Taxon.Create("T-Shirts", taxonomyId, parentId: parent.Id);
        childRes.IsError.ShouldBeFalse();
        var child = childRes.Value;

    TaxonTestHelper.Register(child);

        // when parent exists and has a permalink, child slug should include parent permalink
        child.Permalink.ShouldBe("t-shirts");
        child.SetPermalink();
        // ensure child's slug is joined with parent's
        var expected = string.Join('/', new[] { parent.Permalink.TrimEnd('/'), "t-shirts" });
        child.GenerateSlug().ShouldBe(expected);
    }

    [Fact]
    public void SetParent_Succeeds_WhenSameTaxonomy()
    {
        var taxonomyId = Guid.NewGuid();
        var parentRes = Taxon.Create("Parent", taxonomyId);
        parentRes.IsError.ShouldBeFalse();
        var parent = parentRes.Value;

        var childRes = Taxon.Create("Child", taxonomyId);
        childRes.IsError.ShouldBeFalse();
        var child = childRes.Value;

        var setResult = child.SetParent(parent);
        setResult.IsError.ShouldBeFalse();
        child.ParentId.ShouldBe(parent.Id);
        child.Parent.ShouldBe(parent);
    }

    [Fact]
    public void RegeneratePrettyNameAndPermalink_PropagatesToChildren()
    {
        var taxonomyId = Guid.NewGuid();
        var rootRes = Taxon.Create("Root", taxonomyId);
        rootRes.IsError.ShouldBeFalse();
        var root = rootRes.Value;

        var childRes = Taxon.Create("Child", taxonomyId, parentId: root.Id);
        childRes.IsError.ShouldBeFalse();
        var child = childRes.Value;
        root.Children.Add(child);

        // mutate root name and regenerate
        root.Name = "RootNew";
        root.RegeneratePrettyNameAndPermalink();

        root.PrettyName.ShouldBe("RootNew");
        child.PrettyName.ShouldBe("RootNew -> Child");
    }

    [Fact]
    public void ValidateForCreateAgainst_Fails_WhenRootAlreadyExists()
    {
        var storeId = Guid.NewGuid();
        var res = Taxonomy.Create("T", storeId);
        res.IsError.ShouldBeFalse();
        var taxonomy = res.Value;

        // Attempt to create another root under same taxonomy
        var newRootRes = Taxon.Create("AnotherRoot", taxonomy.Id);
        newRootRes.IsError.ShouldBeFalse();
        var newRoot = newRootRes.Value;

        var val = newRoot.ValidateForCreateAgainst(taxonomy);
        val.IsError.ShouldBeTrue();
        val.FirstError.Code.ShouldContain("RootConflict");
    }


    [Fact]
    public void SetParent_ShouldFail_WhenParentBelongsToDifferentTaxonomy()
    {
        // Arrange
        var taxonomyA = Guid.NewGuid();
        var taxonomyB = Guid.NewGuid();

        var parentRes = Taxon.Create("Parent", taxonomyA);
        parentRes.IsError.ShouldBeFalse();
        var parent = parentRes.Value;

        var childRes = Taxon.Create("Child", taxonomyB);
        childRes.IsError.ShouldBeFalse();
        var child = childRes.Value;

        // Act
        var setResult = child.SetParent(parent);

        // Assert
        setResult.IsError.ShouldBeTrue();
        setResult.FirstError.Code.ShouldContain("ParentTaxonomyMismatch");
    }

    [Fact]
    public void Delete_ShouldPrevent_WhenHasChildrenOrClassifications()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var parentRes = Taxon.Create("Parent", taxonomyId);
        parentRes.IsError.ShouldBeFalse();
        var parent = parentRes.Value;

        var childRes = Taxon.Create("Child", taxonomyId, parentId: parent.Id);
        childRes.IsError.ShouldBeFalse();
        var child = childRes.Value;

        parent.Children.Add(child);

        // Act
        var deleteRes = parent.Delete();

        // Assert
        deleteRes.IsError.ShouldBeTrue();
        deleteRes.FirstError.Code.ShouldContain("HasChildren");

        // Now remove child and add a classification to simulate classifications
        parent.Children.Remove(child);
        parent.Classifications.Add(new Classification { Id = Guid.NewGuid(), TaxonId = parent.Id });

        var deleteRes2 = parent.Delete();
        deleteRes2.IsError.ShouldBeTrue();
        deleteRes2.FirstError.Code.ShouldContain("HasClassifications");
    }
}
