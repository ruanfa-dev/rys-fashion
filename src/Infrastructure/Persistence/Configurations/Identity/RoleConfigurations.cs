using Core.Identity;

using Infrastructure.Persistence.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Identity;

public sealed class RoleConfigurations : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable(Schema.Roles);

        // Identity properties
        builder.HasKey(r => r.Id);

        // Index
        builder.HasIndex(r => r.NormalizedName)
            .IsUnique();

        // Properties
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(Role.Constraints.MaxNameLength);

        builder.Property(e => e.NormalizedName)
            .IsRequired()
            .HasMaxLength(Role.Constraints.MaxNameLength);

        builder.Property(e => e.Description)
            .HasMaxLength(Role.Constraints.MaxDescriptionLength);
    }
}
