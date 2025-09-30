
using Core.Catalog.Options;

using Infrastructure.Persistence.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Catalogs.Options;
public class OptionValueConfiguration : IEntityTypeConfiguration<OptionValue>
{
	public void Configure(EntityTypeBuilder<OptionValue> builder)
	{
		builder.ToTable(Schema.OptionValues);
		builder.HasKey(ov => ov.Id);

	builder.Property(ov => ov.Name).IsRequired();
	builder.Property(ov => ov.Presentation).IsRequired().HasMaxLength(255);

	builder.HasOne(ov => ov.OptionType).WithMany(ot => ot.OptionValues).HasForeignKey(ov => ov.OptionTypeId).OnDelete(DeleteBehavior.Cascade);
	}
}
