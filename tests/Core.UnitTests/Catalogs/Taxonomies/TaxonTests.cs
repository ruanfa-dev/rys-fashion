using System.Reflection;

using Core.Catalog.Taxonomies;

using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

using Shouldly;

namespace Core.UnitTests.Catalogs.Taxonomies;

public class TaxonTests
{
    private readonly Guid _validTaxonomyId = Guid.NewGuid();
    private readonly string _validName = "Electronics";

    [Fact]
    public void Create_WithValidParameters_ShouldReturnSuccess()
    {
        // Act
        ErrorOr<Taxon> result = Taxon.Create(
            name: _validName,
            taxonomyId: _validTaxonomyId
        );

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Name.ShouldBe(_validName);
        result.Value.TaxonomyId.ShouldBe(_validTaxonomyId);
        result.Value.IsRoot.ShouldBeTrue();
        result.Value.IsManual.ShouldBeTrue();
        result.Value.Automatic.ShouldBeFalse();
        result.Value.RulesMatchPolicy.ShouldBe("all");
        result.Value.SortOrder.ShouldBe("manual");
        result.Value.HideFromNav.ShouldBeFalse();
    }

    [Fact]
    public void Create_WithAllParameters_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        Guid parentId = Guid.NewGuid();
        string description = "Electronic devices and gadgets";
        string metaTitle = "Electronics SEO Title";
        Dictionary<string, string?> publicMetadata = new Dictionary<string, string?> { ["key1"] = "value1" };

        // Act
        ErrorOr<Taxon> result = Taxon.Create(
            name: _validName,
            taxonomyId: _validTaxonomyId,
            parentId: parentId,
            automatic: true,
            rulesMatchPolicy: "any",
            sortOrder: "name-a-z",
            hideFromNav: true,
            description: description,
            metaTitle: metaTitle,
            publicMetadata: publicMetadata
        );

        // Assert
        result.IsError.ShouldBeFalse();
        Taxon taxon = result.Value;

        taxon.Name.ShouldBe(_validName);
        taxon.TaxonomyId.ShouldBe(_validTaxonomyId);
        taxon.ParentId.ShouldBe(parentId);
        taxon.Automatic.ShouldBeTrue();
        taxon.RulesMatchPolicy.ShouldBe("any");
        taxon.SortOrder.ShouldBe("name-a-z");
        taxon.HideFromNav.ShouldBeTrue();
        taxon.Description.ShouldBe(description);
        taxon.MetaTitle.ShouldBe(metaTitle);
        taxon.PublicMetadata?.ShouldContainKeyAndValue("key1", "value1");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidName_ShouldReturnError(string? invalidName)
    {
        // Act
        ErrorOr<Taxon> result = Taxon.Create(
            name: invalidName?? string.Empty,
            taxonomyId: _validTaxonomyId
        );

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Taxon.NameRequired");
    }

    [Fact]
    public void Create_WithEmptyTaxonomyId_ShouldReturnError()
    {
        // Act
        ErrorOr<Taxon> result = Taxon.Create(
            name: _validName,
            taxonomyId: Guid.Empty
        );

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Taxon.TaxonomyRequired");
    }

    [Fact]
    public void Create_WithTooLongName_ShouldReturnError()
    {
        // Arrange
        string tooLongName = new string('A', Taxon.Constraints.NameMaxLength + 1);

        // Act
        ErrorOr<Taxon> result = Taxon.Create(
            name: tooLongName,
            taxonomyId: _validTaxonomyId
        );

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Taxon.InvalidNameLength");
    }

    [Theory]
    [InlineData("invalid_policy")]
    [InlineData("")]
    public void Create_WithInvalidRulesMatchPolicy_ShouldReturnError(string invalidPolicy)
    {
        // Act
        ErrorOr<Taxon> result = Taxon.Create(
            name: _validName,
            taxonomyId: _validTaxonomyId,
            rulesMatchPolicy: invalidPolicy
        );

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Taxon.InvalidRulesMatchPolicy");
    }

