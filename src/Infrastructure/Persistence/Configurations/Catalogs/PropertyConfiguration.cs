using Core.Catalog.Properties;

using Infrastructure.Persistence.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Catalogs;
public sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        // Table name
        builder.ToTable(Schema.Properties);

        // Primary key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.Name)
            .HasMaxLength(Property.Constraints.NameMaxLength)
            .IsRequired();
        builder.Property(p => p.Presentation)
            .HasMaxLength(Property.Constraints.PresentationMaxLength)
            .IsRequired();
        builder.Property(p => p.Kind)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(p => p.DisplayOn)
            .HasConversion<int>()
            .IsRequired();

        // Relationships
        builder.HasMany(p => p.PrototypeProperties)
            .WithOne(pp => pp.Property)
            .HasForeignKey(pp => pp.PropertyId);

        builder.HasMany(p => p.ProductProperties)
            .WithOne(pp => pp.Property)
            .HasForeignKey(pp => pp.PropertyId);

        // Indexes
        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.Presentation);
        builder.HasIndex(p => p.Position);

    }
}