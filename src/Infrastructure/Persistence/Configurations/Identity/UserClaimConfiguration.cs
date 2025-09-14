using Core.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Identity;

public class UserClaimConfiguration : IEntityTypeConfiguration<UserClaim>
{
    public void Configure(EntityTypeBuilder<UserClaim> builder)
    {
        builder.HasOne(uc => uc.User)
               .WithMany(u => u.UserClaims)
               .HasForeignKey(uc => uc.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