    [Theory]
    [InlineData("invalid_sort")]
    [InlineData("")]
    public void Create_WithInvalidSortOrder_ShouldReturnError(string invalidSort)
    {
        // Act
        ErrorOr<Taxon> result = Taxon.Create(
            name: _validName,
            taxonomyId: _validTaxonomyId,
            sortOrder: invalidSort
        );

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Taxon.InvalidSortOrder");
    }

    [Fact]
    public void Create_WithTooLongDescription_ShouldReturnError()
    {
        // Arrange
        string tooLongDescription = new string('A', Taxon.Constraints.DescriptionMaxLength + 1);

        // Act
        ErrorOr<Taxon> result = Taxon.Create(
            name: _validName,
            taxonomyId: _validTaxonomyId,
            description: tooLongDescription
        );

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Taxon.DescriptionTooLong");
    }

    [Fact]
    public void Create_ShouldGenerateDomainEvent()
    {
        // Act
        ErrorOr<Taxon> result = Taxon.Create(_validName, _validTaxonomyId);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.GetDomainEvents().ShouldHaveSingleItem();
        result.Value.GetDomainEvents().First().ShouldBeOfType<Taxon.Events.Created>();

        Taxon.Events.Created createdEvent = (Taxon.Events.Created)result.Value.GetDomainEvents().First();
        createdEvent.TaxonId.ShouldBe(result.Value.Id);
    }
}

public class TaxonUpdateTests
{
    private readonly Taxon _taxon;

    public TaxonUpdateTests()
    {
        ErrorOr<Taxon> result = Taxon.Create("Electronics", Guid.NewGuid());
        _taxon = result.Value;
        _taxon.ClearDomainEvents(); // Clear creation event for cleaner tests
    }

    [Fact]
    public void Update_WithValidName_ShouldUpdateSuccessfully()
    {
        // Arrange
        const string newName = "Updated Electronics";

        // Act
        ErrorOr<Taxon> result = _taxon.Update(name: newName);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Name.ShouldBe(newName);
        result.Value.GetDomainEvents().ShouldHaveSingleItem();
        result.Value.GetDomainEvents().First().ShouldBeOfType<Taxon.Events.Updated>();
    }

    [Fact]
    public void Update_WithSameName_ShouldNotTriggerUpdate()
    {
        // Act
        ErrorOr<Taxon> result = _taxon.Update(name: _taxon.Name);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.GetDomainEvents().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Update_WithInvalidName_ShouldReturnError(string? invalidName)
    {
        // Act
        ErrorOr<Taxon> result = _taxon.Update(name: invalidName);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Taxon.NameRequired");
    }

    [Fact]
    public void Update_WithSelfAsParent_ShouldReturnError()
    {
        // Act
        ErrorOr<Taxon> result = _taxon.Update(parentId: _taxon.Id);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Taxon.SelfParent");
    }

    [Fact]
    public void Update_RulesPolicyChangeOnAutomaticTaxon_ShouldTriggerRegeneration()
    {
        // Arrange
        Taxon automaticTaxon = Taxon.Create("Auto", Guid.NewGuid(), automatic: true).Value;
        automaticTaxon.ClearDomainEvents();

        // Act
        ErrorOr<Taxon> result = automaticTaxon.Update(rulesMatchPolicy: "any");

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.GetDomainEvents().ShouldContain(e => e is Taxon.Events.RegenerateProducts);
    }

    [Fact]
    public void Update_MultipleProperties_ShouldUpdateAll()
    {
        // Arrange
        const string newName = "New Name";
        const string newDescription = "New Description";
        const bool newAutomatic = true;

        // Act
        ErrorOr<Taxon> result = _taxon.Update(
            name: newName,
            description: newDescription,
            automatic: newAutomatic
        );

        // Assert
        result.IsError.ShouldBeFalse();
        Taxon updated = result.Value;
        updated.Name.ShouldBe(newName);
        updated.Description.ShouldBe(newDescription);
        updated.Automatic.ShouldBe(newAutomatic);
    }
}

public class TaxonHierarchyTests
{
    private readonly Taxon _parentTaxon;
    private readonly Taxon _childTaxon;
    private readonly Guid _taxonomyId;

    public TaxonHierarchyTests()
    {
        _taxonomyId = Guid.NewGuid();
        _parentTaxon = Taxon.Create("Parent", _taxonomyId).Value;
        _childTaxon = Taxon.Create("Child", _taxonomyId, _parentTaxon.Id).Value;
    }

