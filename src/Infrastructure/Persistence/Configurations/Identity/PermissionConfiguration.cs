using Core.Identity;
using Core.Identity.Permissions;

using Infrastructure.Persistence.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Identity;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable(Schema.Permissions);

        // Primary key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(Permission.Constraints.MaxNameLength);

        builder.Property(p => p.Area)
            .IsRequired()
            .HasMaxLength(Permission.Constraints.MaxSegmentLength);

        builder.Property(p => p.Resource)
            .IsRequired()
            .HasMaxLength(Permission.Constraints.MaxSegmentLength);

        builder.Property(p => p.Action)
            .IsRequired()
            .HasMaxLength(Permission.Constraints.MaxSegmentLength);

        builder.Property(p => p.DisplayName)
            .HasMaxLength(Permission.Constraints.MaxDisplayNameLength);

        builder.Property(p => p.Description)
            .HasMaxLength(Permission.Constraints.MaxDescriptionLength);

        builder.Property(p => p.Category)
            .HasConversion<int>();

        // Indexes
        builder.HasIndex(p => p.Name)
            .IsUnique();

        builder.HasIndex(p => new { p.Area, p.Resource, p.Action })
            .IsUnique();
    }
}