
using Core.Catalog.Options;

using Infrastructure.Persistence.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Catalogs.Options;
public class OptionValueVariantConfiguration : IEntityTypeConfiguration<OptionValueVariant>
{
    public void Configure(EntityTypeBuilder<OptionValueVariant> builder)
    {
    builder.ToTable(Schema.VariantOptionValues);
        builder.HasKey(ovv => ovv.Id);

        builder.HasOne(ovv => ovv.OptionValue).WithMany(ov => ov.OptionValueVariants).HasForeignKey(ovv => ovv.OptionValueId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(ovv => ovv.Variant).WithMany(v => v.OptionValueVariants).HasForeignKey(ovv => ovv.VariantId).OnDelete(DeleteBehavior.Cascade);
    }
}
