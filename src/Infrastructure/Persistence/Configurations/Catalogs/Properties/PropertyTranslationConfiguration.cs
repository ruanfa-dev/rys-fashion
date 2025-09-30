using Core.Catalog.Properties;

using Infrastructure.Persistence.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Catalogs.Properties;

public sealed class PropertyTranslationConfiguration : IEntityTypeConfiguration<PropertyTranslation>
{
    public void Configure(EntityTypeBuilder<PropertyTranslation> builder)
    {
        // Table name
        builder.ToTable(Schema.TranslationFor(Schema.Properties));

        builder.HasKey(t => t.Id);

        builder.Property(t => t.PropertyId).IsRequired();

        builder.Property(t => t.Culture)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(t => t.IsDefault)
            .IsRequired();

        // Indexes: property id + culture unique to prevent duplicate translations
        builder.HasIndex(t => new { t.PropertyId, t.Culture }).IsUnique();

        // Relationship configured from Property side (HasMany). Ensure FK is configured here as well.
        builder.HasOne(t => t.Property)
            .WithMany(p => p.Translations)
            .HasForeignKey(t => t.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
