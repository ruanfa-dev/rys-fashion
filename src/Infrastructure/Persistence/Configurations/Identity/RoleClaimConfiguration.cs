using Core.Identity;

using Infrastructure.Persistence.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Identity;

public class RoleClaimConfiguration : IEntityTypeConfiguration<RoleClaim>
{
    public void Configure(EntityTypeBuilder<RoleClaim> builder)
    {
        // Table name
        builder.ToTable(Schema.RoleClaims);

        // Configure relationships
        builder.HasOne(rc => rc.Role)
               .WithMany(r => r.RoleClaims)
               .HasForeignKey(rc => rc.RoleId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
