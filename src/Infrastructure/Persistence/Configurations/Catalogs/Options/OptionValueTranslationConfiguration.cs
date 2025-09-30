using Core.Catalog.Options;
using Infrastructure.Persistence.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Catalogs.Options;

public sealed class OptionValueTranslationConfiguration : IEntityTypeConfiguration<OptionValueTranslation>
{
    public void Configure(EntityTypeBuilder<OptionValueTranslation> builder)
    {
        // Table name
        builder.ToTable(Schema.TranslationFor(Schema.OptionValues));

        // Primary key
        builder.HasKey(t => t.Id);

        // Properties
        builder.Property(t => t.OptionValueId).IsRequired();

        // Relationships
        builder.HasOne(t => t.OptionValue)
            .WithMany(p => p.Translations)
            .HasForeignKey(t => t.OptionValueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
