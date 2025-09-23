using Core.Catalogs;

using Infrastructure.Persistence.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Catalogs;
public class OptionTypeConfiguration : IEntityTypeConfiguration<OptionType>
{
    public void Configure(EntityTypeBuilder<OptionType> builder)
    {
        // Table name
        builder.ToTable(name: Schema.OptionTypes);
        // Primary key
        builder.HasKey(e => e.Id);
        // Properties
        builder.Property(e => e.Name)
            .HasMaxLength(maxLength: OptionType.Constraints.NameMaxLength)
            .IsRequired();
        builder.Property(e => e.Presentation)
            .HasMaxLength(maxLength: OptionType.Constraints.PresentationMaxLength)
            .IsRequired();
        // Relationships
        builder.HasMany(e => e.OptionValues)
            .WithOne(ov => ov.OptionType)
            .HasForeignKey(ov => ov.OptionTypeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.ProductOptionTypes)
            .WithOne(pot => pot.OptionType)
            .HasForeignKey(pot => pot.OptionTypeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.PrototypeOptionTypes)
            .WithOne(ot => ot.OptionType)
            .HasForeignKey(ot => ot.OptionTypeId)
            .OnDelete(DeleteBehavior.Cascade);
        // Indexes
        builder.HasIndex(e => e.Name).IsUnique();

    }
}
