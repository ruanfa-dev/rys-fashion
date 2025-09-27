namespace UseCases.Admin.Catalogs.Properties.Commons;
public class PropertyResult
{
    public record class ListItem : PropertyParam
    {
        // Identity
        public required Guid Id { get; set; }

        // Audit
        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        // Statistics
        public int ProductsCount { get; set; } = 0;
        public int PrototypeCount { get; set; } = 0;
    }

    public record class Details : ListItem
    {
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public record class ComboItem
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Presentation { get; set; }
    }
}
