using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SharedKernel.Domain.Attributes.Status;

namespace Infrastructure.Persistence.Configurations.Common.Attributes;

/// <summary>
/// Generic configuration for entities that implement <see cref="IHasStatus{TStatus}"/>.
/// Maps the Status enum to its integer representation and adds an index for fast filtering.
/// </summary>
public sealed class HasStatusEntityConfiguration<TEntity, TStatus> : IEntityTypeConfiguration<TEntity>
    where TEntity : class, IHasStatus<TStatus>
    where TStatus : struct, Enum
{
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        // Map enum to int for storage
        builder.Property(e => e.Status)
            .HasConversion<int>();

        // Add index on status to speed up common queries (e.g., filtering by status)
        builder.HasIndex(e => e.Status);
    }
}
