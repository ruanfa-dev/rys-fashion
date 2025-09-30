using Core.Catalog.Products;
using Core.Catalog.Taxonomies;

using ErrorOr;

using Test.Common;

using Shouldly;

namespace Core.UnitTests.Taxonomies;

public sealed class TaxonAdditionalTests
{
    static TaxonAdditionalTests() => TaxonTestHelper.WireResolver();

    [Fact]
    public void Update_ShouldChangeNameAndRegeneratePrettyNameAndPermalink_WhenParentIsSet()
    {
        Guid taxonomyId = Guid.NewGuid();
        ErrorOr<Taxon> parentRes = Taxon.Create("Root", taxonomyId);
        parentRes.IsError.ShouldBeFalse();
        Taxon parent = parentRes.Value;

        TaxonTestHelper.Register(parent);

        ErrorOr<Taxon> childRes = Taxon.Create("Child", taxonomyId, parentId: parent.Id);
        childRes.IsError.ShouldBeFalse();
        Taxon child = childRes.Value;

        // wire navigation like some handlers/tests do
        parent.Children.Add(child);
        child.Parent = parent;

        ErrorOr<Taxon> updateRes = child.Update(name: "ChildNew");
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
        Guid taxonomyId = Guid.NewGuid();
        Taxon root = Taxon.Create("Root", taxonomyId).Value;
        Taxon child = Taxon.Create("Child", taxonomyId, parentId: root.Id).Value;
        Taxon grand = Taxon.Create("Grand", taxonomyId, parentId: child.Id).Value;

        // wire navigation
        root.Children.Add(child);
        child.Parent = root;
        child.Children.Add(grand);
        grand.Parent = child;

        List<Taxon> ancestors = grand.GetAncestors().ToList();
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
        Guid taxonomyId = Guid.NewGuid();
        Taxon root = Taxon.Create("Root", taxonomyId).Value;
        Taxon child = Taxon.Create("Child", taxonomyId, parentId: root.Id).Value;
        Taxon grand = Taxon.Create("Grand", taxonomyId, parentId: child.Id).Value;

        // wire navigation
        root.Children.Add(child);
        child.Parent = root;
        child.Children.Add(grand);
        grand.Parent = child;

        List<Taxon> all = root.GetAllDescendants().ToList();
        all.Count.ShouldBe(2);
        all.ShouldContain(child);
        all.ShouldContain(grand);
    }

    [Fact]
    public void Classify_Unclassify_ProductLifecycleAndDuplicatePrevention()
    {
        Guid taxonomyId = Guid.NewGuid();
        Taxon taxon = Taxon.Create("T", taxonomyId).Value;

        Product p1 = new Product();
        p1.Id = Guid.NewGuid();

        ErrorOr<Taxon> classify = taxon.ClassifyProduct(p1);
        classify.IsError.ShouldBeFalse();
        taxon.Classifications.Count.ShouldBe(1);

        // duplicate classification is prevented
        ErrorOr<Taxon> classify2 = taxon.ClassifyProduct(p1);
        classify2.IsError.ShouldBeTrue();
        classify2.FirstError.Code.ShouldContain("ProductAlreadyClassified");

        // unclassify succeeds
        ErrorOr<Taxon> un = taxon.UnclassifyProduct(p1.Id);
        un.IsError.ShouldBeFalse();
        taxon.Classifications.Count.ShouldBe(0);

        // unclassify again fails
        ErrorOr<Taxon> un2 = taxon.UnclassifyProduct(p1.Id);
        un2.IsError.ShouldBeTrue();
        un2.FirstError.Code.ShouldContain("ProductNotClassified");
    }

    [Fact]
    public void GetActiveProductsWithDescendants_ExcludesInactiveProducts()
    {
        Guid taxonomyId = Guid.NewGuid();
        Taxon root = Taxon.Create("Root", taxonomyId).Value;
        Taxon child = Taxon.Create("Child", taxonomyId, parentId: root.Id).Value;

        // wire navigation
        root.Children.Add(child);
        child.Parent = root;

        Product active = new Product { Id = Guid.NewGuid(), IsActive = true };
        Product inactive = new Product { Id = Guid.NewGuid(), IsActive = false };

        // classify active on root, inactive on child
        root.Classifications.Add(new Classification { Id = Guid.NewGuid(), TaxonId = root.Id, ProductId = active.Id, Product = active });
        child.Classifications.Add(new Classification { Id = Guid.NewGuid(), TaxonId = child.Id, ProductId = inactive.Id, Product = inactive });

        List<Product> all = root.GetActiveProductsWithDescendants().ToList();
        all.Count.ShouldBe(1);
        all.ShouldContain(active);
        all.ShouldNotContain(inactive);
    }

    [Fact]
    public void SetParent_ShouldFail_WhenCircularReferenceWouldBeCreated()
    {
        Guid taxonomyId = Guid.NewGuid();
        Taxon root = Taxon.Create("Root", taxonomyId).Value;
        Taxon child = Taxon.Create("Child", taxonomyId).Value;
        Taxon grand = Taxon.Create("Grand", taxonomyId).Value;

        // wire navigation: child -> root, grand -> child
        child.SetParent(root).IsError.ShouldBeFalse();
        grand.SetParent(child).IsError.ShouldBeFalse();

        // Attempt to set root's parent to grandchild -> should fail circular
        ErrorOr<Taxon> res = root.SetParent(grand);
        res.IsError.ShouldBeTrue();
        res.FirstError.Code.ShouldContain("CircularReference");
    }
}
