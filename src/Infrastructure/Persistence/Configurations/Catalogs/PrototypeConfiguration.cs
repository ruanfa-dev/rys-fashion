using Core.Catalogs;

using Infrastructure.Persistence.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Catalogs;

public class PrototypeConfiguration: IEntityTypeConfiguration<Prototype>
{
    public void Configure(EntityTypeBuilder<Prototype> builder)
    {
        // Configure table name
        builder.ToTable(Schema.Prototypes);
        // Configure primary key
        builder.HasKey(p => p.Id);
        //builder.HasIndex(p => p.Name).IsUnique();
        //// Configure properties
        //builder.Property(p => p.Name)
        //    .HasMaxLength(Prototype.Constraints.MaxNameLength)
        //    .IsRequired();
        //builder.Property(p => p.Description)
        //    .HasMaxLength(Prototype.Constraints.MaxDescriptionLength)
        //    .IsRequired(false);
    }
}