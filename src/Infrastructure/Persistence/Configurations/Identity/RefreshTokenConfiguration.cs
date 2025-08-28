using Core.Identity;

using Infrastructure.Persistence.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Identity;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        // Table name
        builder.ToTable(Schema.Roles);

        // Primary key
        builder.HasKey(e => e.Id);

        // Indexes
        builder.HasIndex(e => e.Token).IsUnique();
        builder.HasIndex(e => e.UserId);

        // Properties
        builder.Property(e => e.Token)
            .IsRequired()
            .HasMaxLength(RefreshToken.Constraints.TokenLength);

        builder.Property(e => e.CreatedByIp)
            .IsRequired()
            .HasMaxLength(RefreshToken.Constraints.IpAddressLength);

        builder.Property(e => e.RevokedByIp)
            .HasMaxLength(RefreshToken.Constraints.IpAddressLength);

        builder.Property(e => e.ReplacedByToken)
            .HasMaxLength(RefreshToken.Constraints.TokenLength);

        builder.HasOne(e => e.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
