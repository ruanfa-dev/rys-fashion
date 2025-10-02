namespace UseCases.Admin.Catalogs.Taxons.Commons;
public static class TaxonResult
{
    public record ListItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? PrettyName { get; set; }
        public string? Permalink { get; set; }
        public Guid TaxonomyId { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }
        public int Lft { get; set; }
        public int Rgt { get; set; }
        public int Depth { get; set; }
        public bool IsRoot { get; set; }
        public int Position { get; set; }
        public bool IsChild { get; set; }
        public bool IsLeaf { get; set; }
        public IDictionary<string, string?> PublicMetadata { get; set; } = new Dictionary<string, string?>();
        public IDictionary<string, string?> PrivateMetadata { get; set; } = new Dictionary<string, string?>();
    }

    public record Details : ListItem
    {
        public string? Description { get; set; }
        public bool Automatic { get; set; }
        public string? RulesMatchPolicy { get; set; }
        public string? SortOrder { get; set; }
        public bool HideFromNav { get; set; }
        public string? ImageUrl { get; set; }
        public string? SquareImageUrl { get; set; }
        public Guid? ParentId { get; set; }
        public ListItem? Parent { get; set; }
        public List<ListItem> Children { get; set; } = [];
    }

    public record TreeItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? PrettyName { get; set; }
        public string? Permalink { get; set; }
        public int Lft { get; set; }
        public int Rgt { get; set; }
        public int Depth { get; set; }
        public List<TreeItem> Children { get; set; } = [];
        public int Position { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }
        public bool IsRoot { get; set; }
        public bool IsChild { get; set; }
        public bool IsLeaf { get; set; }
    }

    public record ComboItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
