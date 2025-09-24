using Core.Identity;

using Infrastructure.Persistence.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Identity;

public sealed class RoleConfigurations : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        // Table name
        builder.ToTable(Schema.Roles);

        // Identity properties
        builder.HasKey(r => r.Id);
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(Role.Constraints.MaxNameLength);

        builder.Property(e => e.NormalizedName)
            .IsRequired()
            .HasMaxLength(Role.Constraints.MaxNameLength);

        builder.Property(e => e.Description)
            .HasMaxLength(Role.Constraints.MaxDescriptionLength);


        // Index
        builder.HasIndex(r => r.NormalizedName)
            .IsUnique();
        builder.HasIndex(r => r.IsSystemRole);
        builder.HasIndex(r => r.IsDefault);
        builder.HasIndex(r => r.Priority);
    }
}
