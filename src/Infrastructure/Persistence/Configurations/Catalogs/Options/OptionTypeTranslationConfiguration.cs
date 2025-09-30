using Core.Catalog.Options;

using Infrastructure.Persistence.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Catalogs.Options;

public sealed class OptionTypeTranslationConfiguration : IEntityTypeConfiguration<OptionTypeTranslation>
{
    public void Configure(EntityTypeBuilder<OptionTypeTranslation> builder)
    {
        // Table name
        builder.ToTable(Schema.TranslationFor(Schema.OptionTypes));

        // Primary key
        builder.HasKey(t => t.Id);

        // Properties
        builder.Property(t => t.OptionTypeId).IsRequired();

        // Relationships
        builder.HasOne(t => t.OptionType)
            .WithMany(p => p.Translations)
            .HasForeignKey(t => t.OptionTypeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
