using Core.Catalogs;

using Infrastructure.Persistence.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Catalogs;

public class PrototypePropertyConfiguration : IEntityTypeConfiguration<PrototypeProperty>
{
    public void Configure(EntityTypeBuilder<PrototypeProperty> builder)
    {
        // Configure table name
        builder.ToTable(Schema.PrototypeProperties);
        // Configure primary key
        builder.HasKey(pp => pp.Id);
        builder.HasIndex(pp => new { pp.PrototypeId, pp.PropertyId }).IsUnique();
        // Relationships
        builder.HasOne(pp => pp.Prototype)
               .WithMany(p => p.PrototypeProperties)
               .HasForeignKey(pp => pp.PrototypeId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(pp => pp.Property)
               .WithMany(p => p.PrototypeProperties)
               .HasForeignKey(pp => pp.PropertyId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}