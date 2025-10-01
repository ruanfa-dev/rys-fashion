using Core.Catalog.Taxonomies;

using Infrastructure.Persistence.Constants;
using Infrastructure.Persistence.Converters;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence.Configurations.Catalogs.Taxonomies;

public sealed class TaxonConfiguration : IEntityTypeConfiguration<Taxon>
{
    public void Configure(EntityTypeBuilder<Taxon> builder)
    {
        // Table & key
        builder.ToTable(Schema.Taxons);
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();

        // Core properties
        builder.Property(t => t.Name)
            .HasMaxLength(Taxon.Constraints.NameMaxLength)
            .IsRequired();

        builder.Property(t => t.PrettyName)
            .HasMaxLength(Taxon.Constraints.PrettyNameMaxLength)
            .IsRequired(false);

        builder.Property(t => t.Description)
            .HasMaxLength(Taxon.Constraints.DescriptionMaxLength)
            .IsRequired(false);

        builder.Property(t => t.Permalink)
            .HasMaxLength(Taxon.Constraints.PermalinkMaxLength)
            .IsRequired();

        // Behavior flags and fields
        builder.Property(t => t.Automatic).IsRequired();
        builder.Property(t => t.RulesMatchPolicy).HasMaxLength(32).IsRequired();
        builder.Property(t => t.SortOrder).HasMaxLength(32).IsRequired();
        builder.Property(t => t.HideFromNav).IsRequired();
        builder.Property(t => t.MarkedForRegenerateTaxonProducts).IsRequired();

        // Nested set / hierarchy
        builder.Property(t => t.Lft).IsRequired();
        builder.Property(t => t.Rgt).IsRequired();
        builder.Property(t => t.Depth).IsRequired();
        builder.Property(t => t.ChildIndex).IsRequired();

        // Parent-child self-referencing relationship
        builder.HasOne(t => t.Parent)
            .WithMany(p => p.Children)
            .HasForeignKey(t => t.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Taxonomy relationship
        builder.HasOne(t => t.Taxonomy)
            .WithMany(x => x.Taxons)
            .HasForeignKey(t => t.TaxonomyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Collections relations (classifications, rules, prototype/promotion joins) are mapped by their own configs,
        // ensure navigations exist but don't reconfigure them here:
        builder.HasMany(t => t.Classifications).WithOne(c => c.Taxon).HasForeignKey(c => c.TaxonId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(t => t.TaxonRules).WithOne(tr => tr.Taxon).HasForeignKey(tr => tr.TaxonId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(t => t.PrototypeTaxons).WithOne(pt => pt.Taxon).HasForeignKey(pt => pt.TaxonId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(t => t.PromotionRuleTaxons).WithOne(prt => prt.Taxon).HasForeignKey(prt => prt.TaxonId).OnDelete(DeleteBehavior.Cascade);

        // Metadata conversions (JSON) for dictionaries
        ValueConverter<IDictionary<string, string?>?, string?> dictConverter = DictionaryJsonConverter.GetConverter();
        ValueComparer<IDictionary<string, string?>?> dictComparer = DictionaryJsonConverter.GetComparer();

        builder.Property(t => t.PublicMetadata)
            .HasConversion(dictConverter);
        builder.Property(t => t.PublicMetadata)
            .Metadata.SetValueComparer(dictComparer);

        builder.Property(t => t.PrivateMetadata)
            .HasConversion(dictConverter);
        builder.Property(t => t.PrivateMetadata)
            .Metadata.SetValueComparer(dictComparer);

        // Indexes
        builder.HasIndex(t => t.TaxonomyId);
        // Ensure permalink uniqueness within taxonomy (prevent duplicate permalinks per taxonomy)
        builder.HasIndex(t => new { t.TaxonomyId, Permalink = t.Permalink }).IsUnique();

        // Ignore convenience / computed properties not part of relational model
        builder.Ignore("TranslatableFields");     // often provided by ITranslatable implementations
        builder.Ignore("TranslationEntries");     // read-only projection of translations
        builder.Ignore("IsRoot");
        builder.Ignore("IsManual");
        builder.Ignore("IsManualSortOrder");
        builder.Ignore("PageBuilderImageUrl");
        builder.Ignore("SeoTitle");
        builder.Ignore("Slug");
    }
}