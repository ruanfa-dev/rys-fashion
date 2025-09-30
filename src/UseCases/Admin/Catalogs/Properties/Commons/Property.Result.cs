namespace UseCases.Admin.Catalogs.Properties.Commons;
public class PropertyResult
{
    public record ListItem : PropertyParam
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

    public record Details : PropertyParam
    {
        // Identity

        public required Guid Id { get; set; }

        // Audit
        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public record ComboItem
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Presentation { get; set; }
    }
}
