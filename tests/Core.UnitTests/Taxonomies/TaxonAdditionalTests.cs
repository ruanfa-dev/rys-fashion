using Core.Catalog.Products;
using Core.Catalog.Taxonomies;
using Test.Common;

using Shouldly;

namespace Core.UnitTests.Taxonomies;

public sealed class TaxonAdditionalTests
{
    static TaxonAdditionalTests() => TaxonTestHelper.WireResolver();

    [Fact]
    public void Update_ShouldChangeNameAndRegeneratePrettyNameAndPermalink_WhenParentIsSet()
    {
        var taxonomyId = Guid.NewGuid();
        var parentRes = Taxon.Create("Root", taxonomyId);
        parentRes.IsError.ShouldBeFalse();
        var parent = parentRes.Value;

        TaxonTestHelper.Register(parent);

        var childRes = Taxon.Create("Child", taxonomyId, parentId: parent.Id);
        childRes.IsError.ShouldBeFalse();
        var child = childRes.Value;

        // wire navigation like some handlers/tests do
        parent.Children.Add(child);
        child.Parent = parent;

        var updateRes = child.Update(name: "ChildNew");
        updateRes.IsError.ShouldBeFalse();

        child.Name.ShouldBe("ChildNew");
        child.PrettyName.ShouldBe("Root -> ChildNew");
        // permalink should include parent prefix
        child.Permalink.ShouldContain(parent.Permalink);
        child.Permalink.ShouldContain("childnew");
    }

    [Fact]
    public void Ancestors_Level_IsAncestor_IsDescendant_BehaveAsExpected()
    {
        var taxonomyId = Guid.NewGuid();
        var root = Taxon.Create("Root", taxonomyId).Value;
        var child = Taxon.Create("Child", taxonomyId, parentId: root.Id).Value;
        var grand = Taxon.Create("Grand", taxonomyId, parentId: child.Id).Value;

        // wire navigation
        root.Children.Add(child);
        child.Parent = root;
        child.Children.Add(grand);
        grand.Parent = child;

        var ancestors = grand.GetAncestors().ToList();
        ancestors.Count.ShouldBe(2);
        ancestors[0].Id.ShouldBe(root.Id);
        ancestors[1].Id.ShouldBe(child.Id);

        grand.GetLevel().ShouldBe(2);
        root.IsAncestorOf(grand).ShouldBeTrue();
        grand.IsDescendantOf(root).ShouldBeTrue();
    }

    [Fact]
    public void GetAllDescendants_ReturnsFullSubtree()
    {
        var taxonomyId = Guid.NewGuid();
        var root = Taxon.Create("Root", taxonomyId).Value;
        var child = Taxon.Create("Child", taxonomyId, parentId: root.Id).Value;
        var grand = Taxon.Create("Grand", taxonomyId, parentId: child.Id).Value;

        // wire navigation
        root.Children.Add(child);
        child.Parent = root;
        child.Children.Add(grand);
        grand.Parent = child;

        var all = root.GetAllDescendants().ToList();
        all.Count.ShouldBe(2);
        all.ShouldContain(child);
        all.ShouldContain(grand);
    }

    [Fact]
    public void Classify_Unclassify_ProductLifecycleAndDuplicatePrevention()
    {
        var taxonomyId = Guid.NewGuid();
        var taxon = Taxon.Create("T", taxonomyId).Value;

        var p1 = new Product();
        p1.Id = Guid.NewGuid();

        var classify = taxon.ClassifyProduct(p1);
        classify.IsError.ShouldBeFalse();
        taxon.Classifications.Count.ShouldBe(1);

        // duplicate classification is prevented
        var classify2 = taxon.ClassifyProduct(p1);
        classify2.IsError.ShouldBeTrue();
        classify2.FirstError.Code.ShouldContain("ProductAlreadyClassified");

        // unclassify succeeds
        var un = taxon.UnclassifyProduct(p1.Id);
        un.IsError.ShouldBeFalse();
        taxon.Classifications.Count.ShouldBe(0);

        // unclassify again fails
        var un2 = taxon.UnclassifyProduct(p1.Id);
        un2.IsError.ShouldBeTrue();
        un2.FirstError.Code.ShouldContain("ProductNotClassified");
    }

    [Fact]
    public void GetActiveProductsWithDescendants_ExcludesInactiveProducts()
    {
        var taxonomyId = Guid.NewGuid();
        var root = Taxon.Create("Root", taxonomyId).Value;
        var child = Taxon.Create("Child", taxonomyId, parentId: root.Id).Value;

        // wire navigation
        root.Children.Add(child);
        child.Parent = root;

        var active = new Product { Id = Guid.NewGuid(), IsActive = true };
        var inactive = new Product { Id = Guid.NewGuid(), IsActive = false };

        // classify active on root, inactive on child
        root.Classifications.Add(new Classification { Id = Guid.NewGuid(), TaxonId = root.Id, ProductId = active.Id, Product = active });
        child.Classifications.Add(new Classification { Id = Guid.NewGuid(), TaxonId = child.Id, ProductId = inactive.Id, Product = inactive });

        var all = root.GetActiveProductsWithDescendants().ToList();
        all.Count.ShouldBe(1);
        all.ShouldContain(active);
        all.ShouldNotContain(inactive);
    }

    [Fact]
    public void SetParent_ShouldFail_WhenCircularReferenceWouldBeCreated()
    {
        var taxonomyId = Guid.NewGuid();
        var root = Taxon.Create("Root", taxonomyId).Value;
        var child = Taxon.Create("Child", taxonomyId).Value;
        var grand = Taxon.Create("Grand", taxonomyId).Value;

        // wire navigation: child -> root, grand -> child
        child.SetParent(root).IsError.ShouldBeFalse();
        grand.SetParent(child).IsError.ShouldBeFalse();

        // Attempt to set root's parent to grandchild -> should fail circular
        var res = root.SetParent(grand);
        res.IsError.ShouldBeTrue();
        res.FirstError.Code.ShouldContain("CircularReference");
    }
}
