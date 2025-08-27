using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SharedKernel.Domain.Attributes;

using UseCases.Common.Persistence.Constants;

namespace Infrastructure.Persistence.Configurations.Common;
public sealed class AuditableEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : class, IAuditable
{
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(propertyExpression: e => e.CreatedAt)
            .IsRequired();

        builder.Property(propertyExpression: e => e.UpdatedAt);

        builder.Property(propertyExpression: e => e.CreatedBy)
            .HasMaxLength(maxLength: Constraints.CreatedByMaxLength);

        builder.Property(propertyExpression: e => e.CreatedAt)
            .HasMaxLength(maxLength: Constraints.UpdatedByMaxLength);

        builder.HasIndex(indexExpression: e => e.CreatedAt);
        builder.HasIndex(indexExpression: e => e.CreatedBy);
        builder.HasIndex(indexExpression: e => e.UpdatedAt);
    }
}
