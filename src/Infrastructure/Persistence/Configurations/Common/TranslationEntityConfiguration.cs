using Infrastructure.Persistence.Converters;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SharedKernel.Domain.Attributes.TranslatableResource;

namespace Infrastructure.Persistence.Configurations.Common;

/// <summary>
/// Generic configuration for translation entities implementing ITranslation.
/// Configures common columns: Culture, IsDefault and Fields (JSON).
/// </summary>
public sealed class TranslationEntityConfiguration<TTranslation> : IEntityTypeConfiguration<TTranslation>
    where TTranslation : class, ITranslation
{
    public void Configure(EntityTypeBuilder<TTranslation> builder)
    {
        // Culture column
        builder.Property<string>(m => m.Culture)
            .HasMaxLength(TranslatableConstraints.CultureMaxLength)
            .IsRequired();

        // IsDefault flag - bool is non-nullable, mark as required (cannot be optional)
        builder.Property<bool>(m => m.IsDefault)
            .IsRequired();

        // Fields dictionary stored as JSON using the nullable-aware converter/comparer
        var converter = DictionaryJsonConverter.GetConverter();
        var comparer = DictionaryJsonConverter.GetComparer();

        builder.Property(t => t.Fields)
            .HasConversion(converter);

        builder.Property(t => t.Fields)
            .Metadata.SetValueComparer(comparer);

        // Index for lookup by culture
        builder.HasIndex(m => m.Culture);
    }
}
