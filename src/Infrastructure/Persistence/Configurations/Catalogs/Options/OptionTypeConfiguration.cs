
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

		builder.Property(ot => ot.Presentation).HasMaxLength(OptionType.Constraints.PresentationMaxLength).IsRequired();
		builder.Property(ot => ot.Position).IsRequired();

		builder.HasMany(ot => ot.OptionValues).WithOne(ov => ov.OptionType).HasForeignKey(ov => ov.OptionTypeId).OnDelete(DeleteBehavior.Cascade);
		// OptionTypePrototypes handled separately
	}
}
