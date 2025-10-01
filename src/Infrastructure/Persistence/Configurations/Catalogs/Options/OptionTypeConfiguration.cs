using Core.Catalog.Options;

using Infrastructure.Persistence.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Catalogs.Options;

public class OptionTypeConfiguration : IEntityTypeConfiguration<OptionType>
{
	public void Configure(EntityTypeBuilder<OptionType> builder)
	{
		// Table name and key
		builder.ToTable(Schema.OptionTypes);

		// Primary key
		builder.HasKey(ot => ot.Id);

		// Relationships
		builder.HasMany(ot => ot.OptionValues)
			.WithOne(ov => ov.OptionType)
			.HasForeignKey(ov => ov.OptionTypeId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(ot => ot.ProductOptionTypes)
			.WithOne(pot => pot.OptionType)
			.HasForeignKey(pot => pot.OptionTypeId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(ot => ot.OptionTypePrototypes)
			.WithOne(otp => otp.OptionType)
			.HasForeignKey(otp => otp.OptionTypeId)
			.OnDelete(DeleteBehavior.Cascade);

		// Translations relationship
		builder.HasMany(ot => ot.Translations)
			.WithOne(t => t.OptionType)
			.HasForeignKey(t => t.OptionTypeId)
			.OnDelete(DeleteBehavior.Cascade);

		// Indexes
		builder.HasIndex(ot => ot.Filterable);
	}
}
