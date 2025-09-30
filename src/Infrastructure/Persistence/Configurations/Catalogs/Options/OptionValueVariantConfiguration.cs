
using Core.Catalog.Options;

using Infrastructure.Persistence.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Catalogs.Options;
public class OptionValueVariantConfiguration : IEntityTypeConfiguration<OptionValueVariant>
{
    public void Configure(EntityTypeBuilder<OptionValueVariant> builder)
    {
        // Table name
        builder.ToTable(Schema.VariantOptionValues);

        // Primary key
        builder.HasKey(ovv => ovv.Id);

        // Properties
        builder.Property(ovv => ovv.OptionValueId).IsRequired();
        builder.HasOne(ovv => ovv.OptionValue)
            .WithMany(ov => ov.OptionValueVariants)
            .HasForeignKey(ovv => ovv.OptionValueId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(ovv => ovv.Variant)
            .WithMany(v => v.OptionValueVariants)
            .HasForeignKey(ovv => ovv.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
