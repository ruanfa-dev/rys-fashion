using Core.Catalog.Taxonomies;

using Infrastructure.Persistence.Constants;
using Infrastructure.Persistence.Converters;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Catalogs.Taxonomies;

public sealed class TaxonTranslationConfiguration : IEntityTypeConfiguration<TaxonTranslation>
{
    public void Configure(EntityTypeBuilder<TaxonTranslation> builder)
    {
        // Table & key
        builder.ToTable(Schema.TranslationFor(Schema.Taxons));
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();

        // Required FK to parent Taxon
        builder.Property(t => t.TaxonId).IsRequired();
        builder.HasOne(t => t.Taxon)
            .WithMany(taxon => taxon.Translations)
            .HasForeignKey(t => t.TaxonId)
            .OnDelete(DeleteBehavior.Cascade);

        // Culture and default flag are configured by the generic TranslationEntityConfiguration
        // but add an index useful for lookups (taxon + culture).
        builder.HasIndex(t => new { t.TaxonId, t.Culture });

        // Map Fields dictionary to JSON with converter + comparer for change-tracking
        var converter = DictionaryJsonConverter.GetConverter();
        var comparer = DictionaryJsonConverter.GetComparer();

        builder.Property(t => t.Fields)
            .HasConversion(converter);

        builder.Property(t => t.Fields)
            .Metadata.SetValueComparer(comparer);

        // Optionally set DB column type for JSON (uncomment if you target Postgres jsonb)
        // builder.Property(t => t.Fields).HasColumnType("jsonb");
    }
}
