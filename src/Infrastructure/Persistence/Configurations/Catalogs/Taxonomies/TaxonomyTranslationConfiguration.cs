using Core.Catalog.Taxonomies;

using Infrastructure.Persistence.Constants;
using Infrastructure.Persistence.Converters;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Catalogs.Taxonomies;

internal sealed class TaxonomyTranslationConfiguration : IEntityTypeConfiguration<TaxonomyTranslation>
{
    public void Configure(EntityTypeBuilder<TaxonomyTranslation> builder)
    {
        // Table name
        builder.ToTable(Schema.TranslationFor(Schema.Taxonomies));

        // Primary key
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TaxonomyId).IsRequired();

        builder.Property(t => t.IsDefault)
            .IsRequired();

        builder.HasIndex(t => new { t.TaxonomyId, t.Culture }).IsUnique();


        // Relationships
        builder.HasOne(t => t.Taxonomy)
            .WithMany(t => t.Translations)
            .HasForeignKey(t => t.TaxonomyId)
            .OnDelete(DeleteBehavior.Cascade);


    }
}
