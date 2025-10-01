using Infrastructure.Persistence.Converters;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SharedKernel.Domain.Attributes.TranslatableResource;

namespace Infrastructure.Persistence.Configurations.Common.Attributes;

/// <summary>
/// Generic configuration helper for entities implementing ITranslatable{TTranslation}.
/// This configures some non-relationship concerns and instructs EF to ignore the
/// TranslatableFields helper properties on the entity.
/// Concrete translation entity configuration should configure the inverse FK and navigation
/// (see PropertyTranslationConfiguration) so this class keeps the mapping minimal to avoid
/// relationship conflicts.
/// </summary>  
public sealed class TranslatableEntityConfiguration<TEntity, TTranslation> : IEntityTypeConfiguration<TEntity>
    where TEntity : class, ITranslatable<TTranslation>
    where TTranslation : class, ITranslation
{
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        // Do not configure the one-to-many relationship here to avoid duplicate/ambiguous
        // relationship mappings. Concrete entity configurations should define the inverse
        // (HasOne/HasForeignKey) so EF can bind the FK property explicitly.

        // Ignore helper/read-only properties so EF Core doesn't try to map them.
        builder.Ignore(m => m.TranslatableFields);

        // Many entities expose a convenience TranslationEntries property (read-only projection).
        // It's not part of the relational model and should be ignored if present.
    }
}
