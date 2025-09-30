using Core.Catalog.Options;

using Infrastructure.Persistence.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Catalogs.Options;

public sealed class OptionTypePrototypeConfiguration : IEntityTypeConfiguration<OptionTypePrototype>
{
    public void Configure(EntityTypeBuilder<OptionTypePrototype> builder)
    {
        // Table name
        builder.ToTable(Schema.PrototypeOptionTypes);

        // Primary key
        builder.HasKey(otp => otp.Id);

        // Relationships
        builder.HasOne(otp => otp.OptionType)
            .WithMany(ot => ot.OptionTypePrototypes)
            .HasForeignKey(otp => otp.OptionTypeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(otp => otp.Prototype)
            .WithMany(p => p.OptionTypePrototypes)
            .HasForeignKey(otp => otp.PrototypeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(otp => new { otp.OptionTypeId, otp.PrototypeId }).IsUnique();
    }
}