    [Fact]
    public void SetParent_WithValidParent_ShouldSucceed()
    {
        // Arrange
        Taxon newParent = Taxon.Create("NewParent", _taxonomyId).Value;
        Taxon child = Taxon.Create("TestChild", _taxonomyId).Value;

        // Act
        ErrorOr<Taxon> result = child.SetParent(newParent);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ParentId.ShouldBe(newParent.Id);
        result.Value.Parent.ShouldBe(newParent);
        newParent.Children.ShouldContain(child);
    }

    [Fact]
    public void SetParent_WithSelfAsParent_ShouldReturnError()
    {
        // Act
        ErrorOr<Taxon> result = _parentTaxon.SetParent(_parentTaxon);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Taxon.SelfParent");
    }

    [Fact]
    public void SetParent_WithDifferentTaxonomy_ShouldReturnError()
    {
        // Arrange
        Taxon differentTaxonomyParent = Taxon.Create("DifferentParent", Guid.NewGuid()).Value;

        // Act
        ErrorOr<Taxon> result = _childTaxon.SetParent(differentTaxonomyParent);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Taxon.ParentTaxonomyMismatch");
    }

    [Fact]
    public void SetParent_WithCircularReference_ShouldReturnError()
    {
        // Arrange - Create a grandchild
        Taxon grandChild = Taxon.Create("GrandChild", _taxonomyId, _childTaxon.Id).Value;

        // Simulate hierarchy by setting up the ancestor relationship
        // In a real scenario, this would be handled by the infrastructure
        grandChild.SetParent(_childTaxon);
        _childTaxon.SetParent(_parentTaxon);

        // Act - Try to make parent a child of grandchild (circular reference)
        ErrorOr<Taxon> result = _parentTaxon.SetParent(grandChild);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Taxon.CircularReference");
    }

    [Fact]
    public void IsRoot_WhenNoParent_ShouldReturnTrue()
    {
        // Assert
        _parentTaxon.IsRoot.ShouldBeTrue();
        _childTaxon.IsRoot.ShouldBeFalse();
    }

    [Fact]
    public void IsAncestorOf_WhenIsParent_ShouldReturnTrue()
    {
        // Arrange
        _childTaxon.SetParent(_parentTaxon);

        // Act & Assert
        _parentTaxon.IsAncestorOf(_childTaxon).ShouldBeTrue();
        _childTaxon.IsAncestorOf(_parentTaxon).ShouldBeFalse();
    }

    [Fact]
    public void IsDescendantOf_WhenIsChild_ShouldReturnTrue()
    {
        // Arrange
        _childTaxon.SetParent(_parentTaxon);

        // Act & Assert
        _childTaxon.IsDescendantOf(_parentTaxon).ShouldBeTrue();
        _parentTaxon.IsDescendantOf(_childTaxon).ShouldBeFalse();
    }
}

public class TaxonBusinessRulesTests
{
    private readonly Taxon _taxon = Taxon.Create("Electronics", Guid.NewGuid(), automatic: true).Value;

    [Fact]
    public void AddRule_ToAutomaticTaxon_ShouldSucceed()
    {
        // Arrange
        TaxonRule rule = TaxonRule.Create(_taxon.Id, "product_name", "laptop").Value;

        // Act
        ErrorOr<Taxon> result = _taxon.AddRule(rule);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.TaxonRules.ShouldContain(rule);
        result.Value.GetDomainEvents().ShouldContain(e => e is Taxon.Events.RuleAdded);
    }

    [Fact]
    public void AddRule_ToManualTaxon_ShouldReturnError()
    {
        // Arrange
        Taxon manualTaxon = Taxon.Create("Manual", Guid.NewGuid(), automatic: false).Value;
        TaxonRule rule = TaxonRule.Create(manualTaxon.Id, "product_name", "laptop").Value;

        // Act
        ErrorOr<Taxon> result = manualTaxon.AddRule(rule);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Taxon.AutomaticOnly");
    }

    [Fact]
    public void AddRule_WithNullRule_ShouldReturnError()
    {
        // Act
        ErrorOr<Taxon> result = _taxon.AddRule(null!);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Taxon.NullRule");
    }

