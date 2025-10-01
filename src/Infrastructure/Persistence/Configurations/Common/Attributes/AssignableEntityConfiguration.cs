using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SharedKernel.Domain.Attributes.Assignable;

using UseCases.Common.Persistence.Constants;

namespace Infrastructure.Persistence.Configurations.Common.Attributes;
public class AssignableEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : class, IAssignable
{
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(propertyExpression: e => e.AssignedBy)
            .HasMaxLength(maxLength: Constraints.CreatedByMaxLength);

        builder.HasIndex(indexExpression: e => e.AssignedAt);
        builder.HasIndex(indexExpression: e => e.AssignedBy);
    }
}
