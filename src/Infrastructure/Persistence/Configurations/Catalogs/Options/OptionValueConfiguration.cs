using Core.Catalog.Options;

using Infrastructure.Persistence.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Catalogs.Options;

public class OptionValueConfiguration : IEntityTypeConfiguration<OptionValue>
{
    public void Configure(EntityTypeBuilder<OptionValue> builder)
    {
        // Table name
        builder.ToTable(Schema.OptionValues);

        // Primary key
        builder.HasKey(ov => ov.Id);

        // Properties
        builder.Property(ov => ov.Name)
            .IsRequired()
            .HasMaxLength(OptionValue.Constraints.NameMaxLength);
        builder.Property(ov => ov.Presentation)
            .IsRequired()
            .HasMaxLength(OptionValue.Constraints.PresentationMaxLength);
        builder.Property(ov => ov.Position)
            .IsRequired()
            .HasMaxLength(OptionValue.Constraints.PositionMax);
        builder.Property(ov => ov.OptionTypeId)
            .IsRequired();

        // Relationships
        builder.HasOne(ov => ov.OptionType)
            .WithMany(ot => ot.OptionValues)
            .HasForeignKey(ov => ov.OptionTypeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(ov => ov.OptionValueVariants)
            .WithOne(ovv => ovv.OptionValue)
            .HasForeignKey(ovv => ovv.OptionValueId)
            .OnDelete(DeleteBehavior.Cascade);

		// Translations relationship
		builder.HasMany(ov => ov.Translations)
			.WithOne(t => t.OptionValue)
			.HasForeignKey(t => t.OptionValueId)
			.OnDelete(DeleteBehavior.Cascade);

		// Indexes
		builder.HasIndex(ov => ov.Name);
		builder.HasIndex(ov => ov.Presentation);
		builder.HasIndex(ov => ov.Position);
    }
}
		