    [Fact]
    public void RemoveRule_WithExistingRule_ShouldSucceed()
    {
        // Arrange
        TaxonRule rule = TaxonRule.Create(_taxon.Id, "product_name", "laptop").Value;
        _taxon.AddRule(rule);
        _taxon.ClearDomainEvents();

        // Act
        ErrorOr<Taxon> result = _taxon.RemoveRule(rule.Id);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.TaxonRules.ShouldNotContain(rule);
        result.Value.GetDomainEvents().ShouldContain(e => e is Taxon.Events.RuleRemoved);
    }

    [Fact]
    public void RemoveRule_WithNonExistingRule_ShouldReturnError()
    {
        // Act
        ErrorOr<Taxon> result = _taxon.RemoveRule(Guid.NewGuid());

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Taxon.RuleNotFound");
    }

    [Fact]
    public void ClassifyProduct_WithValidProduct_ShouldSucceed()
    {
        // Arrange
        Guid productId = Guid.NewGuid();

        // Act
        ErrorOr<Taxon> result = _taxon.ClassifyProduct(productId, position: 1);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Classifications.ShouldHaveSingleItem();
        result.Value.GetDomainEvents().ShouldContain(e => e is Taxon.Events.ProductClassified);
    }

    [Fact]
    public void ClassifyProduct_WithEmptyProductId_ShouldReturnError()
    {
        // Act
        ErrorOr<Taxon> result = _taxon.ClassifyProduct(Guid.Empty);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Taxon.NullProduct");
    }

    [Fact]
    public void ClassifyProduct_WithAlreadyClassifiedProduct_ShouldReturnError()
    {
        // Arrange
        Guid productId = Guid.NewGuid();
        _taxon.ClassifyProduct(productId);

        // Act
        ErrorOr<Taxon> result = _taxon.ClassifyProduct(productId);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Taxon.ProductAlreadyClassified");
    }

    [Fact]
    public void UnclassifyProduct_WithClassifiedProduct_ShouldSucceed()
    {
        // Arrange
        Guid productId = Guid.NewGuid();
        _taxon.ClassifyProduct(productId);
        _taxon.ClearDomainEvents();

        // Act
        ErrorOr<Taxon> result = _taxon.UnclassifyProduct(productId);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Classifications.ShouldBeEmpty();
        result.Value.GetDomainEvents().ShouldContain(e => e is Taxon.Events.ProductUnclassified);
    }

    [Fact]
    public void UnclassifyProduct_WithNonClassifiedProduct_ShouldReturnError()
    {
        // Act
        ErrorOr<Taxon> result = _taxon.UnclassifyProduct(Guid.NewGuid());

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Taxon.ProductNotClassified");
    }
}

public class TaxonDeleteTests
{
    [Fact]
    public void Delete_WithNoChildrenOrClassifications_ShouldSucceed()
    {
        // Arrange
        Taxon taxon = Taxon.Create("ToDelete", Guid.NewGuid()).Value;

        // Act
        ErrorOr<Deleted> result = taxon.Delete();

        // Assert
        result.IsError.ShouldBeFalse();
        taxon.GetDomainEvents().ShouldContain(e => e is Taxon.Events.Deleted);
    }

    [Fact]
    public void Delete_WithChildren_ShouldReturnError()
    {
        // Arrange
        Taxon parent = Taxon.Create("Parent", Guid.NewGuid()).Value;
        Taxon child = Taxon.Create("Child", parent.TaxonomyId, parent.Id).Value;
        parent.AddChild(child);

        // Act
        ErrorOr<Deleted> result = parent.Delete();

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Taxon.HasChildren");
    }

    [Fact]
    public void Delete_WithClassifications_ShouldReturnError()
    {
        // Arrange
        Taxon taxon = Taxon.Create("WithProducts", Guid.NewGuid()).Value;
        taxon.ClassifyProduct(Guid.NewGuid());

        // Act
        ErrorOr<Deleted> result = taxon.Delete();

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Taxon.HasClassifications");
    }
}

public class TaxonSlugGenerationTests
{
    [Fact]
    public void GenerateSlug_WithNoParent_ShouldReturnNameAsSlug()
    {
        // Arrange
        Taxon taxon = Taxon.Create("Test Category", Guid.NewGuid()).Value;

        // Act
        string slug = taxon.GenerateSlug();

        // Assert
        slug.ShouldBe("test-category"); // Assumes ToUrl() converts to kebab-case
    }

