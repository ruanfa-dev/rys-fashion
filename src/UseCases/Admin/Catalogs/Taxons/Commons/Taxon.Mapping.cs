using Core.Catalog.Taxonomies;

using Mapster;

namespace UseCases.Admin.Catalogs.Taxons.Commons;
public class TaxonMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // List item
        config.NewConfig<Taxon, TaxonResult.ListItem>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.PrettyName, src => src.PrettyName)
            .Map(dest => dest.Permalink, src => src.Permalink)
            .Map(dest => dest.TaxonomyId, src => src.TaxonomyId)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Map(dest => dest.MetaTitle, src => src.SeoTitle)
            .Map(dest => dest.MetaDescription, src => src.MetaDescription)
            .Map(dest => dest.MetaKeywords, src => src.MetaKeywords)
            .Map(dest => dest.Lft, src => src.Lft)
            .Map(dest => dest.Rgt, src => src.Rgt)
            .Map(dest => dest.Depth, src => src.Depth)
            .Map(dest => dest.IsRoot, src => src.IsRoot)
            .Map(dest => dest.Position, src => src.ChildIndex)
            .Map(dest => dest.IsChild, src => src.ParentId != null)
            .Map(dest => dest.IsLeaf, src => !src.Children.Any())
            .Map(dest => dest.PublicMetadata, src => src.PublicMetadata ?? new Dictionary<string, string?>())
            .Map(dest => dest.PrivateMetadata, src => src.PrivateMetadata ?? new Dictionary<string, string?>())
            .IgnoreNullValues(true);

        // Details
        config.NewConfig<Taxon, TaxonResult.Details>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.PrettyName, src => src.PrettyName)
            .Map(dest => dest.Permalink, src => src.Permalink)
            .Map(dest => dest.TaxonomyId, src => src.TaxonomyId)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Map(dest => dest.MetaTitle, src => src.SeoTitle)
            .Map(dest => dest.MetaDescription, src => src.MetaDescription)
            .Map(dest => dest.MetaKeywords, src => src.MetaKeywords)
            .Map(dest => dest.Lft, src => src.Lft)
            .Map(dest => dest.Rgt, src => src.Rgt)
            .Map(dest => dest.Depth, src => src.Depth)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.Automatic, src => src.Automatic)
            .Map(dest => dest.RulesMatchPolicy, src => src.RulesMatchPolicy)
            .Map(dest => dest.SortOrder, src => src.SortOrder)
            .Map(dest => dest.HideFromNav, src => src.HideFromNav)
            .Map(dest => dest.ImageUrl, src => src.ImageUrl)
            .Map(dest => dest.SquareImageUrl, src => src.SquareImageUrl)
            .Map(dest => dest.ParentId, src => src.ParentId)
            .Map(dest => dest.Parent, src => src.Parent)
            .Map(dest => dest.Children, src => src.Children)
            .Map(dest => dest.PublicMetadata, src => src.PublicMetadata ?? new Dictionary<string, string?>())
            .Map(dest => dest.PrivateMetadata, src => src.PrivateMetadata ?? new Dictionary<string, string?>())
            .Map(dest => dest.Position, src => src.ChildIndex)
            .Map(dest => dest.IsChild, src => src.ParentId != null)
            .Map(dest => dest.IsLeaf, src => !src.Children.Any())
            .Map(dest => dest.IsRoot, src => src.IsRoot)
            .IgnoreNullValues(true);

        // Tree item
        config.NewConfig<Taxon, TaxonResult.TreeItem>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.PrettyName, src => src.PrettyName)
            .Map(dest => dest.Permalink, src => src.Permalink)
            .Map(dest => dest.Lft, src => src.Lft)
            .Map(dest => dest.Rgt, src => src.Rgt)
            .Map(dest => dest.Depth, src => src.Depth)
            .Map(dest => dest.Children, src => src.Children)
            .Map(dest => dest.Position, src => src.ChildIndex)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Map(dest => dest.MetaTitle, src => src.SeoTitle)
            .Map(dest => dest.MetaDescription, src => src.MetaDescription)
            .Map(dest => dest.MetaKeywords, src => src.MetaKeywords)
            .Map(dest => dest.IsRoot, src => src.IsRoot)
            .Map(dest => dest.IsChild, src => src.ParentId != null)
            .Map(dest => dest.IsLeaf, src => !src.Children.Any())
            .IgnoreNullValues(true);

        // Combo item
        config.NewConfig<Taxon, TaxonResult.ComboItem>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name);
    }
}
