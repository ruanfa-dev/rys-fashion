using Infrastructure.Identity.Models;
using Infrastructure.Persistence.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Identity;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table name
        builder.ToTable(Schema.Users);

        // Primary key
        builder.HasKey(u => u.Id);
        builder.HasIndex(e => e.Email).IsUnique();
        builder.HasIndex(e => e.UserName).IsUnique();
        builder.HasIndex(e => e.PhoneNumber);

        #region User informations
        builder.Property(u => u.Email)
            .HasMaxLength(User.Constraints.EmailMaxLength)
            .IsRequired();
        #endregion

        #region Tracking
        builder.Property(u => u.LastSignInIp)
            .HasMaxLength(User.Constraints.IpAddressMaxLength)
            .IsRequired(false);
        builder.Property(u => u.CurrentSignInAt)
            .HasMaxLength(User.Constraints.IpAddressMaxLength)
            .IsRequired(false);
        #endregion
    }
}