    [Fact]
    public void GeneratePrettyName_WithNoParent_ShouldReturnName()
    {
        // Arrange
        Taxon taxon = Taxon.Create("Electronics", Guid.NewGuid()).Value;

        // Act
        string prettyName = taxon.GeneratePrettyName();

        // Assert
        prettyName.ShouldBe("Electronics");
    }

    [Fact]
    public void GeneratePrettyName_WithParent_ShouldIncludeParentPath()
    {
        // Arrange
        Taxon parent = Taxon.Create("Electronics", Guid.NewGuid()).Value;
        parent.SetPrettyName();
        Taxon child = Taxon.Create("Laptops", parent.TaxonomyId, parent.Id).Value;
        child.SetParent(parent);

        // Act
        string prettyName = child.GeneratePrettyName();

        // Assert
        prettyName.ShouldBe("Electronics -> Laptops");
    }
}

public class TaxonComputedPropertiesTests
{
    [Fact]
    public void IsManual_WhenNotAutomatic_ShouldReturnTrue()
    {
        // Arrange
        Taxon manualTaxon = Taxon.Create("Manual", Guid.NewGuid(), automatic: false).Value;
        Taxon automaticTaxon = Taxon.Create("Automatic", Guid.NewGuid(), automatic: true).Value;

        // Assert
        manualTaxon.IsManual.ShouldBeTrue();
        automaticTaxon.IsManual.ShouldBeFalse();
    }

    [Fact]
    public void IsManualSortOrder_WhenSortOrderIsManual_ShouldReturnTrue()
    {
        // Arrange
        Taxon manualSort = Taxon.Create("Manual", Guid.NewGuid(), sortOrder: "manual").Value;
        Taxon nameSort = Taxon.Create("NameSort", Guid.NewGuid(), sortOrder: "name-a-z").Value;

        // Assert
        manualSort.IsManualSortOrder.ShouldBeTrue();
        nameSort.IsManualSortOrder.ShouldBeFalse();
    }

    [Fact]
    public void SeoTitle_WhenMetaTitleIsEmpty_ShouldReturnName()
    {
        // Arrange
        Taxon taxon = Taxon.Create("Electronics", Guid.NewGuid()).Value;

        // Assert
        taxon.SeoTitle.ShouldBe("Electronics");
    }

    [Fact]
    public void SeoTitle_WhenMetaTitleIsSet_ShouldReturnMetaTitle()
    {
        // Arrange
        Taxon taxon = Taxon.Create("Electronics", Guid.NewGuid(), metaTitle: "Buy Electronics Online").Value;

        // Assert
        taxon.SeoTitle.ShouldBe("Buy Electronics Online");
    }

    [Fact]
    public void PageBuilderImageUrl_ShouldPrioritizeSquareImage()
    {
        // Arrange
        Taxon taxon = Taxon.Create("Electronics", Guid.NewGuid(),
            imageUrl: "image.jpg",
            squareImageUrl: "square.jpg").Value;

        // Assert
        taxon.PageBuilderImageUrl.ShouldBe("square.jpg");
    }

    [Fact]
    public void PageBuilderImageUrl_WhenNoSquareImage_ShouldReturnImageUrl()
    {
        // Arrange
        Taxon taxon = Taxon.Create("Electronics", Guid.NewGuid(), imageUrl: "image.jpg").Value;

        // Assert
        taxon.PageBuilderImageUrl.ShouldBe("image.jpg");
    }
}

// Test helper extensions
public static class TaxonTestExtensions
{
    public static void ClearDomainEvents(this Taxon taxon)
    {
        // Access the protected ClearDomainEvents method through reflection if needed
        // Or implement a test-specific method in the domain model
        FieldInfo? field = typeof(AuditableEntity).GetField("_domainEvents",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            List<DomainEvent>? events = field.GetValue(taxon) as List<DomainEvent>;
            events?.Clear();
        }
    }
}