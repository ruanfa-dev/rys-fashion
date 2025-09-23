using Core.Catalogs;

using Infrastructure.Persistence.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Catalogs;
public class OptionValueConfiguration : IEntityTypeConfiguration<OptionValue>
{
    public void Configure(EntityTypeBuilder<OptionValue> builder)
    {
        // Table name
        builder.ToTable(name: Schema.OptionValues);
        // Primary key
        builder.HasKey(e => e.Id);
        // Properties
        builder.Property(e => e.Name)
            .HasMaxLength(maxLength: OptionValue.Constraints.NameMinLength)
            .IsRequired();
        builder.Property(e => e.Presentation)
            .HasMaxLength(maxLength: OptionValue.Constraints.PresentationMaxLength)
            .IsRequired();
        // Relationships
        builder.HasOne(e => e.OptionType)
            .WithMany(ot => ot.OptionValues)
            .HasForeignKey(e => e.OptionTypeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.VariantOptionValues)
            .WithOne(ovv => ovv.OptionValue)
            .HasForeignKey(ovv => ovv.OptionValueId)
            .OnDelete(DeleteBehavior.Cascade);
        // Indexes
        builder.HasIndex(e => new { e.OptionTypeId, e.Name }).IsUnique();
        builder.HasIndex(e => e.Position);
    }
}
