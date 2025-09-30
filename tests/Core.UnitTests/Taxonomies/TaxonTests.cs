using Core.Catalog.Products;
using Core.Catalog.Taxonomies;

using ErrorOr;

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
        Guid taxonomyId = Guid.NewGuid();

        // Act
        ErrorOr<Taxon> res = Taxon.Create("Shirts", taxonomyId);

        // Assert
        res.IsError.ShouldBeFalse();
        Taxon taxon = res.Value;
        taxon.Name.ShouldBe("Shirts");
        taxon.PrettyName.ShouldBe("Shirts");
        taxon.Permalink.ShouldBe("shirts");
    }

    [Fact]
    public void GenerateSlug_IncludesParentPermalink_WhenParentPresent()
    {
        Guid taxonomyId = Guid.NewGuid();
    ErrorOr<Taxon> parentRes = Taxon.Create("Clothing", taxonomyId);
        parentRes.IsError.ShouldBeFalse();
        Taxon parent = parentRes.Value;

    TaxonTestHelper.Register(parent);

    ErrorOr<Taxon> childRes = Taxon.Create("T-Shirts", taxonomyId, parentId: parent.Id);
        childRes.IsError.ShouldBeFalse();
        Taxon child = childRes.Value;

    TaxonTestHelper.Register(child);

        // when parent exists and has a permalink, child slug should include parent permalink
        child.Permalink.ShouldBe("t-shirts");
        child.SetPermalink();
        // ensure child's slug is joined with parent's
        string expected = string.Join('/', new[] { parent.Permalink.TrimEnd('/'), "t-shirts" });
        child.GenerateSlug().ShouldBe(expected);
    }

    [Fact]
    public void SetParent_Succeeds_WhenSameTaxonomy()
    {
        Guid taxonomyId = Guid.NewGuid();
        ErrorOr<Taxon> parentRes = Taxon.Create("Parent", taxonomyId);
        parentRes.IsError.ShouldBeFalse();
        Taxon parent = parentRes.Value;

        ErrorOr<Taxon> childRes = Taxon.Create("Child", taxonomyId);
        childRes.IsError.ShouldBeFalse();
        Taxon child = childRes.Value;

        ErrorOr<Taxon> setResult = child.SetParent(parent);
        setResult.IsError.ShouldBeFalse();
        child.ParentId.ShouldBe(parent.Id);
        child.Parent.ShouldBe(parent);
    }

    [Fact]
    public void RegeneratePrettyNameAndPermalink_PropagatesToChildren()
    {
        Guid taxonomyId = Guid.NewGuid();
        ErrorOr<Taxon> rootRes = Taxon.Create("Root", taxonomyId);
        rootRes.IsError.ShouldBeFalse();
        Taxon root = rootRes.Value;

        ErrorOr<Taxon> childRes = Taxon.Create("Child", taxonomyId, parentId: root.Id);
        childRes.IsError.ShouldBeFalse();
        Taxon child = childRes.Value;
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
        Guid storeId = Guid.NewGuid();
        ErrorOr<Taxonomy> res = Taxonomy.Create("T", storeId);
        res.IsError.ShouldBeFalse();
        Taxonomy taxonomy = res.Value;

        // Attempt to create another root under same taxonomy
        ErrorOr<Taxon> newRootRes = Taxon.Create("AnotherRoot", taxonomy.Id);
        newRootRes.IsError.ShouldBeFalse();
        Taxon newRoot = newRootRes.Value;

        ErrorOr<Success> val = newRoot.ValidateForCreateAgainst(taxonomy);
        val.IsError.ShouldBeTrue();
        val.FirstError.Code.ShouldContain("RootConflict");
    }


    [Fact]
    public void SetParent_ShouldFail_WhenParentBelongsToDifferentTaxonomy()
    {
        // Arrange
        Guid taxonomyA = Guid.NewGuid();
        Guid taxonomyB = Guid.NewGuid();

        ErrorOr<Taxon> parentRes = Taxon.Create("Parent", taxonomyA);
        parentRes.IsError.ShouldBeFalse();
        Taxon parent = parentRes.Value;

        ErrorOr<Taxon> childRes = Taxon.Create("Child", taxonomyB);
        childRes.IsError.ShouldBeFalse();
        Taxon child = childRes.Value;

        // Act
        ErrorOr<Taxon> setResult = child.SetParent(parent);

        // Assert
        setResult.IsError.ShouldBeTrue();
        setResult.FirstError.Code.ShouldContain("ParentTaxonomyMismatch");
    }

    [Fact]
    public void Delete_ShouldPrevent_WhenHasChildrenOrClassifications()
    {
        // Arrange
        Guid taxonomyId = Guid.NewGuid();
        ErrorOr<Taxon> parentRes = Taxon.Create("Parent", taxonomyId);
        parentRes.IsError.ShouldBeFalse();
        Taxon parent = parentRes.Value;

        ErrorOr<Taxon> childRes = Taxon.Create("Child", taxonomyId, parentId: parent.Id);
        childRes.IsError.ShouldBeFalse();
        Taxon child = childRes.Value;

        parent.Children.Add(child);

        // Act
        ErrorOr<Deleted> deleteRes = parent.Delete();

        // Assert
        deleteRes.IsError.ShouldBeTrue();
        deleteRes.FirstError.Code.ShouldContain("HasChildren");

        // Now remove child and add a classification to simulate classifications
        parent.Children.Remove(child);
        parent.Classifications.Add(new Classification { Id = Guid.NewGuid(), TaxonId = parent.Id });

        ErrorOr<Deleted> deleteRes2 = parent.Delete();
        deleteRes2.IsError.ShouldBeTrue();
        deleteRes2.FirstError.Code.ShouldContain("HasClassifications");
    }
}
