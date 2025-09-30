using System.Reflection;

using Infrastructure.Persistence.Converters;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using SharedKernel.Domain.Attributes.Metadata;

namespace Infrastructure.Persistence.Configurations.Common;

internal static class MetadataSupportModelBuilderExtensions
{
    public static void ApplyMetadataSupportConversions(this ModelBuilder builder)
    {
        Type metadataInterface = typeof(IMetadataSupport);
        ValueConverter<IDictionary<string, string?>?, string?> converter = DictionaryJsonConverter.GetConverter();
        ValueComparer<IDictionary<string, string?>?> comparer = DictionaryJsonConverter.GetComparer();

        foreach (IMutableEntityType entityType in builder.Model.GetEntityTypes())
        {
            Type? clrType = entityType.ClrType;
            if (clrType == null) continue;
            if (!metadataInterface.IsAssignableFrom(clrType)) continue;

            EntityTypeBuilder entityBuilder = builder.Entity(clrType);

            // PublicMetadata
            PropertyInfo? pubProp = clrType.GetProperty(nameof(IMetadataSupport.PublicMetadata), BindingFlags.Public | BindingFlags.Instance);
            if (pubProp != null)
            {
                entityBuilder
                    .Property(typeof(IDictionary<string, string?>), pubProp.Name)
                    .HasConversion(converter);
                entityBuilder
                    .Property(typeof(IDictionary<string, string?>), pubProp.Name)
                    .Metadata.SetValueComparer(comparer);
            }

            // PrivateMetadata
            PropertyInfo? privProp = clrType.GetProperty(nameof(IMetadataSupport.PrivateMetadata), BindingFlags.Public | BindingFlags.Instance);
            if (privProp != null)
            {
                entityBuilder
                    .Property(typeof(IDictionary<string, string?>), privProp.Name)
                    .HasConversion(converter);
                entityBuilder
                    .Property(typeof(IDictionary<string, string?>), privProp.Name)
                    .Metadata.SetValueComparer(comparer);
            }
        }
    }
}