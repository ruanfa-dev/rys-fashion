using Core.Catalogs;

using Infrastructure.Persistence.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Catalogs;
public class ProductPropertyConfiguration : IEntityTypeConfiguration<ProductProperty>
{
    public void Configure(EntityTypeBuilder<ProductProperty> builder)
    {
        // Configure table name
        builder.ToTable(Schema.ProductProperties);

        // Configure primary key
        builder.HasKey(pp => pp.Id);
        builder.HasIndex(pp => new { pp.ProductId, pp.PropertyId }).IsUnique();

        // Relationships
        builder.HasOne(pp => pp.Product)
               .WithMany(p => p.ProductProperties)
               .HasForeignKey(pp => pp.ProductId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pp => pp.Property)
               .WithMany(p => p.ProductProperties)
               .HasForeignKey(pp => pp.PropertyId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
