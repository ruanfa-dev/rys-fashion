using Core.Catalog.Taxonomies;

using Infrastructure.Persistence.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Catalogs;

internal sealed class TaxonomyConfiguration : IEntityTypeConfiguration<Taxonomy>
{
    public void Configure(EntityTypeBuilder<Taxonomy> builder)
    {
        // Table name
        builder.ToTable(Schema.Taxonomies);

        // Primary key
        builder.HasKey(t => t.Id);

        // Properties
        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(Taxonomy.Constraints.NameMaxLength);

        builder.Property(t => t.Position)
            .IsRequired();

        builder.Property(t => t.StoreId)
            .IsRequired();

        // Relationships
        builder.HasOne(t => t.Store)
            .WithMany()
            .HasForeignKey(t => t.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Taxons)
            .WithOne(tn => tn.Taxonomy)
            .HasForeignKey(tn => tn.TaxonomyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Translations: 
        builder.HasMany(t => t.Translations)
            .WithOne(tt => tt.Taxonomy)
            .HasForeignKey(tt => tt.TaxonomyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(t => new { t.StoreId, t.Name });
        builder.HasIndex(t => new { t.Position, t.CreatedAt });
        builder.HasIndex(t => new { t.StoreId, t.Name }).IsUnique();
    }
}
